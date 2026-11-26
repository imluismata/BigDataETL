using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Staging;

/// <summary>
/// Código común para guardar en cualquier tabla de staging.
///
/// Usa SqlBulkCopy: envía todas las filas del lote a SQL Server en una
/// sola operación masiva, en vez de un INSERT por fila. Con miles de
/// registros la diferencia de tiempo es muy grande.
///
/// Cada tabla hija (Encuestas, ResenasWeb, ComentariosSociales) solo
/// tiene que decir:
///   - el nombre de su tabla,
///   - qué columnas tiene,
///   - cómo pasar un registro a una fila.
/// (Patrón Template Method: el "cómo guardar" está aquí una sola vez.)
/// </summary>
public abstract class StagingRepositoryBase<T> : IStagingRepository<T>
{
    private readonly OpcionesStaging _opciones;

    protected StagingRepositoryBase(IOptions<OpcionesStaging> opciones)
    {
        _opciones = opciones.Value;
    }

    /// <summary>Nombre de la tabla con su esquema. Ej: "stg.Encuestas".</summary>
    protected abstract string NombreTabla { get; }

    /// <summary>Crea una tabla en memoria con las mismas columnas de la tabla de staging.</summary>
    protected abstract DataTable CrearTablaEnMemoria();

    /// <summary>Agrega a la tabla en memoria una fila con los datos del registro.</summary>
    protected abstract void AgregarFila(DataTable tabla, T registro, Guid idEjecucion, string origen);

    public async Task LimpiarAsync(CancellationToken cancellationToken)
    {
        // NombreTabla es un texto fijo del código (no viene del usuario),
        // por eso es seguro ponerlo directo en la sentencia.
        string sentencia = $"TRUNCATE TABLE {NombreTabla};";

        await using var conexion = new SqlConnection(_opciones.CadenaConexion);
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(sentencia, conexion);
        comando.CommandTimeout = _opciones.TimeoutSegundos;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GuardarLoteAsync(
        IReadOnlyList<T> lote,
        Guid idEjecucion,
        string origen,
        CancellationToken cancellationToken)
    {
        if (lote.Count == 0)
        {
            return 0;
        }

        // 1. Pasar el lote a una tabla en memoria
        using DataTable tabla = CrearTablaEnMemoria();

        foreach (T registro in lote)
        {
            AgregarFila(tabla, registro, idEjecucion, origen);
        }

        // 2. Abrir la conexión con la base de staging
        await using var conexion = new SqlConnection(_opciones.CadenaConexion);
        await conexion.OpenAsync(cancellationToken);

        // 3. Carga masiva. TableLock bloquea la tabla durante la carga,
        //    lo que hace la inserción más rápida (nadie más escribe en staging).
        using var cargaMasiva = new SqlBulkCopy(conexion, SqlBulkCopyOptions.TableLock, null);
        cargaMasiva.DestinationTableName = NombreTabla;
        cargaMasiva.BatchSize = lote.Count;
        cargaMasiva.BulkCopyTimeout = _opciones.TimeoutSegundos;

        // Las columnas se relacionan por nombre (no por posición), así la
        // columna identidad y las de valor por defecto no causan problemas.
        foreach (DataColumn columna in tabla.Columns)
        {
            cargaMasiva.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);
        }

        await cargaMasiva.WriteToServerAsync(tabla, cancellationToken);

        return tabla.Rows.Count;
    }

    /// <summary>
    /// Convierte null en DBNull.Value, que es como ADO.NET representa NULL.
    /// </summary>
    protected static object ValorODbNull(string? valor)
    {
        if (valor is null)
        {
            return DBNull.Value;
        }

        return valor;
    }
}
