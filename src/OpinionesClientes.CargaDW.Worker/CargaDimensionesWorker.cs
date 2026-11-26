using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;

namespace OpinionesClientes.CargaDW.Worker;

/// <summary>
/// Servicio en segundo plano que dispara la carga de dimensiones.
///   - CargaDW:IntervaloMinutos = 0 -> carga una vez y se detiene.
///   - Mayor que 0                  -> carga al iniciar y luego cada N minutos.
/// </summary>
public class CargaDimensionesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _cicloVida;
    private readonly OpcionesCargaDW _opciones;
    private readonly ILogger<CargaDimensionesWorker> _logger;

    public CargaDimensionesWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime cicloVida,
        IOptions<OpcionesCargaDW> opciones,
        ILogger<CargaDimensionesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _cicloVida = cicloVida;
        _opciones = opciones.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_opciones.IntervaloMinutos <= 0)
        {
            await EjecutarCargaAsync(stoppingToken);
            _logger.LogInformation("IntervaloMinutos = 0: el Worker se detiene después de una carga.");
            _cicloVida.StopApplication();
            return;
        }

        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(_opciones.IntervaloMinutos));

        try
        {
            do
            {
                await EjecutarCargaAsync(stoppingToken);
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Se pidió detener el servicio: salida normal
        }
    }

    private async Task EjecutarCargaAsync(CancellationToken stoppingToken)
    {
        // Un scope nuevo por ejecución: conexiones y servicios nuevos cada vez
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IOrquestadorCargaDimensiones orquestador = scope.ServiceProvider.GetRequiredService<IOrquestadorCargaDimensiones>();

        try
        {
            ResumenCargaDW resumen = await orquestador.EjecutarAsync(stoppingToken);
            MostrarResumen(resumen);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Carga cancelada porque el servicio se está deteniendo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante la carga de dimensiones.");
        }
    }

    private void MostrarResumen(ResumenCargaDW resumen)
    {
        _logger.LogInformation(
            "  {Estado,-6} {Dimension,-16} {Leidos,7} {Insertados,10} {Actualizados,12} {SinCambios,10} {Rechazados,10} {Duracion,8}",
            "Estado", "Dimensión", "Leídos", "Insertados", "Actualizados", "SinCambios", "Rechazados", "ms");

        foreach (ResumenCargaDimension d in resumen.Dimensiones)
        {
            _logger.LogInformation(
                "  {Estado,-6} {Dimension,-16} {Leidos,7} {Insertados,10} {Actualizados,12} {SinCambios,10} {Rechazados,10} {Duracion,8}",
                d.Exitosa ? "OK" : "FALLÓ", d.Dimension, d.RegistrosLeidos, d.Insertados,
                d.Actualizados, d.SinCambios, d.Rechazados, d.DuracionMs);
        }

        if (resumen.TodasExitosas)
        {
            _logger.LogInformation("=== Carga {IdEjecucion} completada en {Duracion} ms ===",
                resumen.IdEjecucion, resumen.DuracionTotalMs);
        }
        else
        {
            _logger.LogWarning("=== Carga {IdEjecucion} terminó con errores ({Duracion} ms). Revise etl.LogCargaDimensiones ===",
                resumen.IdEjecucion, resumen.DuracionTotalMs);
        }
    }
}
