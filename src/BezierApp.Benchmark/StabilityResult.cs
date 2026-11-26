using System;
using System.Globalization;

namespace BezierApp.Benchmark
{
    /// <summary>
    /// Error de cada método frente a la referencia exacta, para un grado y un
    /// valor de t. Un error NaN significa que el método devolvió un valor no
    /// finito (desbordamiento) y no pudo calcular el punto.
    /// </summary>
    public sealed class StabilityResult
    {
        public int Degree { get; set; }

        public double T { get; set; }

        /// <summary>Distancia euclidiana entre el punto de Bernstein y la referencia.</summary>
        public double BernsteinError { get; set; }

        /// <summary>Distancia euclidiana entre el punto de De Casteljau y la referencia.</summary>
        public double DeCasteljauError { get; set; }

        public bool BernsteinIsFinite
        {
            get { return !double.IsNaN(BernsteinError); }
        }

        public static string CsvHeader
        {
            get { return "grado,t,error_bernstein,error_casteljau,bernstein_finito"; }
        }

        public string ToCsvRow()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1:R},{2:E6},{3:E6},{4}",
                Degree, T, BernsteinError, DeCasteljauError, BernsteinIsFinite ? "si" : "no");
        }
    }
}
