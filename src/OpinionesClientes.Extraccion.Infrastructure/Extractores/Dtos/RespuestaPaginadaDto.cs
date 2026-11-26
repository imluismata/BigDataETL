namespace OpinionesClientes.Extraccion.Infrastructure.Extractores.Dtos;

/// <summary>
/// Forma del JSON que devuelve la API de comentarios en cada página:
/// {
///   "pagina": 1, "tamanoPagina": 50, "totalRegistros": 200, "totalPaginas": 4,
///   "datos": [ { ...comentario... }, ... ]
/// }
/// </summary>
public class RespuestaPaginadaDto
{
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public List<ComentarioApiDto> Datos { get; set; } = new();
}
