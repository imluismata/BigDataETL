using CsvHelper.Configuration;
using OpinionesClientes.Extraccion.Domain.Entidades;

namespace OpinionesClientes.Extraccion.Infrastructure.Extractores.Mapas;

/// <summary>
/// Le dice a CsvHelper qué columna del archivo va en cada propiedad.
///
/// Se usa un ClassMap (en vez de atributos en la entidad) para que la capa
/// de dominio no dependa de CsvHelper: el dominio queda limpio.
///
/// Algunas columnas del archivo tienen tilde ("Clasificación",
/// "PuntajeSatisfacción"); se aceptan también sin tilde por si otro
/// archivo viene sin ellas.
/// </summary>
public sealed class EncuestaCsvMap : ClassMap<EncuestaExtraida>
{
    public EncuestaCsvMap()
    {
        Map(e => e.IdOpinion).Name("IdOpinion");
        Map(e => e.IdCliente).Name("IdCliente");
        Map(e => e.IdProducto).Name("IdProducto");
        Map(e => e.Fecha).Name("Fecha");
        Map(e => e.Comentario).Name("Comentario");
        Map(e => e.Clasificacion).Name("Clasificación", "Clasificacion");
        Map(e => e.PuntajeSatisfaccion).Name("PuntajeSatisfacción", "PuntajeSatisfaccion");
        Map(e => e.Fuente).Name("Fuente");

        // NumeroFila no es una columna del archivo: la llena el extractor
        Map(e => e.NumeroFila).Ignore();
    }
}
