/* =====================================================================
   Proyecto : Sistema de Análisis de Opiniones de Clientes con Proceso ETL
   Materia  : Introducción al Big Data - ITLA
   Autor    : Luis Miguel Mata Miranda (2017-5359)
   Archivo  : 05_PrepararCargaDW.sql

   Prepara el DataWarehouse para la carga de dimensiones:
     1. Esquema etl con la bitácora de la carga:
          etl.LogCargaDimensiones     -> una fila por dimensión y ejecución
          etl.ErroresCargaDimensiones -> filas rechazadas con su motivo
     2. Permisos mínimos para el login etl_extraccion (creado en 03):
          OpinionesClientesDB : solo lectura
          OpinionesClientesDW : lectura y escritura (sin crear ni borrar tablas)

   Requisitos: OpinionesClientesDB (ETL anterior) y OpinionesClientesDW
   (script de la Actividad 3.1) ya creadas, y 03_CrearUsuarioEtl.sql ejecutado.
   El script se puede ejecutar varias veces.
   ===================================================================== */
USE OpinionesClientesDW;
GO

IF SCHEMA_ID(N'etl') IS NULL
    EXEC (N'CREATE SCHEMA etl');
GO

/* ---------------------------------------------------------------------
   1. Bitácora de la carga
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'etl.LogCargaDimensiones', N'U') IS NULL
BEGIN
    CREATE TABLE etl.LogCargaDimensiones
    (
        IdLog            INT               NOT NULL IDENTITY(1,1),
        IdEjecucion      UNIQUEIDENTIFIER  NOT NULL,
        Dimension        VARCHAR(30)       NOT NULL,
        FechaInicio      DATETIME2(3)      NOT NULL,
        FechaFin         DATETIME2(3)      NOT NULL,
        DuracionMs       BIGINT            NOT NULL,
        RegistrosLeidos  INT               NOT NULL,
        Insertados       INT               NOT NULL,
        Actualizados     INT               NOT NULL,
        SinCambios       INT               NOT NULL,
        Rechazados       INT               NOT NULL,
        Estado           VARCHAR(15)       NOT NULL,
        MensajeError     NVARCHAR(1000)    NULL,

        CONSTRAINT PK_LogCargaDimensiones        PRIMARY KEY (IdLog),
        CONSTRAINT CK_LogCargaDimensiones_Estado CHECK (Estado IN ('Completada', 'Fallida'))
    );

    CREATE INDEX IX_LogCargaDimensiones_IdEjecucion ON etl.LogCargaDimensiones (IdEjecucion);
END
GO

/* ---------------------------------------------------------------------
   2. Registros rechazados en la transformación
   Revisado = 0 indica que TI todavía no lo ha revisado.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'etl.ErroresCargaDimensiones', N'U') IS NULL
BEGIN
    CREATE TABLE etl.ErroresCargaDimensiones
    (
        IdError       INT               NOT NULL IDENTITY(1,1),
        IdEjecucion   UNIQUEIDENTIFIER  NOT NULL,
        Dimension     VARCHAR(30)       NOT NULL,
        ClaveNegocio  NVARCHAR(200)     NOT NULL,
        Motivo        NVARCHAR(300)     NOT NULL,
        FechaError    DATETIME2(0)      NOT NULL CONSTRAINT DF_ErroresCargaDimensiones_Fecha    DEFAULT (SYSDATETIME()),
        Revisado      BIT               NOT NULL CONSTRAINT DF_ErroresCargaDimensiones_Revisado DEFAULT (0),

        CONSTRAINT PK_ErroresCargaDimensiones PRIMARY KEY (IdError)
    );
END
GO

/* ---------------------------------------------------------------------
   3. Permisos del login etl_extraccion
   --------------------------------------------------------------------- */
-- Origen: solo lectura
USE OpinionesClientesDB;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'etl_extraccion')
    CREATE USER etl_extraccion FOR LOGIN etl_extraccion;
GO
ALTER ROLE db_datareader ADD MEMBER etl_extraccion;
GO

-- DataWarehouse: leer y escribir filas (INSERT/UPDATE del MERGE y la bitácora)
USE OpinionesClientesDW;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'etl_extraccion')
    CREATE USER etl_extraccion FOR LOGIN etl_extraccion;
GO
ALTER ROLE db_datareader ADD MEMBER etl_extraccion;
ALTER ROLE db_datawriter ADD MEMBER etl_extraccion;
GO

PRINT 'Esquema etl y permisos listos para la carga de dimensiones.';
GO
