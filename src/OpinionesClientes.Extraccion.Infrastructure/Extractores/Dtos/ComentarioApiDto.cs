namespace OpinionesClientes.Extraccion.Infrastructure.Extractores.Dtos;

/// <summary>
/// Un comentario tal como viene en el JSON de la API.
/// Es un DTO (objeto de transferencia): solo existe para leer el JSON.
/// Luego se copia a ComentarioSocialExtraido (entidad del dominio).
/// </summary>
public class ComentarioApiDto
{
    public string? IdComment { get; set; }
    public string? IdCliente { get; set; }
    public string? IdProducto { get; set; }
    public string? Fuente { get; set; }
    public string? Fecha { get; set; }
    public string? Comentario { get; set; }
}
