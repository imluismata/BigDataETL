namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimCliente del DataWarehouse.
/// Se llena desde la tabla Clientes de OpinionesClientesDB.
/// </summary>
public class DimCliente
{
    public int ClienteId { get; set; }                 // clave de negocio (IdCliente del origen)
    public string NombreCliente { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string TipoCliente { get; set; } = "Registrado";
}
