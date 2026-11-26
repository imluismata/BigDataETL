using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW;

/// <summary>
/// Flujo común de carga de UNA dimensión (patrón Template Method):
///
///   EXTRAER     : ejecuta la consulta en OpinionesClientesDB y lee las filas.
///   TRANSFORMAR : limpia cada fila con TransformadorDimensiones; las que no
///                 cumplen las reglas se rechazan con su motivo.
///   CARGAR      : dentro de una transacción en OpinionesClientesDW:
///                   1. crea una tabla temporal #Origen,
///                   2. copia las filas limpias con SqlBulkCopy,
///                   3. ejecuta un MERGE contra la dimensión:
///                        - si la fila no existe  -> INSERT
///                        - si existe y cambió    -> UPDATE (SCD tipo 1)
///                        - si existe igual       -> no hace nada
///
/// Gracias al MERGE la carga se puede ejecutar varias veces sin duplicar
/// filas (es idempotente). Las filas especiales -1 nunca se tocan porque
/// no existen en el origen.
///
/// Cada dimensión hija solo define: su consulta, cómo leer una fila,
/// qué regla de transformación usa, su tabla temporal y su MERGE.
/// </summary>
public abstract class CargaDimensionBase<T> : ICargaDimension where T : class
{
    private readonly OpcionesConexionesDW _conexiones;
    private readonly ILogger _logger;

    /// <summary>Cadenas de conexión y timeout (para las hijas que redefinen la extracción).</summary>
    protected OpcionesConexionesDW Conexiones => _conexiones;

    protected CargaDimensionBase(IOptions<OpcionesConexionesDW> conexiones, ILogger logger)
    {
        _conexiones = conexiones.Value;
        _logger = logger;
    }

    public abstract string NombreDimension { get; }

    /// <summary>SELECT que se ejecuta en OpinionesClientesDB.</summary>
    protected abstract string ConsultaOrigen { get; }

    /// <summary>Convierte la fila actual del SqlDataReader en un objeto.</summary>
    protected abstract T LeerFila(SqlDataReader lector);

    /// <summary>Aplica la regla de transformación de esta dimensión.</summary>
    protected abstract ResultadoTransformacion<T> Transformar(T fila);

    /// <summary>Texto que identifica la fila en el registro de errores. Ej: "IdCliente=25".</summary>
    protected abstract string ObtenerClaveNegocio(T fila);

    /// <summary>CREATE TABLE #Origen (...) con las columnas que usa el MERGE.</summary>
    protected abstract string SentenciaTablaTemporal { get; }

    /// <summary>Tabla en memoria con las mismas columnas que #Origen.</summary>
    protected abstract DataTable CrearTablaEnMemoria();

    /// <summary>Agrega una fila limpia a la tabla en memoria.</summary>
    protected abstract void AgregarFila(DataTable tabla, T fila);

    /// <summary>
    /// MERGE de #Origen contra la dimensión. Debe terminar en
    /// "OUTPUT $action INTO @Acciones;" para poder contar inserts y updates.
    /// </summary>
    protected abstract string SentenciaMerge { get; }

    public async Task<ResumenCargaDimension> CargarAsync(Guid idEjecucion, CancellationToken cancellationToken)
    {
        var resumen = new ResumenCargaDimension
        {
            IdEjecucion = idEjecucion,
            Dimension = NombreDimension,
            FechaInicio = DateTime.Now
        };

        Stopwatch cronometro = Stopwatch.StartNew();

        try
        {
            // ---------------- E: Extraer ----------------
            List<T> filasOrigen = await ExtraerAsync(cancellationToken);
            resumen.RegistrosLeidos = filasOrigen.Count;

            // ---------------- T: Transformar ----------------
            List<T> filasLimpias = TransformarTodas(filasOrigen, resumen);

            // ---------------- L: Cargar ----------------
            (int insertados, int actualizados) = await CargarConMergeAsync(filasLimpias, cancellationToken);
            resumen.Insertados = insertados;
            resumen.Actualizados = actualizados;

            resumen.Exitosa = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Si una dimensión falla, la transacción ya se deshizo: la dimensión
            // queda como estaba. Se registra el error y se sigue con las demás.
            resumen.Exitosa = false;
            resumen.MensajeError = ex.Message;
            _logger.LogError(ex, "[{Dimension}] Error en la carga", NombreDimension);
        }
        finally
        {
            cronometro.Stop();
            resumen.DuracionMs = cronometro.ElapsedMilliseconds;
            resumen.FechaFin = DateTime.Now;
        }

        if (resumen.Exitosa)
        {
            _logger.LogInformation(
                "[{Dimension}] Leídos {Leidos} | Insertados {Insertados} | Actualizados {Actualizados} | Sin cambios {SinCambios} | Rechazados {Rechazados} | {Duracion} ms",
                NombreDimension, resumen.RegistrosLeidos, resumen.Insertados, resumen.Actualizados,
                resumen.SinCambios, resumen.Rechazados, resumen.DuracionMs);
        }

        return resumen;
    }

    /// <summary>
    /// EXTRAER: ejecuta ConsultaOrigen en OpinionesClientesDB.
    /// Es virtual porque DimFecha no lee filas: genera el calendario.
    /// </summary>
    protected virtual async Task<List<T>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var filas = new List<T>();

