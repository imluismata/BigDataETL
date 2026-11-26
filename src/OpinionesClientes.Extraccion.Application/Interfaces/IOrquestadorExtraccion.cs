using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Application.Interfaces;

/// <summary>
/// Ejecuta la extracción de todas las fuentes habilitadas.
/// Es lo único que el Worker Service necesita conocer.
/// </summary>
public interface IOrquestadorExtraccion
{
    Task<ResumenEjecucion> EjecutarAsync(CancellationToken cancellationToken);
}
