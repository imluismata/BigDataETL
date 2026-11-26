using OpinionesClientes.CargaDW.Worker;
using OpinionesClientes.Extraccion.Infrastructure;

// ---------------------------------------------------------------------
// Worker de CARGA DE DIMENSIONES del DataWarehouse OpinionesClientesDW.
//
// Origen : OpinionesClientesDB (base transaccional que llena el ETL)
// Destino: DimFecha, DimSentimiento, DimFuente, DimPalabraClave,
//          DimProducto y DimCliente
//
// Las cadenas de conexión se cargan desde User Secrets (desarrollo) o
// variables de entorno (producción); appsettings.json no tiene credenciales.
// ---------------------------------------------------------------------
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarCargaDimensiones(builder.Configuration);
builder.Services.AddHostedService<CargaDimensionesWorker>();

IHost host = builder.Build();
host.Run();
