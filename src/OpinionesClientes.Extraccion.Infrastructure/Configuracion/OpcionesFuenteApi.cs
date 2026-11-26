namespace OpinionesClientes.Extraccion.Infrastructure.Configuracion;

/// <summary>
/// Configuración de la fuente API REST (comentarios de redes sociales).
/// Sección en appsettings.json: "Fuentes:ComentariosSociales".
/// </summary>
public class OpcionesFuenteApi
{
    public const string Seccion = "Fuentes:ComentariosSociales";

    public bool Habilitada { get; set; } = true;

    /// <summary>Dirección base de la API. Ej: https://localhost:7215</summary>
    public string UrlBase { get; set; } = string.Empty;

    /// <summary>Ruta del recurso. Ej: /api/comentarios</summary>
    public string Endpoint { get; set; } = "/api/comentarios";

    /// <summary>
    /// Clave de acceso que se envía en el encabezado X-Api-Key.
    /// NO se escribe en appsettings.json: va en User Secrets o variables de entorno.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Registros que se piden por página.</summary>
    public int TamanoPagina { get; set; } = 50;

    /// <summary>Tiempo máximo de espera de cada llamada, en segundos.</summary>
    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>Cuántas veces se reintenta una llamada que falló por un error temporal.</summary>
    public int MaxReintentos { get; set; } = 3;
}
