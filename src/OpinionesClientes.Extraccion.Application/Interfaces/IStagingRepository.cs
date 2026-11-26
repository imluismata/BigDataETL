namespace OpinionesClientes.Extraccion.Application.Interfaces;

/// <summary>
/// Guarda los registros extraídos en su tabla de staging (esquema stg).
/// Hay una implementación por tabla: Encuestas, ResenasWeb y ComentariosSociales.
/// </summary>
public interface IStagingRepository<T>
{
    /// <summary>
    /// Vacía la tabla de staging antes de una nueva extracción
    /// (staging solo guarda la última extracción de cada fuente).
    /// </summary>
    Task LimpiarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inserta un lote de registros de una sola vez (carga masiva).
    /// Devuelve cuántas filas se insertaron.
    /// </summary>
    Task<int> GuardarLoteAsync(
        IReadOnlyList<T> lote,
        Guid idEjecucion,
        string origen,
        CancellationToken cancellationToken);
}
