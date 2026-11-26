using System.Data;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Staging;

/// <summary>
/// Guarda las reseñas extraídas de TiendaOnlineDB en la tabla stg.ResenasWeb.
/// </summary>
public class ResenasWebStagingRepository : StagingRepositoryBase<ResenaWebExtraida>
{
    public ResenasWebStagingRepository(IOptions<OpcionesStaging> opciones) : base(opciones)
    {
    }

    protected override string NombreTabla => "stg.ResenasWeb";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("IdEjecucion", typeof(Guid));
        tabla.Columns.Add("IdReview", typeof(string));
        tabla.Columns.Add("IdCliente", typeof(string));
        tabla.Columns.Add("IdProducto", typeof(string));
        tabla.Columns.Add("Fecha", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("Rating", typeof(string));
        tabla.Columns.Add("Origen", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, ResenaWebExtraida registro, Guid idEjecucion, string origen)
    {
        tabla.Rows.Add(
            idEjecucion,
            ValorODbNull(registro.IdReview),
            ValorODbNull(registro.IdCliente),
            ValorODbNull(registro.IdProducto),
            ValorODbNull(registro.Fecha),
            ValorODbNull(registro.Comentario),
            ValorODbNull(registro.Rating),
            origen);
    }
}
