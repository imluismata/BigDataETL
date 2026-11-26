using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW;

/// <summary>
/// Guarda la bitácora de la carga en el esquema etl del DataWarehouse:
///   etl.LogCargaDimensiones     -> resultado de cada dimensión
///   etl.ErroresCargaDimensiones -> cada fila rechazada con su motivo
/// Todo va con parámetros y en una sola transacción.
/// </summary>
public class LogCargaDWRepository : ILogCargaDWRepository
{
    private const string InsertLog = @"
        INSERT INTO etl.LogCargaDimensiones
            (IdEjecucion, Dimension, FechaInicio, FechaFin, DuracionMs, RegistrosLeidos,
             Insertados, Actualizados, SinCambios, Rechazados, Estado, MensajeError)
        VALUES
            (@IdEjecucion, @Dimension, @FechaInicio, @FechaFin, @DuracionMs, @RegistrosLeidos,
             @Insertados, @Actualizados, @SinCambios, @Rechazados, @Estado, @MensajeError);";

    private const string InsertError = @"
        INSERT INTO etl.ErroresCargaDimensiones (IdEjecucion, Dimension, ClaveNegocio, Motivo)
        VALUES (@IdEjecucion, @Dimension, @ClaveNegocio, @Motivo);";

    private readonly OpcionesConexionesDW _conexiones;

    public LogCargaDWRepository(IOptions<OpcionesConexionesDW> conexiones)
    {
        _conexiones = conexiones.Value;
    }

    public async Task RegistrarAsync(ResumenCargaDimension resumen, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(_conexiones.CadenaDataWarehouse);
        await conexion.OpenAsync(cancellationToken);
        SqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            await InsertarLogAsync(conexion, transaccion, resumen, cancellationToken);

            foreach (RegistroRechazado rechazo in resumen.Rechazos)
            {
                await InsertarErrorAsync(conexion, transaccion, resumen, rechazo, cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // SQL Server ya la había deshecho
            }

            throw;
        }
        finally
        {
            await transaccion.DisposeAsync();
        }
    }

    private static async Task InsertarLogAsync(SqlConnection conexion, SqlTransaction transaccion,
        ResumenCargaDimension resumen, CancellationToken cancellationToken)
    {
        await using var comando = new SqlCommand(InsertLog, conexion, transaccion);

        string estado = resumen.Exitosa ? "Completada" : "Fallida";
        string? mensaje = Recortar(resumen.MensajeError, 1000);

        comando.Parameters.Add("@IdEjecucion", SqlDbType.UniqueIdentifier).Value = resumen.IdEjecucion;
        comando.Parameters.Add("@Dimension", SqlDbType.VarChar, 30).Value = resumen.Dimension;
        comando.Parameters.Add("@FechaInicio", SqlDbType.DateTime2).Value = resumen.FechaInicio;
        comando.Parameters.Add("@FechaFin", SqlDbType.DateTime2).Value = resumen.FechaFin;
        comando.Parameters.Add("@DuracionMs", SqlDbType.BigInt).Value = resumen.DuracionMs;
        comando.Parameters.Add("@RegistrosLeidos", SqlDbType.Int).Value = resumen.RegistrosLeidos;
        comando.Parameters.Add("@Insertados", SqlDbType.Int).Value = resumen.Insertados;
        comando.Parameters.Add("@Actualizados", SqlDbType.Int).Value = resumen.Actualizados;
        comando.Parameters.Add("@SinCambios", SqlDbType.Int).Value = resumen.SinCambios;
        comando.Parameters.Add("@Rechazados", SqlDbType.Int).Value = resumen.Rechazados;
        comando.Parameters.Add("@Estado", SqlDbType.VarChar, 15).Value = estado;
        comando.Parameters.Add("@MensajeError", SqlDbType.NVarChar, 1000).Value = (object?)mensaje ?? DBNull.Value;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertarErrorAsync(SqlConnection conexion, SqlTransaction transaccion,
        ResumenCargaDimension resumen, RegistroRechazado rechazo, CancellationToken cancellationToken)
    {
        await using var comando = new SqlCommand(InsertError, conexion, transaccion);

        comando.Parameters.Add("@IdEjecucion", SqlDbType.UniqueIdentifier).Value = resumen.IdEjecucion;
        comando.Parameters.Add("@Dimension", SqlDbType.VarChar, 30).Value = resumen.Dimension;
        comando.Parameters.Add("@ClaveNegocio", SqlDbType.NVarChar, 200).Value = Recortar(rechazo.ClaveNegocio, 200)!;
        comando.Parameters.Add("@Motivo", SqlDbType.NVarChar, 300).Value = Recortar(rechazo.Motivo, 300)!;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? Recortar(string? texto, int maximo)
    {
        if (texto is null || texto.Length <= maximo)
        {
            return texto;
        }

        return texto.Substring(0, maximo);
    }
}
