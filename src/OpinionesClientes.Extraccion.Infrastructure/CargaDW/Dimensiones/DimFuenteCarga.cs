using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimFuente con los canales que aparecen en las opiniones.
/// Si llega un canal nuevo (por ejemplo, otra red social), se agrega una fila.
/// Clave de negocio: (CodigoTipoFuente, Canal).
/// </summary>
public class DimFuenteCarga : CargaDimensionBase<DimFuente>
{
    public DimFuenteCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimFuenteCarga> logger)
        : base(conexiones, logger)
    {
    }

    public override string NombreDimension => "DimFuente";

    protected override string ConsultaOrigen => @"
        SELECT DISTINCT TipoFuente, Canal
        FROM dbo.Opiniones;";

    protected override DimFuente LeerFila(SqlDataReader lector)
    {
        return new DimFuente
        {
            CodigoTipoFuente = LeerTexto(lector, 0) ?? string.Empty,
            Canal = LeerTexto(lector, 1) ?? string.Empty
        };
    }

    protected override ResultadoTransformacion<DimFuente> Transformar(DimFuente fila)
    {
        return TransformadorDimensiones.TransformarFuente(fila);
    }

    protected override string ObtenerClaveNegocio(DimFuente fila) => $"TipoFuente={fila.CodigoTipoFuente}, Canal={fila.Canal}";

    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            CodigoTipoFuente  VARCHAR(20)   COLLATE DATABASE_DEFAULT NOT NULL,
            Canal             NVARCHAR(30)  COLLATE DATABASE_DEFAULT NOT NULL,
            TipoFuente        NVARCHAR(30)  COLLATE DATABASE_DEFAULT NOT NULL,
            MedioIngreso      NVARCHAR(30)  COLLATE DATABASE_DEFAULT NOT NULL,
            PRIMARY KEY (CodigoTipoFuente, Canal)
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("CodigoTipoFuente", typeof(string));
        tabla.Columns.Add("Canal", typeof(string));
        tabla.Columns.Add("TipoFuente", typeof(string));
        tabla.Columns.Add("MedioIngreso", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimFuente fila)
    {
        tabla.Rows.Add(fila.CodigoTipoFuente, fila.Canal, fila.TipoFuente, fila.MedioIngreso);
    }

    protected override string SentenciaMerge => @"
        MERGE dbo.DimFuente AS destino
        USING #Origen AS origen
            ON  destino.CodigoTipoFuente = origen.CodigoTipoFuente
            AND destino.Canal            = origen.Canal
        WHEN MATCHED AND (   destino.TipoFuente   <> origen.TipoFuente
                          OR destino.MedioIngreso <> origen.MedioIngreso)
            THEN UPDATE SET destino.TipoFuente   = origen.TipoFuente,
                            destino.MedioIngreso = origen.MedioIngreso
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (CodigoTipoFuente, TipoFuente, Canal, MedioIngreso)
                 VALUES (origen.CodigoTipoFuente, origen.TipoFuente, origen.Canal, origen.MedioIngreso)
        OUTPUT $action INTO @Acciones;";
}
