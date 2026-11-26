namespace BezierCore;

/// <summary>
/// Algoritmo de De Casteljau: interpolación lineal repetida entre puntos de
/// control hasta reducirlos a un único punto de la curva.
///
///     P_i^(r)(t) = (1-t) · P_i^(r-1) + t · P_{i+1}^(r-1)
///
/// Complejidad: O(n²) por cada valor de t (n niveles, cada uno con una
/// interpolación menos), pero numéricamente más estable que la evaluación
/// directa porque sólo realiza combinaciones convexas (sin potencias ni
/// coeficientes binomiales grandes).
/// </summary>
public sealed class DeCasteljauEvaluator : IBezierEvaluator
{
    public string Name => "De Casteljau";

    public ControlPoint Evaluate(IReadOnlyList<ControlPoint> controlPoints, double t)
    {
        ValidateControlPoints(controlPoints);

        // Buffer de trabajo: se sobrescribe nivel a nivel.
        int count = controlPoints.Count;
        var working = new ControlPoint[count];
        for (int i = 0; i < count; i++)
            working[i] = controlPoints[i];

        for (int level = count - 1; level > 0; level--)
        {
            for (int i = 0; i < level; i++)
            {
                working[i] = Lerp(working[i], working[i + 1], t);
            }
        }

        return working[0];
    }

    public IReadOnlyList<ControlPoint> Sample(IReadOnlyList<ControlPoint> controlPoints, int sampleCount)
    {
        ValidateControlPoints(controlPoints);
        if (sampleCount < 2)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Se requieren al menos 2 muestras.");

        var samples = new ControlPoint[sampleCount];
        for (int k = 0; k < sampleCount; k++)
        {
            double t = (double)k / (sampleCount - 1);
            samples[k] = Evaluate(controlPoints, t);
        }

        return samples;
    }

    /// <summary>
    /// Devuelve TODOS los niveles intermedios de la construcción para un t dado.
    /// El nivel 0 son los puntos de control originales y el último nivel contiene
    /// el único punto sobre la curva.
    ///
    /// Requerimiento del enunciado: "Visualizar las interpolaciones de De Casteljau".
    /// Este método es el que debe consumir la interfaz gráfica para dibujar los
    /// polígonos intermedios; NO debe usarse para medir tiempos, ya que asignar
    /// memoria para cada nivel distorsiona el benchmark.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ControlPoint>> EvaluateWithSteps(
        IReadOnlyList<ControlPoint> controlPoints,
        double t)
    {
        ValidateControlPoints(controlPoints);

        var levels = new List<IReadOnlyList<ControlPoint>>();
        var current = controlPoints.ToArray();
        levels.Add(current);

        while (current.Length > 1)
        {
            var next = new ControlPoint[current.Length - 1];
            for (int i = 0; i < next.Length; i++)
            {
                next[i] = Lerp(current[i], current[i + 1], t);
            }

            levels.Add(next);
            current = next;
        }

        return levels;
    }

    /// <summary>Interpolación lineal: (1-t)·a + t·b.</summary>
    private static ControlPoint Lerp(ControlPoint a, ControlPoint b, double t)
        => new(
            (1.0 - t) * a.X + t * b.X,
            (1.0 - t) * a.Y + t * b.Y);

    private static void ValidateControlPoints(IReadOnlyList<ControlPoint> controlPoints)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);
        if (controlPoints.Count < 2)
            throw new ArgumentException("Se requieren al menos 2 puntos de control.", nameof(controlPoints));
    }
}
