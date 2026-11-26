/* =====================================================================
   Proyecto : Sistema de Análisis de Opiniones de Clientes con Proceso ETL
   Autor    : Luis Mata (20175359)
   Archivo  : 03_CrearUsuarioEtl.sql
   ===================================================================== */
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'etl_extraccion')
BEGIN
    CREATE LOGIN etl_extraccion
        WITH PASSWORD = N'ITLA1010',
             CHECK_POLICY = ON;
END
GO

/* ---------------------------------------------------------------------
   Fuente: solo lectura
   --------------------------------------------------------------------- */
USE TiendaOnlineDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'etl_extraccion')
    CREATE USER etl_extraccion FOR LOGIN etl_extraccion;
GO

ALTER ROLE db_datareader ADD MEMBER etl_extraccion;
GO

/* ---------------------------------------------------------------------
   Staging: permisos solo sobre el esquema stg
   --------------------------------------------------------------------- */
USE OpinionesClientesStaging;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'etl_extraccion')
    CREATE USER etl_extraccion FOR LOGIN etl_extraccion;
GO

-- SELECT e INSERT para leer y cargar (SqlBulkCopy)
GRANT SELECT, INSERT ON SCHEMA::stg TO etl_extraccion;
-- ALTER es necesario para TRUNCATE TABLE (vaciar staging antes de extraer)
GRANT ALTER ON SCHEMA::stg TO etl_extraccion;
GO
