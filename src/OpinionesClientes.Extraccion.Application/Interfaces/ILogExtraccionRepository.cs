using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Application.Interfaces;

/// <summary>
/// Guarda en la base de datos (tabla stg.LogExtraccion) el resultado
/// de cada fuente en cada ejecución. Complementa los logs de ILogger:
/// ILogger muestra lo que pasa en el momento, esta tabla deja un
/// historial que se puede consultar con SQL.
/// </summary>
public interface ILogExtraccionRepository
{
    Task RegistrarAsync(ResumenFuente resumen, CancellationToken cancellationToken);
}
