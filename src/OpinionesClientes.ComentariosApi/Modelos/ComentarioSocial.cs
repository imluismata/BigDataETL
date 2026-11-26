namespace OpinionesClientes.ComentariosApi.Modelos;

/// <summary>
/// Un comentario de redes sociales tal como lo expone la API.
/// Las propiedades tienen el mismo nombre que las columnas del CSV
/// social_comments.csv, así CsvHelper las relaciona solo.
/// </summary>
public class ComentarioSocial
{
    public string IdComment { get; set; } = string.Empty;
    public string? IdCliente { get; set; }
    public string IdProducto { get; set; } = string.Empty;
    public string Fuente { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
}
