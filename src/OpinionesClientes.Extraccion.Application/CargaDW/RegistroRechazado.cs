namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Una fila del origen que no se pudo cargar en la dimensión, con su motivo.
/// Se guarda en etl.ErroresCargaDimensiones para que TI la revise.
/// </summary>
public class RegistroRechazado
{
    public string ClaveNegocio { get; set; } = string.Empty;   // Ej: IdCliente = 25
    public string Motivo { get; set; } = string.Empty;
}
