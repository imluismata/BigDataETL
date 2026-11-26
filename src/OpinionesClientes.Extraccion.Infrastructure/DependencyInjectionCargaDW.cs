using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Infrastructure.CargaDW;
using OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure;

/// <summary>
/// Registra todo lo que necesita la carga de dimensiones del DataWarehouse.
/// El Worker CargaDW solo llama a AgregarCargaDimensiones().
///
/// Para agregar una dimensión nueva: crear su clase en CargaDW/Dimensiones
/// (hereda de CargaDimensionBase), registrarla aquí y poner su nombre en
/// la lista CargaDW:Dimensiones de appsettings.json.
/// </summary>
public static class DependencyInjectionCargaDW
{
    public static IServiceCollection AgregarCargaDimensiones(this IServiceCollection services, IConfiguration configuration)
    {
        // Opciones generales: intervalo y lista (con orden) de dimensiones
        services.AddOptions<OpcionesCargaDW>()
            .Bind(configuration.GetSection(OpcionesCargaDW.Seccion))
            .Validate(o => o.IntervaloMinutos >= 0, "CargaDW:IntervaloMinutos no puede ser negativo.")
            .Validate(o => o.Dimensiones.Count > 0, "CargaDW:Dimensiones debe tener al menos una dimensión.")
            .ValidateOnStart();

        // Conexiones: vienen de User Secrets o variables de entorno, nunca de appsettings.json
        services.AddOptions<OpcionesConexionesDW>()
            .Configure(o =>
            {
                o.CadenaOrigen = configuration.GetConnectionString(OpcionesConexionesDW.NombreCadenaOrigen) ?? string.Empty;
                o.CadenaDataWarehouse = configuration.GetConnectionString(OpcionesConexionesDW.NombreCadenaDataWarehouse) ?? string.Empty;
                o.TimeoutSegundos = configuration.GetValue("CargaDW:TimeoutSegundos", 120);
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaOrigen),
                "Falta ConnectionStrings:OpinionesClientesDB. Configúrela con 'dotnet user-secrets' o con una variable de entorno.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaDataWarehouse),
                "Falta ConnectionStrings:OpinionesClientesDW. Configúrela con 'dotnet user-secrets' o con una variable de entorno.")
            .ValidateOnStart();

        // Una clase por dimensión, todas con la misma interfaz
        services.AddScoped<ICargaDimension, DimFechaCarga>();
        services.AddScoped<ICargaDimension, DimSentimientoCarga>();
        services.AddScoped<ICargaDimension, DimFuenteCarga>();
        services.AddScoped<ICargaDimension, DimPalabraClaveCarga>();
        services.AddScoped<ICargaDimension, DimProductoCarga>();
        services.AddScoped<ICargaDimension, DimClienteCarga>();

        services.AddScoped<ILogCargaDWRepository, LogCargaDWRepository>();
        services.AddScoped<IOrquestadorCargaDimensiones, OrquestadorCargaDimensiones>();

        return services;
    }
}
