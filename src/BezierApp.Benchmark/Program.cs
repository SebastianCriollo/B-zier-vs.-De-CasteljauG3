using BezierApp.Benchmark;

// Escenarios de experimentación. Ajustar según lo que se reporte en el informe.
int[] controlPointCounts = [4, 6, 8, 11, 16, 21, 31];
int[] sampleCounts = [100, 1_000, 10_000];

var runner = new BenchmarkRunner
{
    Repetitions = 10,   // mínimo exigido por el enunciado
    WarmupRuns = 3
};

Console.WriteLine("Comparación: Bernstein (directo) vs. De Casteljau");
Console.WriteLine($"Repeticiones por escenario: {runner.Repetitions} (se excluye el tiempo de dibujo)");
Console.WriteLine();
Console.WriteLine($"{"Grado",6} {"Muestras",10} {"Bernstein ms",14} {"Casteljau ms",14} {"Razón",8} {"Dif. máx.",14}");
Console.WriteLine(new string('-', 70));

var results = runner.RunMatrix(controlPointCounts, sampleCounts);

foreach (var r in results)
{
    Console.WriteLine(
        $"{r.Degree,6} {r.SampleCount,10} " +
        $"{r.BernsteinMeanMs,14:F4} {r.DeCasteljauMeanMs,14:F4} " +
        $"{r.SpeedRatio,8:F2} {r.MaxNumericDifference,14:E3}");
}

// Exporta los resultados para armar las tablas y gráficos del informe.
string outputDirectory = Path.Combine(AppContext.BaseDirectory, "resultados");
Directory.CreateDirectory(outputDirectory);
string csvPath = Path.Combine(outputDirectory, $"comparacion-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

await using (var writer = new StreamWriter(csvPath))
{
    await writer.WriteLineAsync(ComparisonResult.CsvHeader);
    foreach (var r in results)
        await writer.WriteLineAsync(r.ToCsvRow());
}

Console.WriteLine();
Console.WriteLine($"Resultados exportados a: {csvPath}");

// TODO (SebastianCriollo): agregar un escenario de estabilidad numérica con
//   grados altos (p. ej. 30-50 puntos) y t cercano a los extremos, donde la
//   evaluación directa pierde precisión frente a De Casteljau. Ese es el
//   insumo principal para responder la pregunta de investigación.
