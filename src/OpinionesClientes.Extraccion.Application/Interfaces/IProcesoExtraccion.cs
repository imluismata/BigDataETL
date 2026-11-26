using OpinionesClientes.Extraccion.Application.Modelos;

namespace OpinionesClientes.Extraccion.Application.Interfaces;

/// <summary>
/// Representa el proceso completo de UNA fuente:
/// extraer -> guardar en staging -> devolver un resumen.
///
/// No es genérica a propósito: así el orquestador puede tener una
/// lista con todas las fuentes juntas, aunque cada una produzca un
/// tipo de registro distinto.
/// </summary>
public interface IProcesoExtraccion
{
    string NombreFuente { get; }

    Task<ResumenFuente> EjecutarAsync(Guid idEjecucion, CancellationToken cancellationToken);
}
