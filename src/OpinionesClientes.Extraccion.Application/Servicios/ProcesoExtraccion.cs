using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Configuracion;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Application.Servicios;

/// <summary>
/// Flujo de extracción de UNA fuente. Es igual para todas las fuentes:
///
///   1. (Opcional) vaciar la tabla de staging de la fuente.
///   2. Leer los registros del extractor uno por uno.
///   3. Juntarlos en lotes de N registros.
///   4. Guardar cada lote en staging con carga masiva.
///   5. Medir el tiempo con Stopwatch y devolver un resumen.
///
/// Es genérica (T) para no repetir este mismo código tres veces.
/// Solo depende de interfaces: no sabe si la fuente es CSV, BD o API,
/// ni cómo se guarda en SQL Server (principio de inversión de dependencias).
/// </summary>
public class ProcesoExtraccion<T> : IProcesoExtraccion
{
    private readonly IExtractor<T> _extractor;
    private readonly IStagingRepository<T> _staging;
    private readonly OpcionesEjecucion _opciones;
    private readonly ILogger<ProcesoExtraccion<T>> _logger;

    public ProcesoExtraccion(
        IExtractor<T> extractor,
        IStagingRepository<T> staging,
        IOptions<OpcionesEjecucion> opciones,
        ILogger<ProcesoExtraccion<T>> logger)
    {
        _extractor = extractor;
        _staging = staging;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public string NombreFuente => _extractor.NombreFuente;

    public async Task<ResumenFuente> EjecutarAsync(Guid idEjecucion, CancellationToken cancellationToken)
    {
        // Datos iniciales del resumen
        var resumen = new ResumenFuente
        {
            IdEjecucion = idEjecucion,
            NombreFuente = _extractor.NombreFuente,
            TipoFuente = _extractor.TipoFuente,
            Origen = _extractor.Origen,
            FechaInicio = DateTime.Now
        };

        // Cronómetro para medir el rendimiento de esta fuente
        Stopwatch cronometro = Stopwatch.StartNew();

        _logger.LogInformation(
            "[{Fuente}] Inicio de la extracción ({Tipo}) desde {Origen}",
            resumen.NombreFuente, resumen.TipoFuente, resumen.Origen);

        try
        {
            // Paso 1: vaciar staging (si está configurado así)
            if (_opciones.LimpiarStagingAntesDeExtraer)
            {
                await _staging.LimpiarAsync(cancellationToken);
            }

            // Paso 2 y 3: leer y acumular en un lote
            var lote = new List<T>(_opciones.TamanoLote);

            await foreach (T registro in _extractor.ExtraerAsync(cancellationToken))
            {
                lote.Add(registro);

                // Paso 4: cuando el lote se llena, se guarda y se vacía
                if (lote.Count >= _opciones.TamanoLote)
                {
                    await GuardarLoteAsync(lote, resumen, cancellationToken);
                }
            }

            // Guardar lo que quedó en el último lote (si quedó algo)
            if (lote.Count > 0)
            {
                await GuardarLoteAsync(lote, resumen, cancellationToken);
            }

            resumen.Exitosa = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // El servicio se está deteniendo: no es un error de la fuente,
            // así que se deja subir la excepción.
            throw;
        }
        catch (Exception ex)
        {
            // Si una fuente falla, se registra el error pero NO se detienen
            // las demás fuentes (cada una corre por separado).
            resumen.Exitosa = false;
            resumen.MensajeError = ex.Message;

            _logger.LogError(ex,
                "[{Fuente}] Error durante la extracción. Registros guardados antes del error: {Registros}",
                resumen.NombreFuente, resumen.RegistrosExtraidos);
        }
        finally
        {
            // Paso 5: detener el cronómetro y completar el resumen
            cronometro.Stop();
            resumen.DuracionMs = cronometro.ElapsedMilliseconds;
            resumen.FechaFin = DateTime.Now;
        }

        if (resumen.Exitosa)
        {
            _logger.LogInformation(
                "[{Fuente}] Extracción completada: {Registros} registros en {Lotes} lote(s), {Duracion} ms ({Velocidad:F0} reg/s)",
                resumen.NombreFuente, resumen.RegistrosExtraidos, resumen.LotesGuardados,
                resumen.DuracionMs, resumen.RegistrosPorSegundo);
        }

        return resumen;
    }

    /// <summary>
    /// Guarda un lote en staging, actualiza los contadores y vacía la lista
    /// para reutilizarla con el siguiente lote.
    /// </summary>
    private async Task GuardarLoteAsync(List<T> lote, ResumenFuente resumen, CancellationToken cancellationToken)
    {
        int insertados = await _staging.GuardarLoteAsync(
            lote, resumen.IdEjecucion, resumen.Origen, cancellationToken);

        resumen.RegistrosExtraidos += insertados;
        resumen.LotesGuardados++;

        _logger.LogDebug(
            "[{Fuente}] Lote {NumeroLote} guardado en staging ({Cantidad} registros)",
            resumen.NombreFuente, resumen.LotesGuardados, insertados);

        lote.Clear();
    }
}
