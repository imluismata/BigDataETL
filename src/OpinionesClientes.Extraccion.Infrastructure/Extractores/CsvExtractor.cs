using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.Interfaces;
using OpinionesClientes.Extraccion.Domain.Entidades;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;
using OpinionesClientes.Extraccion.Infrastructure.Extractores.Mapas;

namespace OpinionesClientes.Extraccion.Infrastructure.Extractores;

/// <summary>
/// Extrae las encuestas internas desde un archivo CSV usando CsvHelper.
///
/// Validaciones que hace (solo de estructura, no de contenido):
///   - El archivo existe.
///   - El encabezado tiene todas las columnas esperadas.
///   - Cada fila tiene la misma cantidad de columnas que el encabezado.
///     Las filas mal formadas se registran en el log y se saltan.
///
/// La validación del CONTENIDO (fechas, números, duplicados...) es parte
/// de la transformación, no de la extracción.
/// </summary>
public class CsvExtractor : IExtractor<EncuestaExtraida>
{
    private readonly OpcionesFuenteCsv _opciones;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<OpcionesFuenteCsv> opciones, ILogger<CsvExtractor> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public string NombreFuente => "Encuestas";

    public string TipoFuente => "CSV";

    public string Origen => Path.GetFileName(_opciones.RutaArchivo);

    public async IAsyncEnumerable<EncuestaExtraida> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string rutaCompleta = ObtenerRutaCompleta();

        // Validación 1: el archivo debe existir
        if (!File.Exists(rutaCompleta))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo CSV de encuestas en: {rutaCompleta}", rutaCompleta);
        }

        CsvConfiguration configuracion = CrearConfiguracion();

        // Se abre el archivo en modo lectura. StreamReader lee por partes,
        // no carga el archivo completo en memoria.
        using var lectorArchivo = new StreamReader(rutaCompleta, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(lectorArchivo, configuracion);
        csv.Context.RegisterClassMap<EncuestaCsvMap>();

        // Validación 2: leer el encabezado y comprobar que estén todas las columnas.
        // Si falta alguna, CsvHelper lanza HeaderValidationException y la fuente
        // se marca como fallida (el archivo no tiene la estructura esperada).
        if (!await csv.ReadAsync())
        {
            _logger.LogWarning("[{Fuente}] El archivo {Archivo} está vacío", NombreFuente, Origen);
            yield break;
        }

        csv.ReadHeader();
        csv.ValidateHeader<EncuestaExtraida>();
        int columnasEsperadas = csv.HeaderRecord!.Length;

        // Recorrer el archivo fila por fila
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            EncuestaExtraida? encuesta = LeerFila(csv, columnasEsperadas);

            // null = fila con problemas: ya se registró en el log, se salta
            if (encuesta is null)
            {
                continue;
            }

            // yield return entrega el registro al proceso y sigue leyendo
            yield return encuesta;
        }
    }

    /// <summary>
    /// Convierte la fila actual en un objeto EncuestaExtraida.
    /// Devuelve null si la fila está mal formada.
    /// </summary>
    private EncuestaExtraida? LeerFila(CsvReader csv, int columnasEsperadas)
    {
        int numeroFila = csv.Parser.Row;

        // Validación 3: cantidad de columnas de la fila
        if (csv.Parser.Count != columnasEsperadas)
        {
            _logger.LogWarning(
                "[{Fuente}] Fila {Fila} omitida: tiene {Columnas} columnas y se esperaban {Esperadas}. Contenido: {Contenido}",
                NombreFuente, numeroFila, csv.Parser.Count, columnasEsperadas, csv.Parser.RawRecord.Trim());
            return null;
        }

        try
        {
            EncuestaExtraida encuesta = csv.GetRecord<EncuestaExtraida>();
            encuesta.NumeroFila = numeroFila;
            return encuesta;
        }
        catch (CsvHelperException ex)
        {
            _logger.LogWarning(
                "[{Fuente}] Fila {Fila} omitida: no se pudo leer ({Error})",
                NombreFuente, numeroFila, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Opciones de lectura del CSV.
    /// </summary>
    private CsvConfiguration CrearConfiguracion()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = _opciones.Delimitador,

            // Comillas mal cerradas, etc.: se avisa en el log pero la fila se
            // guarda igual (staging recibe el dato tal como llega).
            BadDataFound = argumentos =>
                _logger.LogWarning(
                    "[{Fuente}] Dato con formato irregular en la fila {Fila}: {Campo}",
                    NombreFuente, argumentos.Context.Parser?.Row, argumentos.Field)
        };
    }

    /// <summary>
    /// Si la ruta configurada es relativa, se toma desde la carpeta del ejecutable.
    /// </summary>
    private string ObtenerRutaCompleta()
    {
        if (Path.IsPathRooted(_opciones.RutaArchivo))
        {
            return _opciones.RutaArchivo;
        }

        return Path.Combine(AppContext.BaseDirectory, _opciones.RutaArchivo);
    }
}
