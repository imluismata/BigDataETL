namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Resultado de una ejecución completa de la carga de dimensiones.
/// </summary>
public class ResumenCargaDW
{
    public Guid IdEjecucion { get; set; }
    public long DuracionTotalMs { get; set; }
    public List<ResumenCargaDimension> Dimensiones { get; set; } = new();

    public bool TodasExitosas => Dimensiones.All(d => d.Exitosa);
}
