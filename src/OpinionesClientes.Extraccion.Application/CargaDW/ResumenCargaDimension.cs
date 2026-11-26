namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Resultado de cargar UNA dimensión en una ejecución.
/// Se muestra en el log y se guarda en etl.LogCargaDimensiones.
/// </summary>
public class ResumenCargaDimension
{
    public Guid IdEjecucion { get; set; }
    public string Dimension { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public long DuracionMs { get; set; }

    /// <summary>Filas leídas del origen (Extracción).</summary>
    public int RegistrosLeidos { get; set; }

    /// <summary>Filas nuevas que se agregaron a la dimensión.</summary>
    public int Insertados { get; set; }

    /// <summary>Filas que ya existían y cambiaron (SCD tipo 1: se sobrescriben).</summary>
    public int Actualizados { get; set; }

    /// <summary>Filas rechazadas en la transformación, con su motivo.</summary>
    public List<RegistroRechazado> Rechazos { get; set; } = new();

    public int Rechazados => Rechazos.Count;

    /// <summary>Filas que ya estaban en la dimensión exactamente iguales.</summary>
    public int SinCambios => Math.Max(0, RegistrosLeidos - Rechazados - Insertados - Actualizados);

    public bool Exitosa { get; set; }
    public string? MensajeError { get; set; }
}
