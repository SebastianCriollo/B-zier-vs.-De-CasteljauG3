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
            ValidateControlPoints(controlPoints);

            int n = controlPoints.Count - 1;
            double[] coefficients = BinomialCoefficients(n);
            return EvaluateWithCoefficients(controlPoints, coefficients, t);
        }

        public IReadOnlyList<ControlPoint> Sample(IReadOnlyList<ControlPoint> controlPoints, int sampleCount)
        {
            ValidateControlPoints(controlPoints);
            if (sampleCount < 2)
                throw new ArgumentOutOfRangeException("sampleCount", "Se requieren al menos 2 muestras.");

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
        /// Coeficientes binomiales C(n,0..n) mediante el triángulo de Pascal
        /// (evita calcular factoriales, que desbordan rápido con grados altos).
        /// </summary>
        public static double[] BinomialCoefficients(int n)
        {
            var row = new double[n + 1];
            row[0] = 1.0;

            for (int i = 1; i <= n; i++)
            {
                // Recurrencia C(n,i) = C(n,i-1) * (n-i+1) / i
                row[i] = row[i - 1] * (n - i + 1) / i;
            }

            return row;
        }

        private static void ValidateControlPoints(IReadOnlyList<ControlPoint> controlPoints)
        {
            if (controlPoints == null)
                throw new ArgumentNullException("controlPoints");
            if (controlPoints.Count < 2)
                throw new ArgumentException("Se requieren al menos 2 puntos de control.", "controlPoints");
        }
    }
}
