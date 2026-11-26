using System.Globalization;
using CsvHelper;
using OpinionesClientes.ComentariosApi.Modelos;

namespace OpinionesClientes.ComentariosApi.Repositorio;

/// <summary>
/// Origen de datos de la API: lee social_comments.csv una sola vez
/// (la primera vez que se necesita) y lo mantiene en memoria.
///
/// Esta API simula el servicio externo de redes sociales. En un caso real
/// los datos vendrían de la plataforma de cada red social.
/// </summary>
public class RepositorioComentariosCsv
{
    private readonly Lazy<List<ComentarioSocial>> _comentarios;

    public RepositorioComentariosCsv(IConfiguration configuration, IWebHostEnvironment entorno)
    {
        string rutaRelativa = configuration["ComentariosApi:RutaArchivo"] ?? "Data/social_comments.csv";
        string rutaCompleta = Path.Combine(entorno.ContentRootPath, rutaRelativa);

        // Lazy: el archivo se lee solo cuando llega la primera petición
        _comentarios = new Lazy<List<ComentarioSocial>>(() => LeerArchivo(rutaCompleta));
    }

    /// <summary>Devuelve una página de comentarios y el total de registros.</summary>
    public (List<ComentarioSocial> Pagina, int Total) ObtenerPagina(int pagina, int tamanoPagina)
    {
        List<ComentarioSocial> todos = _comentarios.Value;

        List<ComentarioSocial> datos = todos
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (datos, todos.Count);
    }

    private static List<ComentarioSocial> LeerArchivo(string ruta)
    {
        using var lector = new StreamReader(ruta);
        using var csv = new CsvReader(lector, CultureInfo.InvariantCulture);
        return csv.GetRecords<ComentarioSocial>().ToList();
    }
}
