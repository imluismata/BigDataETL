using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesClientes.Extraccion.Application.CargaDW;
using OpinionesClientes.Extraccion.Domain.Dimensiones;
using OpinionesClientes.Extraccion.Infrastructure.Configuracion;

namespace OpinionesClientes.Extraccion.Infrastructure.CargaDW.Dimensiones;

/// <summary>
/// Carga DimFecha. Es distinta a las demás: no copia filas del origen,
/// sino que lee la fecha mínima y máxima de las opiniones y GENERA un día
/// por fila en ese rango. Solo inserta los días que falten
/// (los días nunca cambian, por eso no hay UPDATE).
/// </summary>
public class DimFechaCarga : CargaDimensionBase<DimFecha>
{
    private readonly ILogger<DimFechaCarga> _logger;

    public DimFechaCarga(IOptions<OpcionesConexionesDW> conexiones, ILogger<DimFechaCarga> logger)
        : base(conexiones, logger)
    {
        _logger = logger;
    }

    public override string NombreDimension => "DimFecha";

    protected override string ConsultaOrigen => @"
        SELECT MIN(Fecha) AS Desde, MAX(Fecha) AS Hasta
        FROM dbo.Opiniones;";

    /// <summary>
    /// EXTRAER: rango de fechas de las opiniones -> calendario de ese rango.
    /// </summary>
    protected override async Task<List<DimFecha>> ExtraerAsync(CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(Conexiones.CadenaOrigen);
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(ConsultaOrigen, conexion);
        comando.CommandTimeout = Conexiones.TimeoutSegundos;

        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);

        // Si no hay opiniones, MIN y MAX vienen en NULL: no hay días que cargar
        if (!await lector.ReadAsync(cancellationToken) || lector.IsDBNull(0) || lector.IsDBNull(1))
        {
            return new List<DimFecha>();
        }

        DateTime desde = lector.GetDateTime(0);
        DateTime hasta = lector.GetDateTime(1);

        _logger.LogInformation("[DimFecha] Rango de fechas de las opiniones: {Desde:yyyy-MM-dd} a {Hasta:yyyy-MM-dd}", desde, hasta);

        return TransformadorDimensiones.GenerarCalendario(desde, hasta);
    }

    // No se usa: la extracción de DimFecha está redefinida arriba
    protected override DimFecha LeerFila(SqlDataReader lector)
    {
        throw new NotSupportedException("DimFecha genera sus filas en ExtraerAsync.");
    }

    // El calendario ya sale limpio del generador
    protected override ResultadoTransformacion<DimFecha> Transformar(DimFecha fila)
    {
        return ResultadoTransformacion<DimFecha>.Valida(fila);
    }

    protected override string ObtenerClaveNegocio(DimFecha fila) => $"FechaKey={fila.FechaKey}";

    protected override string SentenciaTablaTemporal => @"
        CREATE TABLE #Origen
        (
            FechaKey         INT           NOT NULL PRIMARY KEY,
            Fecha            DATE          NOT NULL,
            Anio             SMALLINT      NOT NULL,
            Trimestre        TINYINT       NOT NULL,
            NombreTrimestre  VARCHAR(2)    COLLATE DATABASE_DEFAULT NOT NULL,
            AnioTrimestre    CHAR(7)       COLLATE DATABASE_DEFAULT NOT NULL,
            Mes              TINYINT       NOT NULL,
            NombreMes        NVARCHAR(12)  COLLATE DATABASE_DEFAULT NOT NULL,
            AnioMes          CHAR(7)       COLLATE DATABASE_DEFAULT NOT NULL,
            InicioSemana     DATE          NOT NULL,
            DiaMes           TINYINT       NOT NULL,
            DiaSemana        TINYINT       NOT NULL,
            NombreDia        NVARCHAR(12)  COLLATE DATABASE_DEFAULT NOT NULL,
            EsFinDeSemana    BIT           NOT NULL
        );";

    protected override DataTable CrearTablaEnMemoria()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("FechaKey", typeof(int));
        tabla.Columns.Add("Fecha", typeof(DateTime));
        tabla.Columns.Add("Anio", typeof(short));
        tabla.Columns.Add("Trimestre", typeof(byte));
        tabla.Columns.Add("NombreTrimestre", typeof(string));
        tabla.Columns.Add("AnioTrimestre", typeof(string));
        tabla.Columns.Add("Mes", typeof(byte));
        tabla.Columns.Add("NombreMes", typeof(string));
        tabla.Columns.Add("AnioMes", typeof(string));
        tabla.Columns.Add("InicioSemana", typeof(DateTime));
        tabla.Columns.Add("DiaMes", typeof(byte));
        tabla.Columns.Add("DiaSemana", typeof(byte));
        tabla.Columns.Add("NombreDia", typeof(string));
        tabla.Columns.Add("EsFinDeSemana", typeof(bool));
        return tabla;
    }

    protected override void AgregarFila(DataTable tabla, DimFecha fila)
    {
        tabla.Rows.Add(
            fila.FechaKey, fila.Fecha, fila.Anio, fila.Trimestre, fila.NombreTrimestre,
            fila.AnioTrimestre, fila.Mes, fila.NombreMes, fila.AnioMes, fila.InicioSemana,
            fila.DiaMes, fila.DiaSemana, fila.NombreDia, fila.EsFinDeSemana);
    }

    protected override string SentenciaMerge => @"
        MERGE dbo.DimFecha AS destino
        USING #Origen AS origen
            ON destino.FechaKey = origen.FechaKey
        WHEN NOT MATCHED BY TARGET
            THEN INSERT (FechaKey, Fecha, Anio, Trimestre, NombreTrimestre, AnioTrimestre,
                         Mes, NombreMes, AnioMes, InicioSemana, DiaMes,
                         DiaSemana, NombreDia, EsFinDeSemana)
                 VALUES (origen.FechaKey, origen.Fecha, origen.Anio, origen.Trimestre,
                         origen.NombreTrimestre, origen.AnioTrimestre, origen.Mes,
                         origen.NombreMes, origen.AnioMes, origen.InicioSemana,
                         origen.DiaMes, origen.DiaSemana, origen.NombreDia, origen.EsFinDeSemana)
        OUTPUT $action INTO @Acciones;";
}
