using System.Data;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.Staging;

/// <summary>
/// Guarda las encuestas extraídas del CSV en la tabla stg.Encuestas.
/// </summary>
public class EncuestasStagingRepository : StagingRepositoryBase<EncuestaExtraida>
{
    public EncuestasStagingRepository(IOptions<OpcionesStaging> opciones) : base(opciones)
    {
    }

    protected override string NombreTabla => "stg.Encuestas";

    protected override DataTable CrearTablaEnMemoria()
    {
        // Mismos nombres de columna que en la tabla stg.Encuestas
        var tabla = new DataTable();
        tabla.Columns.Add("IdEjecucion", typeof(Guid));
        tabla.Columns.Add("IdOpinion", typeof(string));
        tabla.Columns.Add("IdCliente", typeof(string));
        tabla.Columns.Add("IdProducto", typeof(string));
        tabla.Columns.Add("Fecha", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("Clasificacion", typeof(string));
        tabla.Columns.Add("PuntajeSatisfaccion", typeof(string));
        tabla.Columns.Add("Fuente", typeof(string));
        tabla.Columns.Add("NumeroFila", typeof(int));
        tabla.Columns.Add("Origen", typeof(string));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, EncuestaExtraida registro, Guid idEjecucion, string origen)
    {
        tabla.Rows.Add(
            idEjecucion,
            ValorODbNull(registro.IdOpinion),
            ValorODbNull(registro.IdCliente),
            ValorODbNull(registro.IdProducto),
            ValorODbNull(registro.Fecha),
            ValorODbNull(registro.Comentario),
            ValorODbNull(registro.Clasificacion),
            ValorODbNull(registro.PuntajeSatisfaccion),
            ValorODbNull(registro.Fuente),
            registro.NumeroFila,
            origen);
    }
}
