namespace BezierApp.Benchmark;

/// <summary>
/// Resultado de un escenario de prueba: un número fijo de puntos de control y
/// de muestras, ejecutado N veces con cada algoritmo.
/// Los tiempos están en milisegundos y NO incluyen la representación gráfica.
/// </summary>
public sealed record ComparisonResult
{
    /// <summary>Número de puntos de control (grado = ControlPointCount - 1).</summary>
    public required int ControlPointCount { get; init; }

    /// <summary>Cantidad de muestras generadas por corrida.</summary>
    public required int SampleCount { get; init; }

    /// <summary>Repeticiones ejecutadas (el enunciado exige al menos 10).</summary>
    public required int Repetitions { get; init; }

    public required double BernsteinMeanMs { get; init; }
    public required double BernsteinStdDevMs { get; init; }
    public required double BernsteinMinMs { get; init; }
    public required double BernsteinMaxMs { get; init; }

    public required double DeCasteljauMeanMs { get; init; }
    public required double DeCasteljauStdDevMs { get; init; }
    public required double DeCasteljauMinMs { get; init; }
    public required double DeCasteljauMaxMs { get; init; }

    /// <summary>Diferencia euclidiana máxima entre puntos homólogos de ambos métodos.</summary>
    public required double MaxNumericDifference { get; init; }

    /// <summary>Diferencia euclidiana promedio entre puntos homólogos.</summary>
    public required double MeanNumericDifference { get; init; }

    public int Degree => ControlPointCount - 1;

    /// <summary>Cuántas veces más lento resultó De Casteljau respecto a Bernstein.</summary>
    public double SpeedRatio => BernsteinMeanMs > 0 ? DeCasteljauMeanMs / BernsteinMeanMs : double.NaN;

    public static string CsvHeader =>
        "puntos_control,grado,muestras,repeticiones," +
        "bernstein_media_ms,bernstein_desv_ms,bernstein_min_ms,bernstein_max_ms," +
        "casteljau_media_ms,casteljau_desv_ms,casteljau_min_ms,casteljau_max_ms," +
        "dif_max,dif_media,razon_tiempo";

    public string ToCsvRow() =>
        $"{ControlPointCount},{Degree},{SampleCount},{Repetitions}," +
        $"{BernsteinMeanMs:F6},{BernsteinStdDevMs:F6},{BernsteinMinMs:F6},{BernsteinMaxMs:F6}," +
        $"{DeCasteljauMeanMs:F6},{DeCasteljauStdDevMs:F6},{DeCasteljauMinMs:F6},{DeCasteljauMaxMs:F6}," +
        $"{MaxNumericDifference:E6},{MeanNumericDifference:E6},{SpeedRatio:F3}";
}
