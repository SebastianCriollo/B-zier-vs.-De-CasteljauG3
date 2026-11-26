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
            // Escenarios de experimentación. Ajustar según lo que se reporte en el informe.
            int[] controlPointCounts = { 4, 6, 8, 11, 16, 21, 31 };
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
            Console.WriteLine("Resultados exportados a: " + csvPath);
        }

        /// <summary>
        /// Estabilidad numérica: error de cada método frente a una referencia casi
        /// exacta, con grados altos y valores de t cercanos a los extremos.
        /// Es el insumo principal para responder la pregunta de investigación.
        /// </summary>
        private static void RunStabilityScenario(string csvPath)
        {
            // Grados desde cúbica hasta valores donde Bernstein desborda (> ~1030).
            int[] degrees = { 3, 10, 30, 100, 500, 1000, 1100 };

            Console.WriteLine("=== 2. ESTABILIDAD NUMÉRICA: error frente a la referencia exacta ===");
            Console.WriteLine("Referencia: De Casteljau en decimal (28 dígitos). NaN = el método desbordó.");
            Console.WriteLine("(El grado 1000+ puede tardar unos segundos por la referencia en decimal.)");
            Console.WriteLine();

            var analyzer = new StabilityAnalyzer();
            IReadOnlyList<StabilityResult> results = analyzer.Run(degrees);

            Console.WriteLine("{0,6} {1,24} {2,16} {3,16}", "Grado", "t", "Error Bernstein", "Error Casteljau");
            Console.WriteLine(new string('-', 66));

            foreach (var r in results)
            {
                Console.WriteLine("{0,6} {1,24:R} {2,16} {3,16}",
                    r.Degree, r.T, FormatError(r.BernsteinError), FormatError(r.DeCasteljauError));
            }

            Console.WriteLine();
            Console.WriteLine("Resumen por grado (peor error entre todos los t):");
            Console.WriteLine("{0,6} {1,16} {2,16}", "Grado", "Máx. Bernstein", "Máx. Casteljau");
            Console.WriteLine(new string('-', 42));

            foreach (int degree in degrees)
            {
                var rows = results.Where(r => r.Degree == degree).ToList();
                Console.WriteLine("{0,6} {1,16} {2,16}",
                    degree,
                    FormatError(WorstError(rows.Select(r => r.BernsteinError))),
                    FormatError(WorstError(rows.Select(r => r.DeCasteljauError))));
            }

            using (var writer = new StreamWriter(csvPath))
            {
                writer.WriteLine(StabilityResult.CsvHeader);
                foreach (var r in results)
                    writer.WriteLine(r.ToCsvRow());
            }

            Console.WriteLine();
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
    }
}
