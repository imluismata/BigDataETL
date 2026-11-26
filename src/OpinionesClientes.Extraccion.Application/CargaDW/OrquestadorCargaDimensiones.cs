using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Coordina la carga de las dimensiones:
///   1. Genera un IdEjecucion para relacionar la bitácora.
///   2. Carga las dimensiones en el orden de appsettings.json (CargaDW:Dimensiones).
///   3. Si una dimensión falla, se registra el error y se sigue con la siguiente.
///   4. Guarda el resultado de cada dimensión en etl.LogCargaDimensiones.
///
/// Las dimensiones no dependen unas de otras, pero se cargan una tras otra
/// para que el log sea fácil de seguir. La tabla de hechos (siguiente
/// actividad) sí necesita que TODAS las dimensiones estén cargadas antes.
/// </summary>
public class OrquestadorCargaDimensiones : IOrquestadorCargaDimensiones
{
    private readonly IEnumerable<ICargaDimension> _cargas;
    private readonly ILogCargaDWRepository _logRepository;
    private readonly OpcionesCargaDW _opciones;
    private readonly ILogger<OrquestadorCargaDimensiones> _logger;

    public OrquestadorCargaDimensiones(
        IEnumerable<ICargaDimension> cargas,
        ILogCargaDWRepository logRepository,
        IOptions<OpcionesCargaDW> opciones,
        ILogger<OrquestadorCargaDimensiones> logger)
    {
        _cargas = cargas;
        _logRepository = logRepository;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<ResumenCargaDW> EjecutarAsync(CancellationToken cancellationToken)
    {
        var resumen = new ResumenCargaDW { IdEjecucion = Guid.NewGuid() };
        List<ICargaDimension> cargasEnOrden = OrdenarSegunConfiguracion();

        _logger.LogInformation(
            "=== Carga de dimensiones {IdEjecucion}: {Cantidad} dimensión(es): {Dimensiones} ===",
            resumen.IdEjecucion, cargasEnOrden.Count, string.Join(", ", cargasEnOrden.Select(c => c.NombreDimension)));

        Stopwatch cronometro = Stopwatch.StartNew();

        foreach (ICargaDimension carga in cargasEnOrden)
        {
            ResumenCargaDimension resultado = await carga.CargarAsync(resumen.IdEjecucion, cancellationToken);
            resumen.Dimensiones.Add(resultado);

            await RegistrarEnBitacoraAsync(resultado, cancellationToken);
        }

        cronometro.Stop();
        resumen.DuracionTotalMs = cronometro.ElapsedMilliseconds;
        return resumen;
    }

    /// <summary>
    /// Devuelve las cargas registradas en el orden de la lista de appsettings.json.
    /// Las que no están en la lista no se ejecutan.
    /// </summary>
    private List<ICargaDimension> OrdenarSegunConfiguracion()
    {
        var resultado = new List<ICargaDimension>();

        foreach (string nombre in _opciones.Dimensiones)
        {
            ICargaDimension? carga = _cargas.FirstOrDefault(
                c => string.Equals(c.NombreDimension, nombre, StringComparison.OrdinalIgnoreCase));

            if (carga is null)
            {
                _logger.LogWarning("La dimensión '{Nombre}' de appsettings.json no existe. Se omite.", nombre);
                continue;
            }

            resultado.Add(carga);
        }

        return resultado;
    }

    /// <summary>
    /// Guarda el resultado en la bitácora. Si falla, solo se avisa en el log.
    /// </summary>
    private async Task RegistrarEnBitacoraAsync(ResumenCargaDimension resultado, CancellationToken cancellationToken)
    {
        try
        {
            await _logRepository.RegistrarAsync(resultado, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[{Dimension}] No se pudo guardar el resultado en la bitácora", resultado.Dimension);
        }
    }
}
