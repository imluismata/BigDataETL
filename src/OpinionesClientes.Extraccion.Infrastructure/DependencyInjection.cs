using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Configuracion;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Application.Servicios;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;
using OpinionesClientes.Extraccion.Infrastructure.Extractores;
using OpinionesClientes.Extraccion.Infrastructure.Logging;
using OpinionesClientes.Extraccion.Infrastructure.Staging;

namespace OpinionesClientes.Extraccion.Infrastructure;

/// <summary>
/// Registra en el contenedor de inyección de dependencias todo lo que
/// necesita la extracción. Program.cs solo llama a AgregarExtraccion().
///
/// Configuración modular: cada fuente se registra SOLO si en appsettings.json
/// tiene "Habilitada": true. Para agregar una fuente nueva:
///   1. Crear su entidad, su extractor (IExtractor) y su repositorio de staging.
///   2. Agregar su sección en appsettings.json.
///   3. Agregar un método Registrar...() aquí.
/// El orquestador y el Worker no se tocan.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AgregarExtraccion(this IServiceCollection services, IConfiguration configuration)
    {
        RegistrarOpcionesGenerales(services, configuration);
        RegistrarStaging(services, configuration);

        RegistrarFuenteCsv(services, configuration);
        RegistrarFuenteBaseDatos(services, configuration);
        RegistrarFuenteApi(services, configuration);

        // El orquestador recibe todos los IProcesoExtraccion registrados arriba
        services.AddScoped<IOrquestadorExtraccion, OrquestadorExtraccion>();

        return services;
    }

    private static void RegistrarOpcionesGenerales(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpcionesEjecucion>()
            .Bind(configuration.GetSection(OpcionesEjecucion.Seccion))
            .Validate(o => o.TamanoLote > 0, "Ejecucion:TamanoLote debe ser mayor que 0.")
            .Validate(o => o.IntervaloMinutos >= 0, "Ejecucion:IntervaloMinutos no puede ser negativo.")
            .ValidateOnStart();
    }

    private static void RegistrarStaging(IServiceCollection services, IConfiguration configuration)
    {
        // La cadena de conexión se toma de "ConnectionStrings:StagingDB".
        // Su valor real viene de User Secrets o de una variable de entorno.
        services.AddOptions<OpcionesStaging>()
            .Configure(o => o.CadenaConexion = configuration.GetConnectionString(OpcionesStaging.NombreCadenaConexion) ?? string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaConexion),
                "Falta ConnectionStrings:StagingDB. Configúrela con 'dotnet user-secrets' o con una variable de entorno.")
            .ValidateOnStart();

        services.AddScoped<ILogExtraccionRepository, LogExtraccionRepository>();
    }

    /// <summary>Fuente 1: archivo CSV de encuestas internas.</summary>
    private static void RegistrarFuenteCsv(IServiceCollection services, IConfiguration configuration)
    {
        if (!EstaHabilitada(configuration, OpcionesFuenteCsv.Seccion))
        {
            return;
        }

        services.AddOptions<OpcionesFuenteCsv>()
            .Bind(configuration.GetSection(OpcionesFuenteCsv.Seccion))
            .Validate(o => !string.IsNullOrWhiteSpace(o.RutaArchivo), "Fuentes:Encuestas:RutaArchivo es obligatorio.")
            .ValidateOnStart();

        services.AddScoped<IExtractor<EncuestaExtraida>, CsvExtractor>();
        services.AddScoped<IStagingRepository<EncuestaExtraida>, EncuestasStagingRepository>();
        services.AddScoped<IProcesoExtraccion, ProcesoExtraccion<EncuestaExtraida>>();
    }

    /// <summary>Fuente 2: base de datos relacional de la tienda en línea.</summary>
    private static void RegistrarFuenteBaseDatos(IServiceCollection services, IConfiguration configuration)
    {
        if (!EstaHabilitada(configuration, OpcionesFuenteBaseDatos.Seccion))
        {
            return;
        }

        services.AddOptions<OpcionesFuenteBaseDatos>()
            .Bind(configuration.GetSection(OpcionesFuenteBaseDatos.Seccion))
            .Configure(o => o.CadenaConexion = configuration.GetConnectionString(OpcionesFuenteBaseDatos.NombreCadenaConexion) ?? string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaConexion),
                "Falta ConnectionStrings:TiendaOnlineDB. Configúrela con 'dotnet user-secrets' o con una variable de entorno.")
            .ValidateOnStart();

        services.AddScoped<IExtractor<ResenaWebExtraida>, DatabaseExtractor>();
        services.AddScoped<IStagingRepository<ResenaWebExtraida>, ResenasWebStagingRepository>();
        services.AddScoped<IProcesoExtraccion, ProcesoExtraccion<ResenaWebExtraida>>();
    }

    /// <summary>Fuente 3: API REST de comentarios de redes sociales.</summary>
    private static void RegistrarFuenteApi(IServiceCollection services, IConfiguration configuration)
    {
        if (!EstaHabilitada(configuration, OpcionesFuenteApi.Seccion))
        {
            return;
        }

        services.AddOptions<OpcionesFuenteApi>()
            .Bind(configuration.GetSection(OpcionesFuenteApi.Seccion))
            .Validate(o => Uri.TryCreate(o.UrlBase, UriKind.Absolute, out _), "Fuentes:ComentariosSociales:UrlBase no es una URL válida.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey),
                "Falta Fuentes:ComentariosSociales:ApiKey. Configúrela con 'dotnet user-secrets' o con una variable de entorno.")
            .Validate(o => o.TamanoPagina is > 0 and <= 500, "Fuentes:ComentariosSociales:TamanoPagina debe estar entre 1 y 500.")
            .ValidateOnStart();

        // IHttpClientFactory: crea y recicla las conexiones HTTP de forma segura
        // (evita agotar los sockets, que pasa al hacer "new HttpClient()" muchas veces).
        services.AddHttpClient<IExtractor<ComentarioSocialExtraido>, ApiExtractor>((proveedor, cliente) =>
        {
            OpcionesFuenteApi opciones = proveedor.GetRequiredService<IOptions<OpcionesFuenteApi>>().Value;

            cliente.BaseAddress = new Uri(opciones.UrlBase);
            cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
            cliente.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // La API key va en un encabezado, nunca en la URL (las URL quedan en logs)
            cliente.DefaultRequestHeaders.Add("X-Api-Key", opciones.ApiKey);
        });

        services.AddScoped<IStagingRepository<ComentarioSocialExtraido>, ComentariosSocialesStagingRepository>();
        services.AddScoped<IProcesoExtraccion, ProcesoExtraccion<ComentarioSocialExtraido>>();
    }

    /// <summary>
    /// Lee "Habilitada" de la sección de la fuente. Si no está escrita, se asume true.
    /// </summary>
    private static bool EstaHabilitada(IConfiguration configuration, string seccion)
    {
        return configuration.GetSection(seccion).GetValue("Habilitada", true);
    }
}
