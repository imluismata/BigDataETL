namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Resultado de transformar UNA fila del origen:
///   - Fila con valor  -> la fila limpia, lista para cargar.
///   - Fila en null    -> la fila se rechaza; MotivoRechazo dice por qué.
/// </summary>
public class ResultadoTransformacion<T> where T : class
{
    public T? Fila { get; private set; }
    public string? MotivoRechazo { get; private set; }

    public bool EsValida => Fila is not null;

    public static ResultadoTransformacion<T> Valida(T fila)
    {
        return new ResultadoTransformacion<T> { Fila = fila };
    }

    public static ResultadoTransformacion<T> Rechazada(string motivo)
    {
        return new ResultadoTransformacion<T> { MotivoRechazo = motivo };
    }
}
