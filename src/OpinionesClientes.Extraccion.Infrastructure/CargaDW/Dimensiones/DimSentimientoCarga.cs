using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimSentimiento desde Clasificaciones. Usa los mismos Id del origen
/// (1 Positiva, 2 Negativa, 3 Neutra), por eso la clave no es IDENTITY.
/// </summary>
public class DimSentimientoCarga : CargaDimensionBase<DimSentimiento>
{
    public DimSentimientoCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimSentimientoCarga> logger)
        : base(conexiones, logger)
    {
    }

    public override string NombreDimension => "DimSentimiento";

    protected override string ConsultaOrigen => @"
        SELECT IdClasificacion, Nombre
        FROM dbo.Clasificaciones
        ORDER BY IdClasificacion;";

    protected override DimSentimiento LeerFila(SqlDataReader lector)
    {
        return new DimSentimiento
        {
            SentimientoKey = lector.GetInt32(0),
            Clasificacion = LeerTexto(lector, 1) ?? string.Empty
        };
    }

    protected override ResultadoTransformacion<DimSentimiento> Transformar(DimSentimiento fila)
    {
        return TransformadorDimensiones.TransformarSentimiento(fila);
    }

    protected override string ObtenerClaveNegocio(DimSentimiento fila) => $"IdClasificacion={fila.SentimientoKey}";

    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            SentimientoKey    INT          NOT NULL PRIMARY KEY,
            Clasificacion     VARCHAR(10)  COLLATE DATABASE_DEFAULT NOT NULL,
            ValorSentimiento  SMALLINT     NOT NULL
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("SentimientoKey", typeof(int));
        tabla.Columns.Add("Clasificacion", typeof(string));
        tabla.Columns.Add("ValorSentimiento", typeof(short));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimSentimiento fila)
    {
        tabla.Rows.Add(fila.SentimientoKey, fila.Clasificacion, fila.ValorSentimiento);
    }

    protected override string SentenciaMerge => @"
        MERGE dbo.DimSentimiento AS destino
        USING #Origen AS origen
            ON destino.SentimientoKey = origen.SentimientoKey
        WHEN MATCHED AND (   destino.Clasificacion    <> origen.Clasificacion
                          OR destino.ValorSentimiento <> origen.ValorSentimiento)
            THEN UPDATE SET destino.Clasificacion    = origen.Clasificacion,
                            destino.ValorSentimiento = origen.ValorSentimiento
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (SentimientoKey, Clasificacion, ValorSentimiento)
                 VALUES (origen.SentimientoKey, origen.Clasificacion, origen.ValorSentimiento)
        OUTPUT $action INTO @Acciones;";
}
