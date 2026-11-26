/* =====================================================================
   Archivo  : 06_ConsultasDimensiones.sql
   Consultas para revisar la carga de dimensiones del DataWarehouse.
   ===================================================================== */
USE OpinionesClientesDW;
GO

-- 1. Resultado de la última carga, por dimensión
SELECT  Dimension, RegistrosLeidos, Insertados, Actualizados, SinCambios,
        Rechazados, DuracionMs, Estado, MensajeError, FechaInicio
FROM etl.LogCargaDimensiones
WHERE IdEjecucion = (SELECT TOP (1) IdEjecucion FROM etl.LogCargaDimensiones ORDER BY IdLog DESC)
ORDER BY IdLog;
GO

-- 2. Filas de cada dimensión (sin contar los miembros especiales -1)
SELECT 'DimFecha'        AS Dimension, COUNT(*) AS Filas FROM dbo.DimFecha
UNION ALL SELECT 'DimSentimiento',  COUNT(*) FROM dbo.DimSentimiento
UNION ALL SELECT 'DimFuente',       COUNT(*) FROM dbo.DimFuente
UNION ALL SELECT 'DimPalabraClave', COUNT(*) FROM dbo.DimPalabraClave
UNION ALL SELECT 'DimProducto',     COUNT(*) FROM dbo.DimProducto WHERE ProductoKey > 0
UNION ALL SELECT 'DimCliente',      COUNT(*) FROM dbo.DimCliente  WHERE ClienteKey  > 0
UNION ALL SELECT 'DimEvento',       COUNT(*) FROM dbo.DimEvento   WHERE EventoKey   > 0;
GO

-- 3. Cuadre origen vs. DataWarehouse
SELECT 'Clientes'  AS Entidad,
       (SELECT COUNT(*) FROM OpinionesClientesDB.dbo.Clientes)           AS EnOrigen,
       (SELECT COUNT(*) FROM dbo.DimCliente WHERE ClienteKey > 0)        AS EnDW
UNION ALL
SELECT 'Productos',
       (SELECT COUNT(*) FROM OpinionesClientesDB.dbo.Productos),
       (SELECT COUNT(*) FROM dbo.DimProducto WHERE ProductoKey > 0)
UNION ALL
SELECT 'Palabras clave',
       (SELECT COUNT(*) FROM OpinionesClientesDB.dbo.PalabrasClave),
       (SELECT COUNT(*) FROM dbo.DimPalabraClave);
GO

-- 4. Muestra de las dimensiones cargadas
SELECT TOP (10) * FROM dbo.DimCliente      ORDER BY ClienteKey;
SELECT TOP (10) * FROM dbo.DimProducto     ORDER BY ProductoKey;
SELECT * FROM dbo.DimFuente                ORDER BY FuenteKey;
SELECT * FROM dbo.DimSentimiento           ORDER BY SentimientoKey;
SELECT TOP (10) * FROM dbo.DimPalabraClave ORDER BY PalabraKey;
SELECT TOP (5)  * FROM dbo.DimFecha        WHERE Fecha >= '2025-09-01' ORDER BY FechaKey;
GO

-- 5. Productos por categoría (comprueba la desnormalización)
SELECT Categoria, COUNT(*) AS Productos
FROM dbo.DimProducto
WHERE ProductoKey > 0
GROUP BY Categoria
ORDER BY Productos DESC;
GO

-- 6. Registros rechazados (si hubo)
SELECT TOP (50) * FROM etl.ErroresCargaDimensiones ORDER BY IdError DESC;
GO
