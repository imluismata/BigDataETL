namespace OpinionesClientes.Extraccion.Domain.Entidades;

/// <summary>
/// Una reseña del sitio web, leída desde la base de datos relacional
/// de la tienda en línea (TiendaOnlineDB, tabla dbo.Resenas).
///
/// Igual que las demás entidades extraídas, todo se guarda como texto:
/// la conversión de tipos se hace en la fase de transformación.
/// </summary>
public class ResenaWebExtraida
{
    public string? IdReview { get; set; }
    public string? IdCliente { get; set; }
    public string? IdProducto { get; set; }
    public string? Fecha { get; set; }
    public string? Comentario { get; set; }
    public string? Rating { get; set; }
}
