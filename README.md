# Opiniones de Clientes - Proceso de Extracción (ETL)

Proyecto de la materia **Introducción al Big Data** (ITLA).

Es la primera fase (la "E") del proceso ETL del Sistema de Análisis de Opiniones de Clientes.
Un Worker Service en .NET 8 lee las opiniones desde tres fuentes y las guarda en tablas de
staging en SQL Server, listas para la transformación.

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

## Autor

Luis Miguel Mata Miranda - 2017-5359
