namespace OpinionesClientes.Extraccion.Application.Modelos;

/// <summary>
/// Resultado de extraer UNA fuente en una ejecución.
/// Se muestra en los logs y se guarda en stg.LogExtraccion.
/// </summary>
public class ResumenFuente
{
    public Guid IdEjecucion { get; set; }
    public string NombreFuente { get; set; } = string.Empty;
    public string TipoFuente { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    /// <summary>Tiempo que tardó la fuente, medido con Stopwatch.</summary>
    public long DuracionMs { get; set; }

    public int RegistrosExtraidos { get; set; }
    public int LotesGuardados { get; set; }

    public bool Exitosa { get; set; }
    public string? MensajeError { get; set; }

    /// <summary>Registros por segundo: indicador simple de rendimiento.</summary>
    public double RegistrosPorSegundo
    {
        get
        {
            if (DuracionMs <= 0)
            {
                return RegistrosExtraidos;
            }

            return RegistrosExtraidos / (DuracionMs / 1000.0);
        }
    }
}
