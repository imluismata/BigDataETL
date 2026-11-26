namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimFuente: un canal por donde llegan las opiniones.
/// Se llena con los pares (TipoFuente, Canal) que existen en Opiniones.
/// </summary>
public class DimFuente
{
    public string CodigoTipoFuente { get; set; } = string.Empty;   // CSV, Web o Red Social (como en el origen)
    public string Canal { get; set; } = string.Empty;              // EncuestaInterna, SitioWeb, Instagram...
    public string TipoFuente { get; set; } = string.Empty;         // Encuesta interna, Reseña web, Red social
    public string MedioIngreso { get; set; } = string.Empty;       // Archivo CSV, Base de datos relacional, API REST
}
