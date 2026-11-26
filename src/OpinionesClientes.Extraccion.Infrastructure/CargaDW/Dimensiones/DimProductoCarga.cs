using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimProducto desde Productos + Categorias (la categoría se desnormaliza:
/// en el modelo estrella va como texto dentro de la dimensión).
/// Clave de negocio: ProductoId. SCD tipo 1.
/// </summary>
public class DimProductoCarga : CargaDimensionBase<DimProducto>
{
    public DimProductoCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimProductoCarga> logger)
        : base(conexiones, logger)
    {
    }

    public override string NombreDimension => "DimProducto";

    protected override string ConsultaOrigen => @"
        SELECT p.IdProducto, p.Nombre, c.Nombre AS Categoria
        FROM dbo.Productos AS p
        LEFT JOIN dbo.Categorias AS c ON c.IdCategoria = p.IdCategoria
        ORDER BY p.IdProducto;";

    protected override DimProducto LeerFila(SqlDataReader lector)
    {
        return new DimProducto
        {
            ProductoId = lector.GetInt32(0),
            NombreProducto = LeerTexto(lector, 1) ?? string.Empty,
            Categoria = LeerTexto(lector, 2) ?? string.Empty
        };
    }

    protected override ResultadoTransformacion<DimProducto> Transformar(DimProducto fila)
    {
        return TransformadorDimensiones.TransformarProducto(fila);
    }

    protected override string ObtenerClaveNegocio(DimProducto fila) => $"IdProducto={fila.ProductoId}";

    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            ProductoId      INT            NOT NULL PRIMARY KEY,
            NombreProducto  NVARCHAR(150)  COLLATE DATABASE_DEFAULT NOT NULL,
            Categoria       NVARCHAR(100)  COLLATE DATABASE_DEFAULT NOT NULL
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("ProductoId", typeof(int));
        tabla.Columns.Add("NombreProducto", typeof(string));
        tabla.Columns.Add("Categoria", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimProducto fila)
    {
        tabla.Rows.Add(fila.ProductoId, fila.NombreProducto, fila.Categoria);
    }

    protected override string SentenciaMerge => @"
        MERGE dbo.DimProducto AS destino
        USING #Origen AS origen
            ON destino.ProductoId = origen.ProductoId
        WHEN MATCHED AND (   destino.NombreProducto <> origen.NombreProducto
                          OR destino.Categoria      <> origen.Categoria)
            THEN UPDATE SET destino.NombreProducto     = origen.NombreProducto,
                            destino.Categoria          = origen.Categoria,
                            destino.FechaActualizacion = SYSDATETIME()
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (ProductoId, NombreProducto, Categoria)
                 VALUES (origen.ProductoId, origen.NombreProducto, origen.Categoria)
        OUTPUT $action INTO @Acciones;";
}
