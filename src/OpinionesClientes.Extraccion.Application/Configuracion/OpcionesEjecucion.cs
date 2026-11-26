namespace OpinionesClientes.Extraccion.Application.Configuracion;

/// <summary>
/// Opciones generales del proceso. Se leen de la sección "Ejecucion"
/// de appsettings.json.
/// </summary>
public class OpcionesEjecucion
{
    public const string Seccion = "Ejecucion";

    /// <summary>
    /// Cada cuántos minutos se repite la extracción.
    /// 0 = se ejecuta una sola vez y el Worker se detiene.
    /// </summary>
    public int IntervaloMinutos { get; set; } = 0;

    /// <summary>
    /// Cantidad de registros que se acumulan antes de guardarlos en staging.
    /// Lotes grandes = menos viajes a la base de datos.
    /// </summary>
    public int TamanoLote { get; set; } = 1000;

    /// <summary>
    /// Si es true, se vacía la tabla de staging de cada fuente antes de extraer.
    /// </summary>
    public bool LimpiarStagingAntesDeExtraer { get; set; } = true;
}
