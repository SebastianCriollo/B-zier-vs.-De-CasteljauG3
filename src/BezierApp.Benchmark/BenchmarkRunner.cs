using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BezierCore;

namespace BezierApp.Benchmark
{
    /// <summary>
    /// Ejecuta los escenarios de experimentación variando la cantidad de puntos de
    /// control y de muestras, midiendo tiempos con Stopwatch y calculando las
    /// diferencias numéricas entre ambos algoritmos.
    ///
    /// Reglas del enunciado respetadas aquí:
    ///   - Cada prueba se ejecuta al menos 10 veces.
    ///   - Se excluye el tiempo de representación gráfica (este proyecto es de
    ///     consola: no dibuja nada).
    ///
    /// Responsable: SebastianCriollo
    /// </summary>
    public sealed class BenchmarkRunner
    {
        private readonly BernsteinEvaluator _bernstein = new BernsteinEvaluator();
        private readonly DeCasteljauEvaluator _deCasteljau = new DeCasteljauEvaluator();

        public BenchmarkRunner()
        {
            Repetitions = 10;
            WarmupRuns = 3;
        }

        /// <summary>Repeticiones por escenario. El enunciado exige mínimo 10.</summary>
        public int Repetitions { get; set; }

        /// <summary>
        /// Corridas de calentamiento descartadas antes de medir, para que el JIT no
        /// contamine la primera medición. Es un detalle que conviene mencionar en el
        /// informe al justificar la metodología.
        /// </summary>
        public int WarmupRuns { get; set; }

        public ComparisonResult Run(int controlPointCount, int sampleCount)
        {
            var controlPoints = GenerateControlPoints(controlPointCount);

            // Calentamiento (no se mide).
            for (int i = 0; i < WarmupRuns; i++)
            {
                _bernstein.Sample(controlPoints, sampleCount);
                _deCasteljau.Sample(controlPoints, sampleCount);
            }

            var bernsteinTimes = new double[Repetitions];
            var casteljauTimes = new double[Repetitions];

            for (int i = 0; i < Repetitions; i++)
            {
                bernsteinTimes[i] = MeasureMs(() => _bernstein.Sample(controlPoints, sampleCount));
                casteljauTimes[i] = MeasureMs(() => _deCasteljau.Sample(controlPoints, sampleCount));
            }

            var difference = CompareNumerically(controlPoints, sampleCount);

            return new ComparisonResult
            {
                ControlPointCount = controlPointCount,
                SampleCount = sampleCount,
                Repetitions = Repetitions,

                BernsteinMeanMs = Mean(bernsteinTimes),
                BernsteinStdDevMs = StdDev(bernsteinTimes),
                BernsteinMinMs = bernsteinTimes.Min(),
                BernsteinMaxMs = bernsteinTimes.Max(),

                DeCasteljauMeanMs = Mean(casteljauTimes),
                DeCasteljauStdDevMs = StdDev(casteljauTimes),
                DeCasteljauMinMs = casteljauTimes.Min(),
                DeCasteljauMaxMs = casteljauTimes.Max(),

                MaxNumericDifference = difference.Item1,
                MeanNumericDifference = difference.Item2
            };
        }

        /// <summary>
        /// Ejecuta la matriz completa de escenarios. Ajustar estos arreglos según lo
        /// que se quiera reportar en las tablas comparativas.
        /// </summary>
        public IReadOnlyList<ComparisonResult> RunMatrix(
            IEnumerable<int> controlPointCounts,
            IEnumerable<int> sampleCounts)
        {
            var results = new List<ComparisonResult>();

            foreach (int points in controlPointCounts)
            {
                foreach (int samples in sampleCounts)
                {
                    results.Add(Run(points, samples));
                }
            }

            return results;
        }

        private static double MeasureMs(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Compara punto por punto los resultados de ambos métodos para el mismo
        /// conjunto de parámetros t, lo que evidencia la pérdida de precisión de la
        /// evaluación directa cuando el grado crece.
        /// Devuelve (diferencia máxima, diferencia promedio).
        /// </summary>
        private Tuple<double, double> CompareNumerically(
            IReadOnlyList<ControlPoint> controlPoints,
            int sampleCount)
        {
            var fromBernstein = _bernstein.Sample(controlPoints, sampleCount);
            var fromCasteljau = _deCasteljau.Sample(controlPoints, sampleCount);

            double max = 0.0;
            double sum = 0.0;

            for (int i = 0; i < sampleCount; i++)
            {
                double d = ControlPoint.Distance(fromBernstein[i], fromCasteljau[i]);
                if (d > max) max = d;
                sum += d;
            }

            return Tuple.Create(max, sum / sampleCount);
        }

        /// <summary>
        /// Genera puntos de control deterministas (misma semilla) para que los
        /// escenarios sean reproducibles entre corridas y entre máquinas.
        /// </summary>
        public static IReadOnlyList<ControlPoint> GenerateControlPoints(int count, int seed = 20261126)
        {
            if (count < 2)
                throw new ArgumentOutOfRangeException("count", "Se requieren al menos 2 puntos de control.");

            var random = new Random(seed);
            var points = new ControlPoint[count];

            for (int i = 0; i < count; i++)
            {
                double x = i * (800.0 / (count - 1));
                double y = random.NextDouble() * 500.0;
                points[i] = new ControlPoint(x, y);
            }

            return points;
        }

        private static double Mean(double[] values)
        {
            return values.Average();
        }

        private static double StdDev(double[] values)
        {
            if (values.Length < 2) return 0.0;
            double mean = values.Average();
            double sumSquares = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSquares / (values.Length - 1));
        }
    }
}
