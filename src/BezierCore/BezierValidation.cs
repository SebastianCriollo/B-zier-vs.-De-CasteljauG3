using System;
using System.Collections.Generic;

namespace BezierCore
{
    internal static class BezierValidation
    {
        internal static void ControlPoints(IReadOnlyList<ControlPoint> points)
        {
            if (points == null)
                throw new ArgumentNullException("controlPoints", "La lista de puntos no puede ser nula.");
            if (points.Count < 2)
                throw new ArgumentException("Se requieren al menos 2 puntos de control.", "controlPoints");

            for (int i = 0; i < points.Count; i++)
            {
                if (!IsFinite(points[i].X) || !IsFinite(points[i].Y))
                    throw new ArgumentException("Las coordenadas de P" + i + " deben ser finitas.", "controlPoints");
            }
        }

        internal static void Parameter(double t)
        {
            if (!IsFinite(t) || t < 0.0 || t > 1.0)
                throw new ArgumentOutOfRangeException("t", "El parámetro t debe ser finito y estar entre 0 y 1.");
        }

        internal static void SampleCount(int sampleCount)
        {
            if (sampleCount < 2)
                throw new ArgumentOutOfRangeException("sampleCount", "Se requieren al menos 2 muestras.");
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
