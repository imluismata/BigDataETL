namespace OpinionesClientes.ComentariosApi.Modelos;

/// <summary>
/// Respuesta de cada página. Paginar evita mandar miles de registros
/// en una sola respuesta: el cliente pide página por página.
/// </summary>
public class RespuestaPaginada<T>
{
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public List<T> Datos { get; set; } = new();
}
