using System;

namespace BezierCore
{
    /// <summary>
    /// Punto de control bidimensional de una curva de Bézier.
    /// Se usa double (no float) para no perder precisión en el análisis de
    /// estabilidad numérica entre Bernstein y De Casteljau.
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
        {
            return new ControlPoint(a.X + b.X, a.Y + b.Y);
        }

        public static ControlPoint operator -(ControlPoint a, ControlPoint b)
        {
            return new ControlPoint(a.X - b.X, a.Y - b.Y);
        }

        public static ControlPoint operator *(ControlPoint p, double scalar)
        {
            return new ControlPoint(p.X * scalar, p.Y * scalar);
        }

        public static ControlPoint operator *(double scalar, ControlPoint p)
        {
            return p * scalar;
        }

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

        public override string ToString()
        {
            return string.Format("({0:F6}, {1:F6})", X, Y);
        }
    }
}
