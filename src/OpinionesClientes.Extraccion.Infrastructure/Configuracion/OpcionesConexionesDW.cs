namespace OpinionesClientes.Extraccion.Infrastructure.Configuracion;

/// <summary>
/// Conexiones de la carga del DataWarehouse.
/// Las cadenas NO van en appsettings.json: se cargan desde User Secrets
/// (desarrollo) o variables de entorno (producción).
/// </summary>
public class OpcionesConexionesDW
{
    /// <summary>Nombre de la cadena de la base transaccional (origen).</summary>
    public const string NombreCadenaOrigen = "OpinionesClientesDB";

    /// <summary>Nombre de la cadena del DataWarehouse (destino).</summary>
    public const string NombreCadenaDataWarehouse = "OpinionesClientesDW";

    public string CadenaOrigen { get; set; } = string.Empty;
    public string CadenaDataWarehouse { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de cada consulta o carga, en segundos.</summary>
    public int TimeoutSegundos { get; set; } = 120;
}
