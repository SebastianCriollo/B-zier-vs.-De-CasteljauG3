using System.Collections.Generic;

namespace BezierCore
{
    /// <summary>
    /// Contrato común para los dos métodos de evaluación que exige la práctica:
    /// evaluación directa con polinomios de Bernstein y algoritmo de De Casteljau.
    /// Tener la misma interfaz permite medir tiempos y diferencias numéricas
    /// intercambiando únicamente la implementación.
    /// </summary>
    public interface IBezierEvaluator
    {
        /// <summary>Nombre del método, usado en tablas y gráficos comparativos.</summary>
        string Name { get; }

        /// <summary>
        /// Evalúa la curva definida por controlPoints en el parámetro t entre 0 y 1.
        /// El grado de la curva es controlPoints.Count - 1.
        /// Requiere al menos dos puntos con coordenadas finitas y t finito en [0, 1].
        /// </summary>
        /// <exception cref="System.ArgumentNullException">La lista es nula.</exception>
        /// <exception cref="System.ArgumentException">Puntos insuficientes o coordenadas no finitas.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">t fuera del dominio.</exception>
        ControlPoint Evaluate(IReadOnlyList<ControlPoint> controlPoints, double t);

        /// <summary>
        /// Genera sampleCount puntos de la curva repartidos uniformemente en
        /// [0, 1] (incluyendo t = 0 y t = 1).
        /// Requiere al menos dos muestras y las mismas condiciones de puntos que Evaluate.
        /// </summary>
        IReadOnlyList<ControlPoint> Sample(IReadOnlyList<ControlPoint> controlPoints, int sampleCount);
    }
}
