using System.Text.RegularExpressions;
using OpinionesClientes.Extraccion.Domain.Dimensiones;

namespace OpinionesClientes.Extraccion.Application.CargaDW;

/// <summary>
/// Reglas de TRANSFORMACIÓN de cada dimensión (la "T" del ETL).
///
/// Son métodos estáticos de C# puro: no leen ni escriben en ninguna base
/// de datos. Reciben la fila tal como viene del origen y devuelven la fila
/// limpia, o el motivo por el que se rechaza. Así las reglas del negocio
/// están en un solo lugar y se pueden probar sin SQL Server.
/// </summary>
public static class TransformadorDimensiones
{
    // Nombres en español escritos a mano para no depender del idioma del servidor
    // (los mismos que usa el script que creó DimFecha).
    private static readonly string[] NombresMes =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    private static readonly string[] NombresDia =
    {
        "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"
    };

    private static readonly string[] ClasificacionesValidas = { "Positiva", "Negativa", "Neutra" };

    // ------------------------------------------------------------------
    // DimCliente
    // ------------------------------------------------------------------
    public static ResultadoTransformacion<DimCliente> TransformarCliente(DimCliente origen)
    {
        // Regla 1: la clave de negocio debe ser válida
        if (origen.ClienteId <= 0)
        {
            return ResultadoTransformacion<DimCliente>.Rechazada("IdCliente debe ser mayor que 0");
        }

        // Regla 2: nombre sin espacios sobrantes; si viene vacío se usa uno genérico
        string nombre = LimpiarTexto(origen.NombreCliente);
        if (nombre.Length == 0)
        {
            nombre = $"Cliente {origen.ClienteId}";
        }

        // Regla 3: correo en minúsculas; si no tiene formato de correo se deja vacío (NULL)
        string? email = LimpiarTexto(origen.Email).ToLowerInvariant();
        if (!EsCorreoValido(email))
        {
            email = null;
        }

        var limpio = new DimCliente
        {
            ClienteId = origen.ClienteId,
            NombreCliente = Recortar(nombre, 150),
            Email = email is null ? null : Recortar(email, 150),
            TipoCliente = "Registrado"   // los clientes de la tabla Clientes están registrados
        };

        return ResultadoTransformacion<DimCliente>.Valida(limpio);
    }

    // ------------------------------------------------------------------
    // DimProducto
    // ------------------------------------------------------------------
    public static ResultadoTransformacion<DimProducto> TransformarProducto(DimProducto origen)
    {
        if (origen.ProductoId <= 0)
        {
            return ResultadoTransformacion<DimProducto>.Rechazada("IdProducto debe ser mayor que 0");
        }

        string nombre = LimpiarTexto(origen.NombreProducto);
        if (nombre.Length == 0)
        {
            nombre = $"Producto {origen.ProductoId}";
        }

        string categoria = LimpiarTexto(origen.Categoria);
        if (categoria.Length == 0)
        {
            categoria = "Sin categoría";
        }

        var limpio = new DimProducto
        {
            ProductoId = origen.ProductoId,
            NombreProducto = Recortar(nombre, 150),
            Categoria = Recortar(categoria, 100)
        };

        return ResultadoTransformacion<DimProducto>.Valida(limpio);
    }

    // ------------------------------------------------------------------
    // DimFuente
    // ------------------------------------------------------------------
    public static ResultadoTransformacion<DimFuente> TransformarFuente(DimFuente origen)
    {
        string codigo = LimpiarTexto(origen.CodigoTipoFuente);
        string canal = LimpiarTexto(origen.Canal);

        if (canal.Length == 0)
        {
            return ResultadoTransformacion<DimFuente>.Rechazada("La opinión no tiene canal");
        }

        // El código del origen decide el nombre del tipo de fuente y cómo entra al ETL
        string tipoFuente;
        string medioIngreso;

        switch (codigo)
        {
            case "CSV":
                tipoFuente = "Encuesta interna";
                medioIngreso = "Archivo CSV";
                break;
            case "Web":
                tipoFuente = "Reseña web";
                medioIngreso = "Base de datos relacional";
                break;
            case "Red Social":
                tipoFuente = "Red social";
                medioIngreso = "API REST";
                break;
            default:
                return ResultadoTransformacion<DimFuente>.Rechazada($"Tipo de fuente desconocido: '{codigo}'");
        }

        var limpio = new DimFuente
        {
            CodigoTipoFuente = codigo,
            Canal = Recortar(canal, 30),
            TipoFuente = tipoFuente,
            MedioIngreso = medioIngreso
        };

        return ResultadoTransformacion<DimFuente>.Valida(limpio);
    }

