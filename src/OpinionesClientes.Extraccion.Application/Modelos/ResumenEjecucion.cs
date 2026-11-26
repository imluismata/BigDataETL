namespace OpinionesClientes.Extraccion.Application.Modelos;

/// <summary>
/// Resultado de una ejecución completa (todas las fuentes).
/// </summary>
public class ResumenEjecucion
{
    public Guid IdEjecucion { get; set; }

    /// <summary>Tiempo total. Como las fuentes corren en paralelo,
    /// es menor que la suma de los tiempos de cada fuente.</summary>
    public long DuracionTotalMs { get; set; }

    public List<ResumenFuente> Fuentes { get; set; } = new();

    public int TotalRegistros => Fuentes.Sum(f => f.RegistrosExtraidos);

    public bool TodasExitosas => Fuentes.All(f => f.Exitosa);
}
