using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;
using OpinionesClientes.Extraccion.Infrastructure.Extractores.Dtos;

namespace OpinionesClientes.Extraccion.Infrastructure.Extractores;

/// <summary>
/// Extrae los comentarios de redes sociales desde la API REST.
///
/// - Recibe un HttpClient ya configurado por IHttpClientFactory
///   (dirección base, timeout y encabezado X-Api-Key). Ver DependencyInjection.cs.
/// - La API devuelve los datos por páginas: se piden una tras otra hasta
///   llegar a la última.
/// - Si una llamada falla por un error temporal (red caída, error 5xx,
///   timeout), se reintenta con una espera cada vez mayor.
/// </summary>
public class ApiExtractor : IExtractor<ComentarioSocialExtraido>
{
    private readonly HttpClient _httpClient;
    private readonly OpcionesFuenteApi _opciones;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(HttpClient httpClient, IOptions<OpcionesFuenteApi> opciones, ILogger<ApiExtractor> logger)
    {
        _httpClient = httpClient;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public string NombreFuente => "ComentariosSociales";

    public string TipoFuente => "API";

    public string Origen => $"{_opciones.UrlBase.TrimEnd('/')}{_opciones.Endpoint}";

    public async IAsyncEnumerable<ComentarioSocialExtraido> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int pagina = 1;
        int totalPaginas;

        do
        {
            RespuestaPaginadaDto respuesta = await ObtenerPaginaConReintentosAsync(pagina, cancellationToken);

            _logger.LogDebug(
                "[{Fuente}] Página {Pagina} de {Total} recibida ({Cantidad} comentarios)",
                NombreFuente, pagina, respuesta.TotalPaginas, respuesta.Datos.Count);

            foreach (ComentarioApiDto dto in respuesta.Datos)
            {
                yield return Convertir(dto);
            }

            totalPaginas = respuesta.TotalPaginas;
            pagina++;
        }
        while (pagina <= totalPaginas);
    }

    /// <summary>
    /// Pide una página. Si falla por un error temporal, espera y reintenta
    /// (1 s, 2 s, 4 s...). Si es un error permanente (por ejemplo 401 por
    /// API key incorrecta) no tiene sentido reintentar y se lanza el error.
    /// </summary>
    private async Task<RespuestaPaginadaDto> ObtenerPaginaConReintentosAsync(int pagina, CancellationToken cancellationToken)
    {
        int intento = 1;

        while (true)
        {
            try
            {
                return await ObtenerPaginaAsync(pagina, cancellationToken);
            }
            catch (Exception ex) when (EsErrorTemporal(ex, cancellationToken) && intento <= _opciones.MaxReintentos)
            {
                TimeSpan espera = TimeSpan.FromSeconds(Math.Pow(2, intento - 1));

                _logger.LogWarning(
                    "[{Fuente}] Falló la página {Pagina} (intento {Intento} de {Max}): {Error}. Reintentando en {Espera} s...",
                    NombreFuente, pagina, intento, _opciones.MaxReintentos + 1, ex.Message, espera.TotalSeconds);

                await Task.Delay(espera, cancellationToken);
                intento++;
            }
        }
    }

    /// <summary>
    /// Hace UNA llamada GET a la API y lee el JSON de respuesta.
    /// </summary>
    private async Task<RespuestaPaginadaDto> ObtenerPaginaAsync(int pagina, CancellationToken cancellationToken)
    {
        string url = $"{_opciones.Endpoint}?pagina={pagina}&tamanoPagina={_opciones.TamanoPagina}";

        using HttpResponseMessage respuesta = await _httpClient.GetAsync(url, cancellationToken);

        // Lanza HttpRequestException si el código no es 2xx (401, 404, 500...)
        respuesta.EnsureSuccessStatusCode();

        RespuestaPaginadaDto? contenido = await respuesta.Content
            .ReadFromJsonAsync<RespuestaPaginadaDto>(cancellationToken);

        if (contenido is null)
        {
            throw new InvalidOperationException($"La API devolvió una respuesta vacía en la página {pagina}.");
        }

        return contenido;
    }

    /// <summary>
    /// Decide si vale la pena reintentar:
    ///   - Sí: sin conexión, timeout, 408, 429 o errores 5xx del servidor.
    ///   - No: 400, 401, 403, 404 (reintentar daría el mismo error).
    /// </summary>
    private static bool EsErrorTemporal(Exception ex, CancellationToken cancellationToken)
    {
        // Timeout del HttpClient (no es una cancelación pedida por el servicio)
        if (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (ex is HttpRequestException errorHttp)
        {
            // Sin código de estado = no hubo respuesta (servidor apagado, red caída)
            if (errorHttp.StatusCode is null)
            {
                return true;
            }

            HttpStatusCode codigo = errorHttp.StatusCode.Value;
            return codigo == HttpStatusCode.RequestTimeout
                || codigo == HttpStatusCode.TooManyRequests
                || (int)codigo >= 500;
        }

        return false;
    }

    /// <summary>
    /// Copia el DTO del JSON a la entidad del dominio.
    /// </summary>
    private static ComentarioSocialExtraido Convertir(ComentarioApiDto dto)
    {
        return new ComentarioSocialExtraido
        {
            IdComment = dto.IdComment,
            IdCliente = dto.IdCliente,
            IdProducto = dto.IdProducto,
            Fuente = dto.Fuente,
            Fecha = dto.Fecha,
            Comentario = dto.Comentario
        };
    }
}
