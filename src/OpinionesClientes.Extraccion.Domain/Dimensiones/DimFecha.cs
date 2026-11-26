namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimFecha (un día del calendario).
/// </summary>
public class DimFecha
{
    public int FechaKey { get; set; }                 // AAAAMMDD
    public DateTime Fecha { get; set; }
    public short Anio { get; set; }
    public byte Trimestre { get; set; }
    public string NombreTrimestre { get; set; } = string.Empty;   // T1..T4
    public string AnioTrimestre { get; set; } = string.Empty;     // 2025-T1
    public byte Mes { get; set; }
    public string NombreMes { get; set; } = string.Empty;
    public string AnioMes { get; set; } = string.Empty;           // 2025-03
    public DateTime InicioSemana { get; set; }                    // lunes de esa semana
    public byte DiaMes { get; set; }
    public byte DiaSemana { get; set; }                           // 1 = Lunes ... 7 = Domingo
    public string NombreDia { get; set; } = string.Empty;
    public bool EsFinDeSemana { get; set; }
}
