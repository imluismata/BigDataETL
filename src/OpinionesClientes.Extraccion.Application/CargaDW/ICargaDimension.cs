namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Contrato común para cargar UNA dimensión del DataWarehouse.
/// Hay una implementación por dimensión (DimCliente, DimProducto...).
/// Cada una hace su propio Extraer -> Transformar -> Cargar.
/// </summary>
public interface ICargaDimension
{
    /// <summary>Nombre de la tabla de la dimensión. Ej: "DimCliente".</summary>
    string NombreDimension { get; }

    Task<ResumenCargaDimension> CargarAsync(Guid idEjecucion, CancellationToken cancellationToken);
}
