namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimSentimiento. Se llena desde Clasificaciones
/// y usa los mismos Id (1 Positiva, 2 Negativa, 3 Neutra).
/// </summary>
public class DimSentimiento
{
    public int SentimientoKey { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public short ValorSentimiento { get; set; }         // +1, -1 o 0
}
