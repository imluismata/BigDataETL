namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Opciones generales de la carga de dimensiones (sección "CargaDW" de appsettings.json).
/// </summary>
public class OpcionesCargaDW
{
    public const string Seccion = "CargaDW";

    /// <summary>0 = una sola ejecución. Mayor que 0 = repetir cada N minutos.</summary>
    public int IntervaloMinutos { get; set; } = 0;

    /// <summary>
    /// Dimensiones que se cargan y en qué orden. Para no cargar una,
    /// basta con quitarla de la lista.
    /// </summary>
    public List<string> Dimensiones { get; set; } = new();
}
