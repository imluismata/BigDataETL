using OpinionesClientes.ComentariosApi.Modelos;
using OpinionesClientes.ComentariosApi.Repositorio;
using OpinionesClientes.ComentariosApi.Seguridad;

// ---------------------------------------------------------------------
// API REST de comentarios de redes sociales (Minimal API).
// Simula el servicio externo del que el Worker extrae la fuente 3.
//
//   GET /api/comentarios?pagina=1&tamanoPagina=50
//   Encabezado obligatorio: X-Api-Key
// ---------------------------------------------------------------------
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RepositorioComentariosCsv>();
builder.Services.AddSingleton<FiltroApiKey>();

WebApplication app = builder.Build();

// Redirige HTTP a HTTPS: la API key no debe viajar sin cifrar
app.UseHttpsRedirection();

app.MapGet("/api/comentarios", (int? pagina, int? tamanoPagina, RepositorioComentariosCsv repositorio) =>
    {
        int numeroPagina = pagina ?? 1;
        int tamano = tamanoPagina ?? 50;

        // Validar los parámetros de entrada
        if (numeroPagina < 1 || tamano < 1 || tamano > 500)
        {
            return Results.BadRequest(new { error = "pagina debe ser >= 1 y tamanoPagina debe estar entre 1 y 500." });
        }

        (List<ComentarioSocial> datos, int total) = repositorio.ObtenerPagina(numeroPagina, tamano);

        var respuesta = new RespuestaPaginada<ComentarioSocial>
        {
            Pagina = numeroPagina,
            TamanoPagina = tamano,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamano),
            Datos = datos
        };

        return Results.Ok(respuesta);
    })
    .AddEndpointFilter<FiltroApiKey>();

app.Run();
