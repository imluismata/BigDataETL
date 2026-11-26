namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimProducto. Se llena desde Productos + Categorias
/// de OpinionesClientesDB (la categoría se guarda desnormalizada).
/// </summary>
public class DimProducto
{
    public int ProductoId { get; set; }                // clave de negocio (IdProducto del origen)
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
}
