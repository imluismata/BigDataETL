using System.Security.Cryptography;
using System.Text;

namespace OpinionesClientes.ComentariosApi.Seguridad;

/// <summary>
/// Protege los endpoints: solo responde si la petición trae el encabezado
/// X-Api-Key con la clave correcta. Si no, devuelve 401 (no autorizado).
/// </summary>
public class FiltroApiKey : IEndpointFilter
{
    public const string NombreEncabezado = "X-Api-Key";

    private readonly string _apiKeyEsperada;

    public FiltroApiKey(IConfiguration configuration)
    {
        // La clave real viene de User Secrets o de una variable de entorno
        _apiKeyEsperada = configuration["ComentariosApi:ApiKey"] ?? string.Empty;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext contexto, EndpointFilterDelegate siguiente)
    {
        bool traeClave = contexto.HttpContext.Request.Headers.TryGetValue(NombreEncabezado, out var claveRecibida);

        if (!traeClave || !SonIguales(claveRecibida.ToString(), _apiKeyEsperada))
        {
            return Results.Unauthorized();
        }

        return await siguiente(contexto);
    }

    /// <summary>
    /// Compara en tiempo constante (FixedTimeEquals) para no dar pistas
    /// sobre la clave según lo que tarda la comparación.
    /// </summary>
    private static bool SonIguales(string recibida, string esperada)
    {
        if (string.IsNullOrEmpty(esperada))
        {
            // Si el servidor no tiene clave configurada, no se deja pasar a nadie
            return false;
        }

        byte[] bytesRecibida = Encoding.UTF8.GetBytes(recibida);
        byte[] bytesEsperada = Encoding.UTF8.GetBytes(esperada);
        return CryptographicOperations.FixedTimeEquals(bytesRecibida, bytesEsperada);
    }
}
