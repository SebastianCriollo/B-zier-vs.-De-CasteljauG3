namespace BezierApp.Benchmark
{
    /// <summary>
    /// Resultado de un escenario de prueba: un número fijo de puntos de control y
    /// de muestras, ejecutado N veces con cada algoritmo.
    /// Los tiempos están en milisegundos y NO incluyen la representación gráfica.
    /// </summary>
    public sealed class ComparisonResult
    {
        /// <summary>Número de puntos de control (grado = ControlPointCount - 1).</summary>
        public int ControlPointCount { get; set; }

        /// <summary>Cantidad de muestras generadas por corrida.</summary>
        public int SampleCount { get; set; }

        /// <summary>Repeticiones ejecutadas (el enunciado exige al menos 10).</summary>
        public int Repetitions { get; set; }

        public double BernsteinMeanMs { get; set; }
        public double BernsteinStdDevMs { get; set; }
        public double BernsteinMinMs { get; set; }
        public double BernsteinMaxMs { get; set; }

        public double DeCasteljauMeanMs { get; set; }
        public double DeCasteljauStdDevMs { get; set; }
        public double DeCasteljauMinMs { get; set; }
        public double DeCasteljauMaxMs { get; set; }

        /// <summary>Diferencia euclidiana máxima entre puntos homólogos de ambos métodos.</summary>
        public double MaxNumericDifference { get; set; }

        /// <summary>Diferencia euclidiana promedio entre puntos homólogos.</summary>
        public double MeanNumericDifference { get; set; }

        public int Degree
        {
            get { return ControlPointCount - 1; }
        }

        /// <summary>Cuántas veces más lento resultó De Casteljau respecto a Bernstein.</summary>
        public double SpeedRatio
        {
            get { return BernsteinMeanMs > 0 ? DeCasteljauMeanMs / BernsteinMeanMs : double.NaN; }
        }

        public static string CsvHeader
        {
            get
            {
                return "puntos_control,grado,muestras,repeticiones," +
                       "bernstein_media_ms,bernstein_desv_ms,bernstein_min_ms,bernstein_max_ms," +
                       "casteljau_media_ms,casteljau_desv_ms,casteljau_min_ms,casteljau_max_ms," +
                       "dif_max,dif_media,razon_tiempo";
            }
        }

        public string ToCsvRow()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3}," +
                "{4:F6},{5:F6},{6:F6},{7:F6}," +
                "{8:F6},{9:F6},{10:F6},{11:F6}," +
                "{12:E6},{13:E6},{14:F3}",
                ControlPointCount, Degree, SampleCount, Repetitions,
                BernsteinMeanMs, BernsteinStdDevMs, BernsteinMinMs, BernsteinMaxMs,
                DeCasteljauMeanMs, DeCasteljauStdDevMs, DeCasteljauMinMs, DeCasteljauMaxMs,
                MaxNumericDifference, MeanNumericDifference, SpeedRatio);
        }
    }
}