    // ------------------------------------------------------------------
    // DimSentimiento
    // ------------------------------------------------------------------
    public static ResultadoTransformacion<DimSentimiento> TransformarSentimiento(DimSentimiento origen)
    {
        string clasificacion = LimpiarTexto(origen.Clasificacion);

        if (!ClasificacionesValidas.Contains(clasificacion))
        {
            return ResultadoTransformacion<DimSentimiento>.Rechazada($"Clasificación desconocida: '{clasificacion}'");
        }

        // Valor numérico del sentimiento: permite calcular un índice neto con AVG
        short valor = clasificacion switch
        {
            "Positiva" => 1,
            "Negativa" => -1,
            _ => 0
        };

        var limpio = new DimSentimiento
        {
            SentimientoKey = origen.SentimientoKey,
            Clasificacion = clasificacion,
            ValorSentimiento = valor
        };

        return ResultadoTransformacion<DimSentimiento>.Valida(limpio);
    }

    // ------------------------------------------------------------------
    // DimPalabraClave
    // ------------------------------------------------------------------
    public static ResultadoTransformacion<DimPalabraClave> TransformarPalabraClave(DimPalabraClave origen)
    {
        // Las palabras se guardan en minúsculas, igual que las busca el clasificador
        string palabra = LimpiarTexto(origen.Palabra).ToLowerInvariant();
        string tipo = LimpiarTexto(origen.TipoPalabra);

        if (palabra.Length == 0)
        {
            return ResultadoTransformacion<DimPalabraClave>.Rechazada("Palabra vacía");
        }

        if (!ClasificacionesValidas.Contains(tipo))
        {
            return ResultadoTransformacion<DimPalabraClave>.Rechazada($"Tipo de palabra desconocido: '{tipo}'");
        }

        var limpio = new DimPalabraClave
        {
            Palabra = Recortar(palabra, 50),
            TipoPalabra = tipo
        };

        return ResultadoTransformacion<DimPalabraClave>.Valida(limpio);
    }

    // ------------------------------------------------------------------
    // DimFecha
    // ------------------------------------------------------------------

    /// <summary>
    /// Genera un día por fila entre dos fechas (incluidas), con todos los
    /// atributos de calendario que usa DimFecha.
    /// </summary>
    public static List<DimFecha> GenerarCalendario(DateTime desde, DateTime hasta)
    {
        var dias = new List<DimFecha>();

        for (DateTime fecha = desde.Date; fecha <= hasta.Date; fecha = fecha.AddDays(1))
        {
            dias.Add(CrearDia(fecha));
        }

        return dias;
    }

    /// <summary>
    /// Calcula los atributos de un día. Mismas fórmulas que el script SQL
    /// que creó DimFecha, para que las filas nuevas sean iguales a las existentes.
    /// </summary>
    public static DimFecha CrearDia(DateTime fecha)
    {
        // DayOfWeek: Domingo = 0 ... Sábado = 6  ->  se pasa a Lunes = 1 ... Domingo = 7
        int diaSemana = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;
        int trimestre = (fecha.Month - 1) / 3 + 1;

        return new DimFecha
        {
            FechaKey = fecha.Year * 10000 + fecha.Month * 100 + fecha.Day,
            Fecha = fecha.Date,
            Anio = (short)fecha.Year,
            Trimestre = (byte)trimestre,
            NombreTrimestre = $"T{trimestre}",
            AnioTrimestre = $"{fecha.Year}-T{trimestre}",
            Mes = (byte)fecha.Month,
            NombreMes = NombresMes[fecha.Month - 1],
            AnioMes = $"{fecha.Year}-{fecha.Month:00}",
            InicioSemana = fecha.Date.AddDays(1 - diaSemana),
            DiaMes = (byte)fecha.Day,
            DiaSemana = (byte)diaSemana,
            NombreDia = NombresDia[diaSemana - 1],
            EsFinDeSemana = diaSemana >= 6
        };
    }

    // ------------------------------------------------------------------
    // Funciones de apoyo
    // ------------------------------------------------------------------

    /// <summary>Quita espacios al inicio y al final, y deja un solo espacio entre palabras.</summary>
    private static string LimpiarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        return Regex.Replace(texto.Trim(), @"\s+", " ");
    }

    /// <summary>Corta el texto al tamaño máximo de la columna destino.</summary>
    private static string Recortar(string texto, int maximo)
    {
        return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
    }

    /// <summary>Validación sencilla: algo@algo.algo, sin espacios.</summary>
    private static bool EsCorreoValido(string correo)
    {
        return Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
