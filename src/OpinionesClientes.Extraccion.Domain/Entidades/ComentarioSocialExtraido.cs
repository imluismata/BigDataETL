namespace OpinionesClientes.Extraccion.Domain.Entidades;

/// <summary>
/// Un comentario de redes sociales, recibido desde la API REST de comentarios.
///
/// IdCliente puede venir vacío: en redes sociales hay comentarios anónimos.
/// </summary>
public class ComentarioSocialExtraido
{
    public string? IdComment { get; set; }
    public string? IdCliente { get; set; }
    public string? IdProducto { get; set; }
    public string? Fuente { get; set; }      // Red social: Twitter, Instagram, Facebook...
    public string? Fecha { get; set; }
    public string? Comentario { get; set; }
}
