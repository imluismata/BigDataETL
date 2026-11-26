using System.Data;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Staging;

/// <summary>
/// Guarda los comentarios recibidos de la API en la tabla stg.ComentariosSociales.
/// </summary>
public class ComentariosSocialesStagingRepository : StagingRepositoryBase<ComentarioSocialExtraido>
{
    public ComentariosSocialesStagingRepository(IOptions<OpcionesStaging> opciones) : base(opciones)
    {
    }

    protected override string NombreTabla => "stg.ComentariosSociales";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("IdEjecucion", typeof(Guid));
        tabla.Columns.Add("IdComment", typeof(string));
        tabla.Columns.Add("IdCliente", typeof(string));
        tabla.Columns.Add("IdProducto", typeof(string));
        tabla.Columns.Add("Fuente", typeof(string));
        tabla.Columns.Add("Fecha", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("Origen", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, ComentarioSocialExtraido registro, Guid idEjecucion, string origen)
    {
        tabla.Rows.Add(
            idEjecucion,
            ValorODbNull(registro.IdComment),
            ValorODbNull(registro.IdCliente),
            ValorODbNull(registro.IdProducto),
            ValorODbNull(registro.Fuente),
            ValorODbNull(registro.Fecha),
            ValorODbNull(registro.Comentario),
            origen);
    }
}
