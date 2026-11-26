namespace OpinionesClientes.Extraccion.Infrastructure.Configuracion;

/// <summary>
/// Configuración de la base de datos de staging (OpinionesClientesStaging).
/// </summary>
public class OpcionesStaging
{
    /// <summary>Nombre de la cadena de conexión dentro de "ConnectionStrings".</summary>
    public const string NombreCadenaConexion = "StagingDB";

    /// <summary>Se carga desde User Secrets o variables de entorno.</summary>
    public string CadenaConexion { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de cada carga masiva, en segundos.</summary>
    public int TimeoutSegundos { get; set; } = 120;
}
