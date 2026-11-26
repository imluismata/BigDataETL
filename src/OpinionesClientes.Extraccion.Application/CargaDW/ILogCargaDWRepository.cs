namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Bitácora de la carga del DataWarehouse (esquema etl del DW):
///   - etl.LogCargaDimensiones    : una fila por dimensión y por ejecución.
///   - etl.ErroresCargaDimensiones: una fila por registro rechazado.
/// </summary>
public interface ILogCargaDWRepository
{
    Task RegistrarAsync(ResumenCargaDimension resumen, CancellationToken cancellationToken);
}