        await using var conexion = new SqlConnection(_conexiones.CadenaOrigen);
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(ConsultaOrigen, conexion);
        comando.CommandTimeout = _conexiones.TimeoutSegundos;

        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);

        while (await lector.ReadAsync(cancellationToken))
        {
            filas.Add(LeerFila(lector));
        }

        return filas;
    }

    /// <summary>
    /// TRANSFORMAR: aplica la regla a cada fila. Las rechazadas se guardan
    /// en el resumen (luego van a etl.ErroresCargaDimensiones).
    /// </summary>
    private List<T> TransformarTodas(List<T> filasOrigen, ResumenCargaDimension resumen)
    {
        var limpias = new List<T>();

        // Claves ya aceptadas. Sin distinguir mayúsculas, igual que SQL Server.
        var clavesVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (T fila in filasOrigen)
        {
            ResultadoTransformacion<T> resultado = Transformar(fila);

            if (!resultado.EsValida)
            {
                Rechazar(resumen, ObtenerClaveNegocio(fila), resultado.MotivoRechazo ?? "Sin motivo");
                continue;
            }

            // Dos filas del origen con la misma clave después de limpiarlas
            // (por ejemplo "Excelente" y "excelente") harían fallar el MERGE:
            // se queda la primera y la otra se rechaza.
            string clave = ObtenerClaveNegocio(resultado.Fila!);
            if (!clavesVistas.Add(clave))
            {
                Rechazar(resumen, clave, "Clave duplicada en el origen");
                continue;
            }

            limpias.Add(resultado.Fila!);
        }

        return limpias;
    }

    private void Rechazar(ResumenCargaDimension resumen, string clave, string motivo)
    {
        resumen.Rechazos.Add(new RegistroRechazado { ClaveNegocio = clave, Motivo = motivo });
        _logger.LogWarning("[{Dimension}] Fila rechazada ({Clave}): {Motivo}", NombreDimension, clave, motivo);
    }

    /// <summary>
    /// CARGAR: tabla temporal + SqlBulkCopy + MERGE, todo en una transacción.
    /// Devuelve cuántas filas insertó y cuántas actualizó el MERGE.
    /// </summary>
    private async Task<(int Insertados, int Actualizados)> CargarConMergeAsync(List<T> filas, CancellationToken cancellationToken)
    {
        if (filas.Count == 0)
        {
            return (0, 0);
        }

        // Pasar las filas limpias a una tabla en memoria
        using DataTable tabla = CrearTablaEnMemoria();
        foreach (T fila in filas)
        {
            AgregarFila(tabla, fila);
        }

        await using var conexion = new SqlConnection(_conexiones.CadenaDataWarehouse);
        await conexion.OpenAsync(cancellationToken);

        // Todo o nada: si algo falla, la dimensión queda como estaba
        SqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            // 1. Tabla temporal (solo existe en esta conexión)
            await EjecutarAsync(conexion, transaccion, SentenciaTablaTemporal, cancellationToken);

            // 2. Copia masiva de las filas limpias a #Origen
            using (var cargaMasiva = new SqlBulkCopy(conexion, SqlBulkCopyOptions.Default, transaccion))
            {
                cargaMasiva.DestinationTableName = "#Origen";
                cargaMasiva.BulkCopyTimeout = _conexiones.TimeoutSegundos;

                foreach (DataColumn columna in tabla.Columns)
                {
                    cargaMasiva.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);
                }

                await cargaMasiva.WriteToServerAsync(tabla, cancellationToken);
            }

            // 3. MERGE y conteo de lo que hizo (INSERT / UPDATE)
            string lote =
                "DECLARE @Acciones TABLE (Accion NVARCHAR(10));\n" +
                SentenciaMerge + "\n" +
                "SELECT\n" +
                "    ISNULL(SUM(CASE WHEN Accion = 'INSERT' THEN 1 ELSE 0 END), 0) AS Insertados,\n" +
                "    ISNULL(SUM(CASE WHEN Accion = 'UPDATE' THEN 1 ELSE 0 END), 0) AS Actualizados\n" +
                "FROM @Acciones;\n" +
                "DROP TABLE #Origen;";

            int insertados = 0;
            int actualizados = 0;

            await using (var comando = new SqlCommand(lote, conexion, transaccion))
            {
                comando.CommandTimeout = _conexiones.TimeoutSegundos;

                await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);
                if (await lector.ReadAsync(cancellationToken))
                {
                    insertados = lector.GetInt32(0);
                    actualizados = lector.GetInt32(1);
                }
            }

            await transaccion.CommitAsync(cancellationToken);
            return (insertados, actualizados);
        }
        catch
        {
            await DeshacerAsync(transaccion);
            throw;
        }
        finally
        {
            await transaccion.DisposeAsync();
        }
    }

    /// <summary>
    /// Deshace la transacción. Si SQL Server ya la deshizo por el error,
    /// Rollback lanza otra excepción: se ignora para no ocultar el error original.
    /// </summary>
    private static async Task DeshacerAsync(SqlTransaction transaccion)
    {
        try
        {
            await transaccion.RollbackAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // La transacción ya no estaba activa
        }
    }

    private async Task EjecutarAsync(SqlConnection conexion, SqlTransaction transaccion, string sentencia, CancellationToken cancellationToken)
    {
        await using var comando = new SqlCommand(sentencia, conexion, transaccion);
        comando.CommandTimeout = _conexiones.TimeoutSegundos;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Convierte null en DBNull.Value para ADO.NET.</summary>
    protected static object ValorODbNull(string? valor)
    {
        if (valor is null)
        {
            return DBNull.Value;
        }

        return valor;
    }

    /// <summary>Lee una columna de texto que puede ser NULL.</summary>
    protected static string? LeerTexto(SqlDataReader lector, int columna)
    {
        return lector.IsDBNull(columna) ? null : lector.GetString(columna);
    }
}
