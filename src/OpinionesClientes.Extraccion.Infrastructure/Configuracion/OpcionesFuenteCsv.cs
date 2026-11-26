namespace OpinionesClientes.Extraccion.Infrastructure.Configuracion;

/// <summary>
/// Configuración de la fuente CSV (encuestas internas).
/// Sección en appsettings.json: "Fuentes:Encuestas".
/// </summary>
public class OpcionesFuenteCsv
{
    public const string Seccion = "Fuentes:Encuestas";

    /// <summary>Si es false, esta fuente no se registra ni se ejecuta.</summary>
    public bool Habilitada { get; set; } = true;

    /// <summary>
    /// Ruta del archivo. Puede ser absoluta o relativa a la carpeta
    /// del ejecutable (por defecto la carpeta "data" se copia allí).
    /// </summary>
    public string RutaArchivo { get; set; } = string.Empty;

    /// <summary>Separador de columnas del archivo.</summary>
    public string Delimitador { get; set; } = ",";
}
