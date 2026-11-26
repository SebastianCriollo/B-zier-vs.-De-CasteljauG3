using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BezierApp.Benchmark
{
    internal static class Program
    {
        private static void Main()
        {
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultados");
            Directory.CreateDirectory(outputDirectory);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

            RunPerformanceScenario(Path.Combine(outputDirectory, "comparacion-" + stamp + ".csv"));
            Console.WriteLine();
            RunStabilityScenario(Path.Combine(outputDirectory, "estabilidad-" + stamp + ".csv"));

            Console.WriteLine();
            Console.WriteLine("Presione una tecla para salir...");
            Console.ReadKey();
        }

        /// <summary>
        /// Tiempos con Stopwatch y diferencia numérica entre ambos métodos
        /// variando puntos de control y muestras (10 repeticiones por escenario).
        /// </summary>
        private static void RunPerformanceScenario(string csvPath)
        {
            // Puntos de control = grado + 1. Se llega hasta grado 100 para ubicar el
            // grado a partir del cual cambia cuál método es más rápido (si existe).
            int[] controlPointCounts = { 4, 6, 8, 11, 16, 21, 31, 41, 51, 76, 101 };
            int[] sampleCounts = { 100, 1000, 10000 };

            var runner = new BenchmarkRunner
            {
                Repetitions = 10,   // mínimo exigido por el enunciado
                WarmupRuns = 3
            };

            Console.WriteLine("=== 1. RENDIMIENTO: Bernstein (directo) vs. De Casteljau ===");
            Console.WriteLine("Repeticiones por escenario: {0} (se excluye el tiempo de dibujo)", runner.Repetitions);
            Console.WriteLine();
            Console.WriteLine("{0,6} {1,10} {2,14} {3,14} {4,8} {5,14}",
                "Grado", "Muestras", "Bernstein ms", "Casteljau ms", "Razón", "Dif. máx.");
            Console.WriteLine(new string('-', 70));

            var results = runner.RunMatrix(controlPointCounts, sampleCounts);

            foreach (var r in results)
            {
                Console.WriteLine("{0,6} {1,10} {2,14:F4} {3,14:F4} {4,8:F2} {5,14:E3}",
                    r.Degree, r.SampleCount,
                    r.BernsteinMeanMs, r.DeCasteljauMeanMs,
                    r.SpeedRatio, r.MaxNumericDifference);
            }

            using (var writer = new StreamWriter(csvPath))
            {
                writer.WriteLine(ComparisonResult.CsvHeader);
                foreach (var r in results)
                    writer.WriteLine(r.ToCsvRow());
            }

            Console.WriteLine();
            Console.WriteLine("(Razón = tiempo De Casteljau / tiempo Bernstein: menor que 1 => De Casteljau es más rápido)");
            Console.WriteLine("Resultados exportados a: " + csvPath);
        }

        /// <summary>
        /// Estabilidad numérica: error de cada método frente a una referencia casi
        /// exacta, con grados altos y valores de t cercanos a los extremos, repetido
        /// con varias semillas (distintos puntos de control).
        /// Es el insumo principal para responder la pregunta de investigación.
        /// </summary>
        private static void RunStabilityScenario(string csvPath)
        {
            // Grados desde cúbica hasta valores donde Bernstein desborda (> ~1030).
            int[] degrees = { 3, 10, 30, 100, 500, 1000, 1100 };
            int[] seeds = { 20261126, 11, 22, 33, 44 };

            Console.WriteLine("=== 2. ESTABILIDAD NUMÉRICA: error frente a la referencia exacta ===");
            Console.WriteLine("Referencia: De Casteljau en decimal (28 dígitos). NaN = el método desbordó.");
            Console.WriteLine("{0} semillas (conjuntos distintos de puntos de control) y 7 valores de t por grado.", seeds.Length);
            Console.WriteLine("Los grados 1000+ pueden tardar de 20 a 40 segundos por la referencia en decimal...");
            Console.WriteLine();

            var analyzer = new StabilityAnalyzer();
            IReadOnlyList<StabilityResult> results = analyzer.Run(degrees, seeds);

            Console.WriteLine("Peor error entre los 7 valores de t, resumido sobre las {0} semillas:", seeds.Length);
            Console.WriteLine("{0,6} {1,14} {2,14} {3,14} {4,14} {5,12}",
                "Grado", "Bern. media", "Bern. máx.", "Cast. media", "Cast. máx.", "Bern. mejor");
            Console.WriteLine(new string('-', 80));

            foreach (int degree in degrees)
            {
                // Peor error de cada método dentro de cada semilla (máximo sobre los t).
                var perSeed = seeds
                    .Select(seed => results.Where(r => r.Degree == degree && r.Seed == seed).ToList())
                    .ToList();

                var bernsteinWorst = perSeed.Select(rows => WorstError(rows.Select(r => r.BernsteinError))).ToList();
                var casteljauWorst = perSeed.Select(rows => WorstError(rows.Select(r => r.DeCasteljauError))).ToList();

                // En cuántas semillas Bernstein tuvo estrictamente menos error (NaN = no gana).
                int bernsteinWins = 0;
                for (int i = 0; i < seeds.Length; i++)
                {
                    if (!double.IsNaN(bernsteinWorst[i]) && bernsteinWorst[i] < casteljauWorst[i])
                        bernsteinWins++;
                }

                Console.WriteLine("{0,6} {1,14} {2,14} {3,14} {4,14} {5,12}",
                    degree,
                    FormatError(MeanOrNaN(bernsteinWorst)),
                    FormatError(WorstError(bernsteinWorst)),
                    FormatError(MeanOrNaN(casteljauWorst)),
                    FormatError(WorstError(casteljauWorst)),
                    bernsteinWins + "/" + seeds.Length);
            }

            Console.WriteLine();
            Console.WriteLine("(Bern. mejor = semillas en las que Bernstein tuvo menor error que De Casteljau)");

            // El CSV conserva el detalle completo: una fila por semilla, grado y t.
            using (var writer = new StreamWriter(csvPath))
            {
                writer.WriteLine(StabilityResult.CsvHeader);
                foreach (var r in results)
                    writer.WriteLine(r.ToCsvRow());
            }

            Console.WriteLine("Resultados exportados a: " + csvPath);
        }

        private static string FormatError(double error)
        {
            return double.IsNaN(error)
                ? "NaN"
                : error.ToString("E3", CultureInfo.InvariantCulture);
        }

        /// <summary>Peor error de una serie; si alguno es NaN, el método falló y se devuelve NaN.</summary>
        private static double WorstError(IEnumerable<double> errors)
        {
            double worst = 0.0;
            foreach (double error in errors)
            {
                if (double.IsNaN(error)) return double.NaN;
                if (error > worst) worst = error;
            }

            return worst;
        }

        /// <summary>Promedio de una serie; si alguno es NaN, devuelve NaN.</summary>
        private static double MeanOrNaN(IList<double> values)
        {
            foreach (double value in values)
            {
                if (double.IsNaN(value)) return double.NaN;
            }

            return values.Average();
        }
    }
}
