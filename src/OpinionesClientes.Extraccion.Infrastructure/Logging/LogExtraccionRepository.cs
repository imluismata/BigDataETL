using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Application.Modelos;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Logging;

/// <summary>
/// Inserta en stg.LogExtraccion una fila por fuente y por ejecución.
/// Todos los valores van como parámetros (nunca concatenados en el SQL).
/// </summary>
public class LogExtraccionRepository : ILogExtraccionRepository
{
    private const string SentenciaInsert = @"
        INSERT INTO stg.LogExtraccion
            (IdEjecucion, Fuente, TipoFuente, Origen, FechaInicio, FechaFin,
             DuracionMs, RegistrosExtraidos, Estado, MensajeError)
        VALUES
            (@IdEjecucion, @Fuente, @TipoFuente, @Origen, @FechaInicio, @FechaFin,
             @DuracionMs, @RegistrosExtraidos, @Estado, @MensajeError);";

    private readonly OpcionesStaging _opciones;

    public LogExtraccionRepository(IOptions<OpcionesStaging> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task RegistrarAsync(ResumenFuente resumen, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(_opciones.CadenaConexion);
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(SentenciaInsert, conexion);

        string estado = resumen.Exitosa ? "Completada" : "Fallida";

        // El mensaje de error se recorta al tamaño de la columna (1000)
        string? mensaje = resumen.MensajeError;
        if (mensaje is not null && mensaje.Length > 1000)
        {
            mensaje = mensaje.Substring(0, 1000);
        }

        comando.Parameters.Add("@IdEjecucion", SqlDbType.UniqueIdentifier).Value = resumen.IdEjecucion;
        comando.Parameters.Add("@Fuente", SqlDbType.NVarChar, 50).Value = resumen.NombreFuente;
        comando.Parameters.Add("@TipoFuente", SqlDbType.NVarChar, 20).Value = resumen.TipoFuente;
        comando.Parameters.Add("@Origen", SqlDbType.NVarChar, 300).Value = resumen.Origen;
        comando.Parameters.Add("@FechaInicio", SqlDbType.DateTime2).Value = resumen.FechaInicio;
        comando.Parameters.Add("@FechaFin", SqlDbType.DateTime2).Value = resumen.FechaFin;
        comando.Parameters.Add("@DuracionMs", SqlDbType.BigInt).Value = resumen.DuracionMs;
        comando.Parameters.Add("@RegistrosExtraidos", SqlDbType.Int).Value = resumen.RegistrosExtraidos;
        comando.Parameters.Add("@Estado", SqlDbType.VarChar, 15).Value = estado;
        comando.Parameters.Add("@MensajeError", SqlDbType.NVarChar, 1000).Value = (object?)mensaje ?? DBNull.Value;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
