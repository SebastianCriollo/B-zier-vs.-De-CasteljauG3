using System;
using System.Collections.Generic;
using System.Numerics;
using BezierCore;

namespace BezierApp.Benchmark
{
    /// <summary>
    /// Escenario de ESTABILIDAD NUMÉRICA: compara el error de Bernstein y de
    /// De Casteljau contra un resultado de referencia casi exacto.
    ///
    /// Cómo se obtiene la referencia:
    ///   - Los puntos de control son enteros, así que son EXACTOS tanto en double
    ///     como en decimal.
    ///   - Los valores de t son potencias de 2 (0, 2^-20, 2^-10, 0.5, ...), que
    ///     también son exactos en double y en decimal.
    ///   - Con entradas idénticas, la referencia se calcula con De Casteljau en
    ///     decimal (28 dígitos significativos, contra ~16 de double). Su error
    ///     (~1e-25) es mucho menor que el de cualquiera de los dos métodos en double.
    ///
    /// El error se mide restando valores EXACTOS (sin pasar por conversiones
    /// double-decimal que redondean a 15 dígitos y taparían el error real).
    ///
    /// Responsable: SebastianCriollo
    /// </summary>
    public sealed class StabilityAnalyzer
    {
        // Mismos valores de t en double y en decimal. Todos son exactos en binario.
        private static readonly double[] TDouble =
        {
            0.0, 0.00000095367431640625, 0.0009765625, 0.5,
            0.9990234375, 0.99999904632568359375, 1.0
        };

        private static readonly decimal[] TDecimal =
        {
            0m, 0.00000095367431640625m, 0.0009765625m, 0.5m,
            0.9990234375m, 0.99999904632568359375m, 1m
        };

        // 10^28: escala con la que se comparan double y decimal sin perder dígitos.
        private static readonly BigInteger Scale = BigInteger.Pow(10, 28);

        private readonly BernsteinEvaluator _bernstein = new BernsteinEvaluator();
        private readonly DeCasteljauEvaluator _deCasteljau = new DeCasteljauEvaluator();

        /// <summary>
        /// Ejecuta el escenario para cada combinación de grado y semilla. Cada
        /// semilla genera un conjunto distinto (pero reproducible) de puntos de
        /// control, así la conclusión no depende de una sola configuración.
        /// </summary>
        public IReadOnlyList<StabilityResult> Run(IEnumerable<int> degrees, IEnumerable<int> seeds)
        {
            var results = new List<StabilityResult>();

            foreach (int degree in degrees)
            foreach (int seed in seeds)
            {
                int count = degree + 1;
                var random = new Random(seed);

                var xs = new int[count];
                var ys = new int[count];
                var points = new ControlPoint[count];
                var decimalX = new decimal[count];
                var decimalY = new decimal[count];

                for (int i = 0; i < count; i++)
                {
                    xs[i] = i * 10;
                    ys[i] = random.Next(0, 501);

                    points[i] = new ControlPoint(xs[i], ys[i]);
                    decimalX[i] = xs[i];
                    decimalY[i] = ys[i];
                }

                for (int k = 0; k < TDouble.Length; k++)
                {
                    double t = TDouble[k];

                    ControlPoint fromBernstein = EvaluateBernstein(points, t);
                    ControlPoint fromCasteljau = _deCasteljau.Evaluate(points, t);

                    Tuple<decimal, decimal> reference = ReferenceDeCasteljau(decimalX, decimalY, TDecimal[k]);

                    results.Add(new StabilityResult
                    {
                        Seed = seed,
                        Degree = degree,
                        T = t,
                        BernsteinError = ErrorAgainst(fromBernstein, reference),
                        DeCasteljauError = ErrorAgainst(fromCasteljau, reference)
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Evalúa con Bernstein. Con grados muy altos los coeficientes binomiales
        /// exceden el rango de double y BezierCore lanza ArgumentOutOfRangeException
        /// (versiones anteriores devolvían NaN). En ambos casos el método NO pudo
        /// calcular el punto, así que se registra como fallo (NaN).
        /// </summary>
        private ControlPoint EvaluateBernstein(IReadOnlyList<ControlPoint> points, double t)
        {
            try
            {
                return _bernstein.Evaluate(points, t);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new ControlPoint(double.NaN, double.NaN);
            }
        }

        /// <summary>De Casteljau en decimal: la referencia casi exacta.</summary>
        private static Tuple<decimal, decimal> ReferenceDeCasteljau(decimal[] xs, decimal[] ys, decimal t)
        {
            var x = (decimal[])xs.Clone();
            var y = (decimal[])ys.Clone();
            decimal oneMinusT = 1m - t;

            for (int level = x.Length - 1; level > 0; level--)
            {
                for (int i = 0; i < level; i++)
                {
                    x[i] = oneMinusT * x[i] + t * x[i + 1];
                    y[i] = oneMinusT * y[i] + t * y[i + 1];
                }
            }

            return Tuple.Create(x[0], y[0]);
        }

        /// <summary>
        /// Distancia euclidiana entre el punto calculado y la referencia.
        /// Devuelve NaN si el método produjo un valor no finito.
        /// </summary>
        private static double ErrorAgainst(ControlPoint point, Tuple<decimal, decimal> reference)
        {
            if (!IsFinite(point.X) || !IsFinite(point.Y))
                return double.NaN;

            double ex = ExactDifference(point.X, reference.Item1);
            double ey = ExactDifference(point.Y, reference.Item2);
            return Math.Sqrt(ex * ex + ey * ey);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>|value - reference| calculada con aritmética entera exacta.</summary>
        private static double ExactDifference(double value, decimal reference)
        {
            BigInteger difference = BigInteger.Abs(ToScaled(value) - ToScaled(reference));
            return (double)difference / 1e28;
        }

        /// <summary>
        /// Valor exacto de un double multiplicado por 10^28.
        /// Un double es mantisa * 2^exponente, así que no hay redondeo decimal.
        /// </summary>
        private static BigInteger ToScaled(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            bool negative = bits < 0;
            int exponent = (int)((bits >> 52) & 0x7FFL);
            long mantissa = bits & 0xFFFFFFFFFFFFFL;

            if (exponent == 0)
                exponent++;                 // número subnormal
            else
                mantissa |= 1L << 52;       // bit implícito

            exponent -= 1075;               // value = mantissa * 2^exponent

            BigInteger scaled = new BigInteger(mantissa) * Scale;
            if (exponent > 0)
                scaled <<= exponent;
            else
                scaled >>= -exponent;

            return negative ? -scaled : scaled;
        }

        /// <summary>Valor exacto de un decimal multiplicado por 10^28.</summary>
        private static BigInteger ToScaled(decimal value)
        {
            int[] parts = decimal.GetBits(value);

            BigInteger integer = ((BigInteger)(uint)parts[2] << 64)
                               | ((BigInteger)(uint)parts[1] << 32)
                               | (BigInteger)(uint)parts[0];

            int decimalPlaces = (parts[3] >> 16) & 0xFF;
            BigInteger scaled = integer * BigInteger.Pow(10, 28 - decimalPlaces);

            return parts[3] < 0 ? -scaled : scaled;
        }
    }
}
