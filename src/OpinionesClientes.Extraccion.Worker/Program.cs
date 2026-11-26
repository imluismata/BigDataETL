using OpinionesClientes.Extraccion.Infrastructure;
using OpinionesClientes.Extraccion.Worker;

// ---------------------------------------------------------------------
// Punto de entrada del Worker Service de extracción (E del ETL).
//
// Host.CreateApplicationBuilder carga la configuración en este orden
// (cada fuente sobrescribe a la anterior):
//   1. appsettings.json                 -> valores no sensibles
//   2. appsettings.{Entorno}.json       -> ajustes por entorno
//   3. User Secrets (solo Development)  -> contraseñas y API key en desarrollo
//   4. Variables de entorno             -> contraseñas y API key en producción
//   5. Argumentos de línea de comandos
// Así las credenciales nunca se escriben en appsettings.json ni se suben a GitHub.
// ---------------------------------------------------------------------
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Registrar extractores, repositorios de staging, opciones y orquestador
builder.Services.AgregarExtraccion(builder.Configuration);

// Registrar el servicio en segundo plano
builder.Services.AddHostedService<ExtraccionWorker>();

IHost host = builder.Build();
host.Run();
