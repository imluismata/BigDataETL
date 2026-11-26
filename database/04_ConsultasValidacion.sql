/* =====================================================================
   Archivo  : 04_ConsultasValidacion.sql
   Consultas para comprobar la extracción después de ejecutar el Worker.
   ===================================================================== */
USE OpinionesClientesStaging;
GO

-- 1. Resultado de la última ejecución, por fuente (rendimiento y estado)
SELECT  l.Fuente,
        l.TipoFuente,
        l.Origen,
        l.RegistrosExtraidos,
        l.DuracionMs,
        CAST(l.RegistrosExtraidos / NULLIF(l.DuracionMs / 1000.0, 0) AS DECIMAL(12,1)) AS RegistrosPorSegundo,
        l.Estado,
        l.MensajeError,
        l.FechaInicio
FROM stg.LogExtraccion AS l
WHERE l.IdEjecucion = (SELECT TOP (1) IdEjecucion FROM stg.LogExtraccion ORDER BY IdLog DESC)
ORDER BY l.Fuente;
GO

-- 2. Conteo de filas en cada tabla staging (esperado con los datos de la
--    plataforma: Encuestas 500, ResenasWeb 200, ComentariosSociales 200)
SELECT 'stg.Encuestas'           AS Tabla, COUNT(*) AS Filas FROM stg.Encuestas
UNION ALL
SELECT 'stg.ResenasWeb',                   COUNT(*)          FROM stg.ResenasWeb
UNION ALL
SELECT 'stg.ComentariosSociales',          COUNT(*)          FROM stg.ComentariosSociales;
GO

-- 3. Muestra de cada tabla
SELECT TOP (5) * FROM stg.Encuestas           ORDER BY IdStaging;
SELECT TOP (5) * FROM stg.ResenasWeb          ORDER BY IdStaging;
SELECT TOP (5) * FROM stg.ComentariosSociales ORDER BY IdStaging;
GO

-- 4. Comprobar que la fuente BD llegó completa: comparar con el origen
SELECT
    (SELECT COUNT(*) FROM TiendaOnlineDB.dbo.Resenas)          AS FilasEnOrigen,
    (SELECT COUNT(*) FROM OpinionesClientesStaging.stg.ResenasWeb) AS FilasEnStaging;
GO

-- 5. Historial de ejecuciones (todas)
SELECT  IdEjecucion,
        MIN(FechaInicio)          AS Inicio,
        SUM(RegistrosExtraidos)   AS TotalRegistros,
        MAX(DuracionMs)           AS DuracionMsFuenteMasLenta,
        SUM(CASE WHEN Estado = 'Fallida' THEN 1 ELSE 0 END) AS FuentesFallidas
FROM stg.LogExtraccion
GROUP BY IdEjecucion
ORDER BY Inicio DESC;
GO
