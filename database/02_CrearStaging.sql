/* =====================================================================
   Proyecto : Sistema de Análisis de Opiniones de Clientes con Proceso ETL
   Materia  : Introducción al Big Data - ITLA
   Autor    : Luis Mata (20175359)
   ===================================================================== */
USE master;
GO

IF DB_ID(N'OpinionesClientesStaging') IS NOT NULL
BEGIN
    ALTER DATABASE OpinionesClientesStaging SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OpinionesClientesStaging;
END
GO

CREATE DATABASE OpinionesClientesStaging;
GO

USE OpinionesClientesStaging;
GO

CREATE SCHEMA stg;
GO

CREATE TABLE stg.Encuestas
(
    IdStaging             BIGINT            NOT NULL IDENTITY(1,1),
    IdEjecucion           UNIQUEIDENTIFIER  NOT NULL,
    IdOpinion             NVARCHAR(50)      NULL,
    IdCliente             NVARCHAR(50)      NULL,
    IdProducto            NVARCHAR(50)      NULL,
    Fecha                 NVARCHAR(50)      NULL,
    Comentario            NVARCHAR(MAX)     NULL,
    Clasificacion         NVARCHAR(50)      NULL,
    PuntajeSatisfaccion   NVARCHAR(20)      NULL,
    Fuente                NVARCHAR(100)     NULL,
    NumeroFila            INT               NULL,      -- fila dentro del archivo
    Origen                NVARCHAR(300)     NOT NULL,  -- nombre del archivo
    FechaExtraccion       DATETIME2(0)      NOT NULL CONSTRAINT DF_stgEncuestas_FechaExtraccion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_stgEncuestas PRIMARY KEY (IdStaging)
);
GO


CREATE TABLE stg.ResenasWeb
(
    IdStaging             BIGINT            NOT NULL IDENTITY(1,1),
    IdEjecucion           UNIQUEIDENTIFIER  NOT NULL,
    IdReview              NVARCHAR(50)      NULL,
    IdCliente             NVARCHAR(50)      NULL,
    IdProducto            NVARCHAR(50)      NULL,
    Fecha                 NVARCHAR(50)      NULL,
    Comentario            NVARCHAR(MAX)     NULL,
    Rating                NVARCHAR(20)      NULL,
    Origen                NVARCHAR(300)     NOT NULL,  -- base.esquema.tabla
    FechaExtraccion       DATETIME2(0)      NOT NULL CONSTRAINT DF_stgResenasWeb_FechaExtraccion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_stgResenasWeb PRIMARY KEY (IdStaging)
);
GO

CREATE TABLE stg.ComentariosSociales
(
    IdStaging             BIGINT            NOT NULL IDENTITY(1,1),
    IdEjecucion           UNIQUEIDENTIFIER  NOT NULL,
    IdComment             NVARCHAR(50)      NULL,
    IdCliente             NVARCHAR(50)      NULL,      -- vacío = comentario anónimo
    IdProducto            NVARCHAR(50)      NULL,
    Fuente                NVARCHAR(50)      NULL,      -- red social
    Fecha                 NVARCHAR(50)      NULL,
    Comentario            NVARCHAR(MAX)     NULL,
    Origen                NVARCHAR(300)     NOT NULL,  -- URL del endpoint
    FechaExtraccion       DATETIME2(0)      NOT NULL CONSTRAINT DF_stgComentariosSociales_FechaExtraccion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_stgComentariosSociales PRIMARY KEY (IdStaging)
);
GO


CREATE TABLE stg.LogExtraccion
(
    IdLog                 INT               NOT NULL IDENTITY(1,1),
    IdEjecucion           UNIQUEIDENTIFIER  NOT NULL,
    Fuente                NVARCHAR(50)      NOT NULL,
    TipoFuente            NVARCHAR(20)      NOT NULL,
    Origen                NVARCHAR(300)     NOT NULL,
    FechaInicio           DATETIME2(3)      NOT NULL,
    FechaFin              DATETIME2(3)      NOT NULL,
    DuracionMs            BIGINT            NOT NULL,
    RegistrosExtraidos    INT               NOT NULL,
    Estado                VARCHAR(15)       NOT NULL,
    MensajeError          NVARCHAR(1000)    NULL,

    CONSTRAINT PK_stgLogExtraccion        PRIMARY KEY (IdLog),
    CONSTRAINT CK_stgLogExtraccion_Estado CHECK (Estado IN ('Completada', 'Fallida')),
    CONSTRAINT CK_stgLogExtraccion_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CK_stgLogExtraccion_Conteo CHECK (RegistrosExtraidos >= 0 AND DuracionMs >= 0)
);
GO

CREATE INDEX IX_stgLogExtraccion_IdEjecucion ON stg.LogExtraccion (IdEjecucion);
GO
