namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Ejecuta la carga de todas las dimensiones habilitadas, en el orden configurado.
/// </summary>
public interface IOrquestadorCargaDimensiones
{
    Task<ResumenCargaDW> EjecutarAsync(CancellationToken cancellationToken);
}
