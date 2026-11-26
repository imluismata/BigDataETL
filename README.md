# Opiniones de Clientes - Proceso ETL

Proyecto de la materia **Introducción al Big Data** (ITLA).

Proceso ETL del Sistema de Análisis de Opiniones de Clientes, en .NET 8:

1. **Extracción:** un Worker lee las opiniones desde tres fuentes (CSV, base de datos y API)
   y las guarda en tablas de staging.
2. **Carga de dimensiones:** otro Worker carga las dimensiones del DataWarehouse
   `OpinionesClientesDW` desde la base transaccional `OpinionesClientesDB`.

## Fuentes de datos

| Fuente | Tipo | Registros |
|---|---|---|
| Encuestas internas (`surveys_part1.csv`) | Archivo CSV | 500 |
| Reseñas del sitio web (`TiendaOnlineDB`) | Base de datos SQL Server | 200 |
| Comentarios de redes sociales (`/api/comentarios`) | API REST | 200 |

## Tecnologías

- .NET 8 (Worker Service y Minimal API)
- SQL Server 2022 (Docker)
- CsvHelper
- ADO.NET (Microsoft.Data.SqlClient)

## Estructura

```
├── src/
│   ├── OpinionesClientes.Extraccion.Domain          Entidades
│   ├── OpinionesClientes.Extraccion.Application     Interfaces y flujo de extracción
│   ├── OpinionesClientes.Extraccion.Infrastructure  Extractores (CSV, BD, API) y staging
│   ├── OpinionesClientes.Extraccion.Worker          Servicio que ejecuta la extracción
│   ├── OpinionesClientes.CargaDW.Worker             Servicio que carga las dimensiones del DW
│   └── OpinionesClientes.ComentariosApi             API de comentarios (fuente 3)
├── database/   Scripts SQL
├── data/       Archivo CSV de encuestas
└── docs/       Diagramas
```

## Cómo ejecutarlo

**1. Crear las bases de datos.** Ejecutar en SQL Server, en orden, los scripts de `database/`:

```
01_CrearFuenteTiendaOnlineDB.sql
02_CrearStaging.sql
03_CrearUsuarioEtl.sql
```

**2. Configurar las credenciales.** No están en `appsettings.json`; se guardan con User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:TiendaOnlineDB" "Server=localhost,1433;Database=TiendaOnlineDB;User Id=etl_extraccion;Password=<clave>;TrustServerCertificate=True" --project src/OpinionesClientes.Extraccion.Worker
dotnet user-secrets set "ConnectionStrings:StagingDB" "Server=localhost,1433;Database=OpinionesClientesStaging;User Id=etl_extraccion;Password=<clave>;TrustServerCertificate=True" --project src/OpinionesClientes.Extraccion.Worker
dotnet user-secrets set "Fuentes:ComentariosSociales:ApiKey" "<api-key>" --project src/OpinionesClientes.Extraccion.Worker
dotnet user-secrets set "ComentariosApi:ApiKey" "<api-key>" --project src/OpinionesClientes.ComentariosApi
```

**3. Ejecutar** la API y, en otra terminal, el Worker:

```bash
dotnet run --project src/OpinionesClientes.ComentariosApi --launch-profile https
dotnet run --project src/OpinionesClientes.Extraccion.Worker
```

Al terminar, el Worker muestra cuántos registros extrajo de cada fuente. Para revisar los datos
en staging se puede usar `database/04_ConsultasValidacion.sql`.

## Carga de dimensiones del DataWarehouse

Carga `DimFecha`, `DimSentimiento`, `DimFuente`, `DimPalabraClave`, `DimProducto` y `DimCliente`.
Cada dimensión se extrae de `OpinionesClientesDB`, se limpia en C# y se carga con un `MERGE`
(inserta lo nuevo y actualiza lo que cambió), así que se puede ejecutar varias veces sin duplicar.

**1.** Con `OpinionesClientesDB` y `OpinionesClientesDW` ya creadas, ejecutar `database/05_PrepararCargaDW.sql`.

**2.** Configurar las conexiones:

```bash
dotnet user-secrets set "ConnectionStrings:OpinionesClientesDB" "Server=localhost,1433;Database=OpinionesClientesDB;User Id=etl_extraccion;Password=<clave>;TrustServerCertificate=True" --project src/OpinionesClientes.CargaDW.Worker
dotnet user-secrets set "ConnectionStrings:OpinionesClientesDW" "Server=localhost,1433;Database=OpinionesClientesDW;User Id=etl_extraccion;Password=<clave>;TrustServerCertificate=True" --project src/OpinionesClientes.CargaDW.Worker
```

**3.** Ejecutar:

```bash
dotnet run --project src/OpinionesClientes.CargaDW.Worker
```

El resultado se revisa con `database/06_ConsultasDimensiones.sql`.

## Autor

Luis Miguel Mata Miranda - 2017-5359
