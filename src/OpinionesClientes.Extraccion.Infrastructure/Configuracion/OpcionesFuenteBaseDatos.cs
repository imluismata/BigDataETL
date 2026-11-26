namespace OpinionesClientes.Extraccion.Infrastructure.Configuracion;

/// <summary>
/// Configuración de la fuente base de datos relacional (reseñas web).
/// Sección en appsettings.json: "Fuentes:ResenasWeb".
/// </summary>
public class OpcionesFuenteBaseDatos
{
    public const string Seccion = "Fuentes:ResenasWeb";

    /// <summary>Nombre de la cadena de conexión dentro de "ConnectionStrings".</summary>
    public const string NombreCadenaConexion = "TiendaOnlineDB";

    public bool Habilitada { get; set; } = true;

    /// <summary>
    /// Cadena de conexión. NO se escribe en appsettings.json: se carga desde
    /// User Secrets (desarrollo) o variables de entorno (producción).
    /// </summary>
    public string CadenaConexion { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de la consulta, en segundos.</summary>
    public int TimeoutSegundos { get; set; } = 60;

    /// <summary>
    /// Extracción incremental opcional: solo reseñas desde esta fecha (yyyy-MM-dd).
    /// Vacío = se extraen todas.
    /// </summary>
    public string? FechaDesde { get; set; }
}
