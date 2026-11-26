namespace OpinionesClientes.Extraccion.Domain.Entidades;

/// <summary>
/// Una fila del archivo CSV de encuestas internas (surveys_part1.csv),
/// tal como viene en el archivo.
///
/// Todas las propiedades son texto (string) a propósito: en la fase de
/// extracción NO se convierte ni se limpia nada. Si un dato viene mal
/// (por ejemplo, una fecha inválida), igual se guarda en staging y la
/// fase de transformación decide qué hacer con él.
/// </summary>
public class EncuestaExtraida
{
    // Columnas del archivo CSV
    public string? IdOpinion { get; set; }
    public string? IdCliente { get; set; }
    public string? IdProducto { get; set; }
    public string? Fecha { get; set; }
    public string? Comentario { get; set; }
    public string? Clasificacion { get; set; }
    public string? PuntajeSatisfaccion { get; set; }
    public string? Fuente { get; set; }

    // Dato de trazabilidad: número de fila dentro del archivo.
    // Sirve para ubicar rápido una fila con problemas.
    public int NumeroFila { get; set; }
}
