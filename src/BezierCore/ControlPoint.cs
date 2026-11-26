namespace BezierCore;

/// <summary>
/// Punto de control bidimensional de una curva de Bézier.
/// Se usa <see cref="double"/> (no float) para no perder precisión en el
/// análisis de estabilidad numérica entre Bernstein y De Casteljau.
/// </summary>
public readonly struct ControlPoint
{
    public double X { get; }
    public double Y { get; }

    public ControlPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static ControlPoint operator +(ControlPoint a, ControlPoint b)
        => new(a.X + b.X, a.Y + b.Y);

    public static ControlPoint operator -(ControlPoint a, ControlPoint b)
        => new(a.X - b.X, a.Y - b.Y);

    public static ControlPoint operator *(ControlPoint p, double scalar)
        => new(p.X * scalar, p.Y * scalar);

    public static ControlPoint operator *(double scalar, ControlPoint p)
        => p * scalar;

    /// <summary>
    /// Distancia euclidiana entre dos puntos. Se utiliza para medir la
    /// diferencia numérica entre los puntos generados por ambos algoritmos.
    /// </summary>
    public static double Distance(ControlPoint a, ControlPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString() => $"({X:F6}, {Y:F6})";
}
