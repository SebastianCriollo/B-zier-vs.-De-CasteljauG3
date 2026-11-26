namespace BezierCore;

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
    /// Evalúa la curva definida por <paramref name="controlPoints"/> en el
    /// parámetro t ∈ [0, 1]. El grado de la curva es controlPoints.Count - 1.
    /// </summary>
    ControlPoint Evaluate(IReadOnlyList<ControlPoint> controlPoints, double t);

    /// <summary>
    /// Genera <paramref name="sampleCount"/> puntos de la curva repartidos
    /// uniformemente en [0, 1] (incluyendo t = 0 y t = 1).
    /// </summary>
    IReadOnlyList<ControlPoint> Sample(IReadOnlyList<ControlPoint> controlPoints, int sampleCount);
}
