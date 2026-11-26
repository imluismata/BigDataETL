using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimPalabraClave desde el diccionario PalabrasClave (unido con
/// Clasificaciones para obtener el tipo de la palabra).
/// Clave de negocio: Palabra. SCD tipo 1.
/// </summary>
public class DimPalabraClaveCarga : CargaDimensionBase<DimPalabraClave>
{
    public DimPalabraClaveCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimPalabraClaveCarga> logger)
        : base(conexiones, logger)
    {
    }

    public override string NombreDimension => "DimPalabraClave";

    protected override string ConsultaOrigen => @"
        SELECT p.Palabra, c.Nombre AS TipoPalabra
        FROM dbo.PalabrasClave AS p
        INNER JOIN dbo.Clasificaciones AS c ON c.IdClasificacion = p.IdClasificacion
        ORDER BY p.Palabra;";

    protected override DimPalabraClave LeerFila(SqlDataReader lector)
    {
        return new DimPalabraClave
        {
            Palabra = LeerTexto(lector, 0) ?? string.Empty,
            TipoPalabra = LeerTexto(lector, 1) ?? string.Empty
        };
    }

    protected override ResultadoTransformacion<DimPalabraClave> Transformar(DimPalabraClave fila)
    {
        return TransformadorDimensiones.TransformarPalabraClave(fila);
    }

    protected override string ObtenerClaveNegocio(DimPalabraClave fila) => $"Palabra={fila.Palabra}";

    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            Palabra      NVARCHAR(50)  COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
            TipoPalabra  VARCHAR(10)   COLLATE DATABASE_DEFAULT NOT NULL
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("Palabra", typeof(string));
        tabla.Columns.Add("TipoPalabra", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimPalabraClave fila)
    {
        tabla.Rows.Add(fila.Palabra, fila.TipoPalabra);
    }

    protected override string SentenciaMerge => @"
        MERGE dbo.DimPalabraClave AS destino
        USING #Origen AS origen
            ON destino.Palabra = origen.Palabra
        WHEN MATCHED AND destino.TipoPalabra <> origen.TipoPalabra
            THEN UPDATE SET destino.TipoPalabra = origen.TipoPalabra
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (Palabra, TipoPalabra)
                 VALUES (origen.Palabra, origen.TipoPalabra)
        OUTPUT $action INTO @Acciones;";
}
