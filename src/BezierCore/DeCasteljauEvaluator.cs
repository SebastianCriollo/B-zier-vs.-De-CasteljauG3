using System;
using System.Collections.Generic;

namespace BezierCore
{
    /// <summary>
    /// Algoritmo de De Casteljau: interpolación lineal repetida entre puntos de
    /// control hasta reducirlos a un único punto de la curva.
    ///
    ///     P(i,r)(t) = (1-t) * P(i,r-1) + t * P(i+1,r-1)
    ///
    /// Complejidad: O(n^2) por cada valor de t (n niveles, cada uno con una
    /// interpolación menos), pero numéricamente más estable que la evaluación
    /// directa porque sólo realiza combinaciones convexas (sin potencias ni
    /// coeficientes binomiales grandes).
    /// </summary>
    public sealed class DeCasteljauEvaluator : IBezierEvaluator
    {
        public string Name
        {
            get { return "De Casteljau"; }
        }

        public ControlPoint Evaluate(IReadOnlyList<ControlPoint> controlPoints, double t)
        {
            BezierValidation.ControlPoints(controlPoints);
            BezierValidation.Parameter(t);
            return EvaluateUnchecked(controlPoints, t, new ControlPoint[controlPoints.Count]);
        }

        private static ControlPoint EvaluateUnchecked(
            IReadOnlyList<ControlPoint> controlPoints, double t, ControlPoint[] working)
        {
            if (t == 0.0) return controlPoints[0];
            if (t == 1.0) return controlPoints[controlPoints.Count - 1];

            // Buffer de trabajo: se sobrescribe nivel a nivel.
            int count = controlPoints.Count;
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
            BezierValidation.ControlPoints(controlPoints);
            BezierValidation.SampleCount(sampleCount);

            var samples = new ControlPoint[sampleCount];
            var working = new ControlPoint[controlPoints.Count];
            for (int k = 0; k < sampleCount; k++)
            {
                double t = (double)k / (sampleCount - 1);
                samples[k] = EvaluateUnchecked(controlPoints, t, working);
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
            BezierValidation.ControlPoints(controlPoints);
            BezierValidation.Parameter(t);

            var levels = new List<IReadOnlyList<ControlPoint>>();

            var current = new ControlPoint[controlPoints.Count];
            for (int i = 0; i < current.Length; i++)
                current[i] = controlPoints[i];
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

        /// <summary>Interpolación lineal: (1-t)*a + t*b.</summary>
        private static ControlPoint Lerp(ControlPoint a, ControlPoint b, double t)
        {
            return new ControlPoint(
                (1.0 - t) * a.X + t * b.X,
                (1.0 - t) * a.Y + t * b.Y);
        }

    }
}
