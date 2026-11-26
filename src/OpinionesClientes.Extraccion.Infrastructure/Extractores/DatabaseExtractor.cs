using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Extractores;

/// <summary>
/// Extrae las reseñas web desde la base de datos relacional de la tienda
/// en línea (TiendaOnlineDB) usando ADO.NET (Microsoft.Data.SqlClient).
///
/// Se usa ADO.NET con SqlDataReader (y no EF Core) porque para extraer
/// solo hace falta leer filas hacia adelante: es la forma más rápida y
/// la que menos memoria usa.
/// </summary>
public class DatabaseExtractor : IExtractor<ResenaWebExtraida>
{
    // Consulta definida en el código (no armada con texto del usuario).
    // El filtro de fecha usa un PARÁMETRO (@FechaDesde): así no hay riesgo
    // de inyección SQL. Si @FechaDesde es NULL, se traen todas las reseñas.
    private const string Consulta = @"
        SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating
        FROM dbo.Resenas
        WHERE (@FechaDesde IS NULL OR Fecha >= @FechaDesde)
        ORDER BY IdReview;";

    private readonly OpcionesFuenteBaseDatos _opciones;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(IOptions<OpcionesFuenteBaseDatos> opciones, ILogger<DatabaseExtractor> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public string NombreFuente => "ResenasWeb";

    public string TipoFuente => "BaseDatos";

    public string Origen => "TiendaOnlineDB.dbo.Resenas";

    public async IAsyncEnumerable<ResenaWebExtraida> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // "await using" cierra la conexión automáticamente al terminar,
        // aunque ocurra un error.
        await using var conexion = new SqlConnection(_opciones.CadenaConexion);
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(Consulta, conexion);
        comando.CommandTimeout = _opciones.TimeoutSegundos;
        comando.Parameters.Add(CrearParametroFecha());

        // SequentialAccess: lee las columnas en orden y sin guardar la fila
        // completa en memoria. Es lo más eficiente para muchos registros.
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);

        while (await lector.ReadAsync(cancellationToken))
        {
            // Con SequentialAccess las columnas deben leerse en el mismo orden del SELECT
            yield return new ResenaWebExtraida
            {
                IdReview = LeerComoTexto(lector, 0),
                IdCliente = LeerComoTexto(lector, 1),
                IdProducto = LeerComoTexto(lector, 2),
                Fecha = LeerComoTexto(lector, 3),
                Comentario = LeerComoTexto(lector, 4),
                Rating = LeerComoTexto(lector, 5)
            };
        }
    }

    /// <summary>
    /// Crea el parámetro @FechaDesde. Si no hay fecha configurada (o no es válida),
    /// se envía NULL y la consulta trae todo.
    /// </summary>
    private SqlParameter CrearParametroFecha()
    {
        var parametro = new SqlParameter("@FechaDesde", SqlDbType.Date)
        {
            Value = DBNull.Value
        };

        if (string.IsNullOrWhiteSpace(_opciones.FechaDesde))
        {
            return parametro;
        }

        bool esFechaValida = DateTime.TryParseExact(
            _opciones.FechaDesde, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime fecha);

        if (esFechaValida)
        {
            parametro.Value = fecha;
            _logger.LogInformation("[{Fuente}] Extracción incremental desde {Fecha}", NombreFuente, _opciones.FechaDesde);
        }
        else
        {
            _logger.LogWarning(
                "[{Fuente}] FechaDesde '{Valor}' no tiene el formato yyyy-MM-dd. Se extraerán todas las reseñas.",
                NombreFuente, _opciones.FechaDesde);
        }

        return parametro;
    }

    /// <summary>
    /// Convierte el valor de una columna a texto (staging guarda todo como texto).
    /// Las fechas se escriben como yyyy-MM-dd, igual que en los archivos CSV.
    /// </summary>
    private static string? LeerComoTexto(SqlDataReader lector, int columna)
    {
        if (lector.IsDBNull(columna))
        {
            return null;
        }

        object valor = lector.GetValue(columna);

        if (valor is DateTime fecha)
        {
            return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(valor, CultureInfo.InvariantCulture);
    }
}
