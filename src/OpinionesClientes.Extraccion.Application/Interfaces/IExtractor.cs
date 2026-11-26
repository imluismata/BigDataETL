namespace OpinionesClientes.Extraccion.Application.Interfaces;

/// <summary>
/// Contrato común para TODAS las fuentes de datos (CSV, base de datos, API...).
///
/// T es el tipo de registro que produce la fuente
/// (EncuestaExtraida, ResenaWebExtraida o ComentarioSocialExtraido).
///
/// Gracias a esta interfaz, el resto del sistema no sabe (ni le importa)
/// de dónde salen los datos. Para agregar una fuente nueva solo hay que
/// crear otra clase que implemente IExtractor (principio Abierto/Cerrado).
/// </summary>
public interface IExtractor<T>
{
    /// <summary>Nombre corto de la fuente, para los logs. Ej: "Encuestas".</summary>
    string NombreFuente { get; }

    /// <summary>Tipo de fuente: "CSV", "BaseDatos" o "API".</summary>
    string TipoFuente { get; }

    /// <summary>
    /// Descripción del origen exacto: nombre del archivo, tabla o URL.
    /// Se guarda en staging para saber de dónde vino cada fila.
    /// </summary>
    string Origen { get; }

    /// <summary>
    /// Devuelve los registros UNO POR UNO a medida que se leen
    /// (IAsyncEnumerable). Así no hace falta tener todo el archivo,
    /// la tabla o la API completa en memoria: se procesa por lotes.
    /// </summary>
    IAsyncEnumerable<T> ExtraerAsync(CancellationToken cancellationToken);
}
