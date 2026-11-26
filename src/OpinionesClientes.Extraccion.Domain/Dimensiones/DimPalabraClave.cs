namespace OpinionesClientes.Extraccion.Domain.Dimensiones;

/// <summary>
/// Fila de la dimensión DimPalabraClave. Se llena desde PalabrasClave
/// (diccionario del clasificador) unida con Clasificaciones.
/// </summary>
public class DimPalabraClave
{
    public string Palabra { get; set; } = string.Empty;      // clave de negocio
    public string TipoPalabra { get; set; } = string.Empty;  // Positiva, Negativa o Neutra
}
