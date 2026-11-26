using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimCliente desde OpinionesClientesDB.dbo.Clientes.
/// Clave de negocio: ClienteId. SCD tipo 1 (si el nombre o el correo cambian, se sobrescriben).
/// </summary>
public class DimClienteCarga : CargaDimensionBase<DimCliente>
{
    public DimClienteCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimClienteCarga> logger)
        : base(conexiones, logger)
    {
    }

    public override string NombreDimension => "DimCliente";

    protected override string ConsultaOrigen => @"
        SELECT IdCliente, Nombre, Email
        FROM dbo.Clientes
        ORDER BY IdCliente;";

    protected override DimCliente LeerFila(SqlDataReader lector)
    {
        return new DimCliente
        {
            ClienteId = lector.GetInt32(0),
            NombreCliente = LeerTexto(lector, 1) ?? string.Empty,
            Email = LeerTexto(lector, 2)
        };
    }

    protected override ResultadoTransformacion<DimCliente> Transformar(DimCliente fila)
    {
        return TransformadorDimensiones.TransformarCliente(fila);
    }

    protected override string ObtenerClaveNegocio(DimCliente fila) => $"IdCliente={fila.ClienteId}";

    // COLLATE DATABASE_DEFAULT: la tabla temporal vive en tempdb; así compara
    // textos con la misma intercalación que el DataWarehouse.
    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            ClienteId      INT            NOT NULL PRIMARY KEY,
            NombreCliente  NVARCHAR(150)  COLLATE DATABASE_DEFAULT NOT NULL,
            Email          NVARCHAR(150)  COLLATE DATABASE_DEFAULT NULL,
            TipoCliente    NVARCHAR(30)   COLLATE DATABASE_DEFAULT NOT NULL
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("ClienteId", typeof(int));
        tabla.Columns.Add("NombreCliente", typeof(string));
        tabla.Columns.Add("Email", typeof(string));
        tabla.Columns.Add("TipoCliente", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimCliente fila)
    {
        tabla.Rows.Add(fila.ClienteId, fila.NombreCliente, ValorODbNull(fila.Email), fila.TipoCliente);
    }

    // Pais, Ciudad y RangoEdad no vienen en el origen: toman su valor por defecto
    // ("No disponible") al insertar y el MERGE no los toca al actualizar.
    protected override string SentenciaMerge => @"
        MERGE dbo.DimCliente AS destino
        USING #Origen AS origen
            ON destino.ClienteId = origen.ClienteId
        WHEN MATCHED AND (   destino.NombreCliente <> origen.NombreCliente
                          OR ISNULL(destino.Email, N'') <> ISNULL(origen.Email, N'')
                          OR destino.TipoCliente <> origen.TipoCliente)
            THEN UPDATE SET destino.NombreCliente      = origen.NombreCliente,
                            destino.Email              = origen.Email,
                            destino.TipoCliente        = origen.TipoCliente,
                            destino.FechaActualizacion = SYSDATETIME()
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (ClienteId, NombreCliente, Email, TipoCliente)
                 VALUES (origen.ClienteId, origen.NombreCliente, origen.Email, origen.TipoCliente)
        OUTPUT $action INTO @Acciones;";
}
