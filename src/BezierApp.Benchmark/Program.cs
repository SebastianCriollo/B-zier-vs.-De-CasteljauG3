using System;
using System.Globalization;
using System.IO;

namespace BezierApp.Benchmark
{
    internal static class Program
    {
        private static void Main()
        {
            // Escenarios de experimentación. Ajustar según lo que se reporte en el informe.
            int[] controlPointCounts = { 4, 6, 8, 11, 16, 21, 31 };
            int[] sampleCounts = { 100, 1000, 10000 };

            var runner = new BenchmarkRunner
            {
                Repetitions = 10,   // mínimo exigido por el enunciado
                WarmupRuns = 3
            };

            Console.WriteLine("Comparación: Bernstein (directo) vs. De Casteljau");
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

            // Exporta los resultados para armar las tablas y gráficos del informe.
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultados");
            Directory.CreateDirectory(outputDirectory);
            string csvPath = Path.Combine(
                outputDirectory,
                "comparacion-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv");

            using (var writer = new StreamWriter(csvPath))
            {
                writer.WriteLine(ComparisonResult.CsvHeader);
                foreach (var r in results)
                    writer.WriteLine(r.ToCsvRow());
            }

            Console.WriteLine();
            Console.WriteLine("Resultados exportados a: " + csvPath);
            Console.WriteLine("Presione una tecla para salir...");
            Console.ReadKey();

            // TODO (SebastianCriollo): agregar un escenario de estabilidad numérica con
            //   grados altos (p. ej. 30-50 puntos) y t cercano a los extremos, donde la
            //   evaluación directa pierde precisión frente a De Casteljau. Ese es el
            //   insumo principal para responder la pregunta de investigación.
        }
    }
}
