using System;
using System.Collections.Generic;

namespace BezierCore
{
    /// <summary>
    /// Evaluación DIRECTA de la curva de Bézier mediante polinomios de Bernstein:
    ///
    ///     B(t) = suma para i = 0..n de  C(n,i) * t^i * (1-t)^(n-i) * P_i
    ///
    /// Implementado sin funciones gráficas que generen la curva automáticamente,
    /// según la restricción del enunciado.
    ///
    /// Complejidad: O(n) por cada valor de t evaluado (una pasada sobre los n+1
    /// puntos de control), asumiendo coeficientes binomiales precalculados.
    /// Sin precálculo, el coeficiente binomial añade trabajo adicional por término.
    /// </summary>
    public sealed class BernsteinEvaluator : IBezierEvaluator
    {
        public string Name
        {
            get { return "Bernstein (directo)"; }
        }

        /// <summary>
        /// Si es true, los coeficientes binomiales C(n,i) se calculan una sola vez
        /// por llamada a Sample y se reutilizan para todos los t. Permite comparar
        /// en el informe el impacto del precálculo.
        /// </summary>
        public bool UsePrecomputedCoefficients { get; set; } = true;

        public ControlPoint Evaluate(IReadOnlyList<ControlPoint> controlPoints, double t)
        {
            BezierValidation.ControlPoints(controlPoints);
            BezierValidation.Parameter(t);
            if (t == 0.0) return controlPoints[0];
            if (t == 1.0) return controlPoints[controlPoints.Count - 1];

            int n = controlPoints.Count - 1;
            double[] coefficients = BinomialCoefficients(n);
            return EvaluateWithCoefficients(controlPoints, coefficients, t);
        }

        public IReadOnlyList<ControlPoint> Sample(IReadOnlyList<ControlPoint> controlPoints, int sampleCount)
        {
            BezierValidation.ControlPoints(controlPoints);
            BezierValidation.SampleCount(sampleCount);

            int n = controlPoints.Count - 1;
            double[] coefficients = UsePrecomputedCoefficients ? BinomialCoefficients(n) : null;

            var samples = new ControlPoint[sampleCount];
            for (int k = 0; k < sampleCount; k++)
            {
                double t = (double)k / (sampleCount - 1);
                samples[k] = coefficients != null
                    ? EvaluateWithCoefficients(controlPoints, coefficients, t)
                    : EvaluateWithCoefficients(controlPoints, BinomialCoefficients(n), t);
            }

            return samples;
        }

        /// <summary>
        /// Núcleo del método: suma ponderada de los puntos de control usando las
        /// bases de Bernstein b(i,n)(t) = C(n,i) * t^i * (1-t)^(n-i).
        /// </summary>
        private static ControlPoint EvaluateWithCoefficients(
            IReadOnlyList<ControlPoint> controlPoints,
            double[] coefficients,
            double t)
        {
            if (t == 0.0) return controlPoints[0];
            if (t == 1.0) return controlPoints[controlPoints.Count - 1];

            int n = controlPoints.Count - 1;
            double oneMinusT = 1.0 - t;

            double x = 0.0;
            double y = 0.0;

            for (int i = 0; i <= n; i++)
            {
                double basis = coefficients[i] * Math.Pow(t, i) * Math.Pow(oneMinusT, n - i);
                x += controlPoints[i].X * basis;
                y += controlPoints[i].Y * basis;
            }

            return new ControlPoint(x, y);
        }

        /// <summary>
        /// Coeficientes binomiales C(n,0..n) mediante recurrencia multiplicativa
        /// (evita calcular factoriales, que desbordan rápido con grados altos).
        /// </summary>
        public static double[] BinomialCoefficients(int n)
        {
            if (n < 0 || n == int.MaxValue)
                throw new ArgumentOutOfRangeException("n", "El grado debe ser no negativo y permitir n + 1 coeficientes.");

            var row = new double[n + 1];
            row[0] = 1.0;

            for (int i = 1; i <= n; i++)
            {
                // Recurrencia C(n,i) = C(n,i-1) * (n-i+1) / i
                row[i] = row[i - 1] * ((double)(n - i + 1) / i);
                if (double.IsInfinity(row[i]))
                    throw new ArgumentOutOfRangeException("n", "Los coeficientes exceden la capacidad de double; use De Casteljau.");
            }

            return row;
        }

    }
}
