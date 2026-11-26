using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Configuracion;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Worker;

/// <summary>
/// Servicio en segundo plano (Worker Service) que dispara la extracción.
///
/// - Si Ejecucion:IntervaloMinutos = 0: extrae una sola vez y se detiene.
/// - Si es mayor que 0: extrae al iniciar y luego cada N minutos.
///
/// El Worker no sabe cómo se extrae: solo llama al orquestador.
/// </summary>
public class ExtraccionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _cicloVida;
    private readonly OpcionesEjecucion _opciones;
    private readonly ILogger<ExtraccionWorker> _logger;

    public ExtraccionWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime cicloVida,
        IOptions<OpcionesEjecucion> opciones,
        ILogger<ExtraccionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _cicloVida = cicloVida;
        _opciones = opciones.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Modo "una sola vez"
        if (_opciones.IntervaloMinutos <= 0)
        {
            await EjecutarExtraccionAsync(stoppingToken);

            _logger.LogInformation("IntervaloMinutos = 0: el Worker se detiene después de una ejecución.");
            _cicloVida.StopApplication();
            return;
        }

        // Modo programado: PeriodicTimer espera el intervalo sin bloquear un hilo
        _logger.LogInformation("La extracción se ejecutará cada {Minutos} minuto(s).", _opciones.IntervaloMinutos);
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(_opciones.IntervaloMinutos));

        try
        {
            do
            {
                await EjecutarExtraccionAsync(stoppingToken);
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Se pidió detener el servicio (Ctrl+C o apagado): salida normal
        }
    }

    /// <summary>
    /// Una ejecución completa. Se crea un "scope" nuevo cada vez para que
    /// los servicios (conexiones, HttpClient, etc.) sean nuevos en cada
    /// ejecución y se liberen al terminar.
    /// </summary>
    private async Task EjecutarExtraccionAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IOrquestadorExtraccion orquestador = scope.ServiceProvider.GetRequiredService<IOrquestadorExtraccion>();

        try
        {
            ResumenEjecucion resumen = await orquestador.EjecutarAsync(stoppingToken);
            MostrarResumen(resumen);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Extracción cancelada porque el servicio se está deteniendo.");
        }
        catch (Exception ex)
        {
            // Un error inesperado no debe tumbar el Worker: se registra y
            // se vuelve a intentar en la siguiente ejecución programada.
            _logger.LogError(ex, "Error inesperado durante la ejecución de la extracción.");
        }
    }

    private void MostrarResumen(ResumenEjecucion resumen)
    {
        foreach (ResumenFuente fuente in resumen.Fuentes)
        {
            string estado = fuente.Exitosa ? "OK" : "FALLÓ";

            _logger.LogInformation(
                "  {Estado,-6} {Fuente,-20} {Tipo,-10} {Registros,6} registros  {Duracion,6} ms",
                estado, fuente.NombreFuente, fuente.TipoFuente, fuente.RegistrosExtraidos, fuente.DuracionMs);
        }

        if (resumen.TodasExitosas)
        {
            _logger.LogInformation(
                "=== Ejecución {IdEjecucion} completada: {Total} registros en {Duracion} ms ===",
                resumen.IdEjecucion, resumen.TotalRegistros, resumen.DuracionTotalMs);
        }
        else
        {
            _logger.LogWarning(
                "=== Ejecución {IdEjecucion} terminó con errores: {Total} registros en {Duracion} ms. Revise stg.LogExtraccion ===",
                resumen.IdEjecucion, resumen.TotalRegistros, resumen.DuracionTotalMs);
        }
    }
}
