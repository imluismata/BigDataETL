using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Application.Servicios;

/// <summary>
/// Coordina una ejecución completa:
///   - Genera un Id de ejecución (Guid) para relacionar staging y logs.
///   - Lanza TODAS las fuentes habilitadas al mismo tiempo (en paralelo).
///   - Espera a que terminen y guarda el resultado de cada una en stg.LogExtraccion.
///
/// Recibe la lista de procesos por inyección de dependencias
/// (IEnumerable&lt;IProcesoExtraccion&gt;). Si una fuente está deshabilitada en
/// appsettings.json, simplemente no aparece en la lista.
/// </summary>
public class OrquestadorExtraccion : IOrquestadorExtraccion
{
    private readonly IEnumerable<IProcesoExtraccion> _procesos;
    private readonly ILogExtraccionRepository _logRepository;
    private readonly ILogger<OrquestadorExtraccion> _logger;

    public OrquestadorExtraccion(
        IEnumerable<IProcesoExtraccion> procesos,
        ILogExtraccionRepository logRepository,
        ILogger<OrquestadorExtraccion> logger)
    {
        _procesos = procesos;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<ResumenEjecucion> EjecutarAsync(CancellationToken cancellationToken)
    {
        var resumen = new ResumenEjecucion { IdEjecucion = Guid.NewGuid() };
        List<IProcesoExtraccion> procesos = _procesos.ToList();

        if (procesos.Count == 0)
        {
            _logger.LogWarning("No hay fuentes habilitadas en la configuración. No se extrae nada.");
            return resumen;
        }

        _logger.LogInformation(
            "=== Ejecución {IdEjecucion}: inicio de la extracción de {Cantidad} fuente(s): {Fuentes} ===",
            resumen.IdEjecucion, procesos.Count, string.Join(", ", procesos.Select(p => p.NombreFuente)));

        Stopwatch cronometroTotal = Stopwatch.StartNew();

        // Paralelismo: cada fuente se lanza en su propio hilo del ThreadPool
        // (Task.Run) SIN esperar a la anterior. Task.Run asegura que corran
        // a la vez aunque una fuente tenga partes síncronas (por ejemplo,
        // leer un archivo local o procesar el CSV).
        // ToList() es importante: hace que todas las tareas arranquen en este momento.
        List<Task<ResumenFuente>> tareas = procesos
            .Select(proceso => Task.Run(() => proceso.EjecutarAsync(resumen.IdEjecucion, cancellationToken), cancellationToken))
            .ToList();

        // Se espera a que terminen TODAS. Cada proceso atrapa sus propios
        // errores, así que una fuente que falla no cancela a las demás.
        ResumenFuente[] resultados = await Task.WhenAll(tareas);

        cronometroTotal.Stop();
        resumen.DuracionTotalMs = cronometroTotal.ElapsedMilliseconds;
        resumen.Fuentes.AddRange(resultados);

        // Guardar el historial de cada fuente en la base de datos
        foreach (ResumenFuente resultado in resultados)
        {
            await RegistrarEnBitacoraAsync(resultado, cancellationToken);
        }

        return resumen;
    }

    /// <summary>
    /// Guarda el resumen en stg.LogExtraccion. Si falla, solo se avisa en el log:
    /// un problema con la bitácora no debe tumbar todo el proceso.
    /// </summary>
    private async Task RegistrarEnBitacoraAsync(ResumenFuente resultado, CancellationToken cancellationToken)
    {
        try
        {
            await _logRepository.RegistrarAsync(resultado, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "[{Fuente}] No se pudo guardar el resultado en stg.LogExtraccion",
                resultado.NombreFuente);
        }
    }
}
