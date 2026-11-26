# Curvas de Bézier: Bernstein vs. De Casteljau

Práctica de **Computación Gráfica** — Universidad de las Fuerzas Armadas ESPE
Departamento de Ciencias de la Computación · Ingeniería en Software · Grupo 3

## Objetivo

Analizar, implementar y comparar la evaluación directa de curvas de Bézier
mediante polinomios de Bernstein y el algoritmo de De Casteljau, considerando
su **rendimiento**, **estabilidad numérica** y **complejidad computacional**.

> **Pregunta de investigación:** ¿Qué método de evaluación de curvas de Bézier
> resulta más eficiente y estable numéricamente bajo diferentes condiciones de
> ejecución?

## Integrantes y áreas

| Integrante | Área | Carpeta | Rama |
|---|---|---|---|
| [@Mario-Anrrango](https://github.com/Mario-Anrrango) | Núcleo matemático y marco teórico | `src/BezierCore/` | `feature/core-algoritmos` |
| [@Damian12T](https://github.com/Damian12T) | Aplicación gráfica (WinForms + GDI+) | `src/BezierApp.UI/` | `feature/ui-winforms` |
| [@SebastianCriollo](https://github.com/SebastianCriollo) | Experimentación, comparación e informe | `src/BezierApp.Benchmark/` | `feature/benchmark-comparacion` |

El detalle de tareas por persona está en **[TASKS.md](TASKS.md)**.
El flujo de trabajo con Git está en **[CONTRIBUTING.md](CONTRIBUTING.md)**.

## Estructura del repositorio

```
BezierVsDeCasteljau.sln
├── src/
│   ├── BezierCore/              Algoritmos puros, sin dependencias gráficas
│   │   ├── ControlPoint.cs          Punto 2D en double (precisión para el análisis)
│   │   ├── IBezierEvaluator.cs      Contrato común a ambos métodos
│   │   ├── BernsteinEvaluator.cs    Evaluación directa — O(n) por cada t
│   │   └── DeCasteljauEvaluator.cs  Interpolación por niveles — O(n²) por cada t
│   ├── BezierApp.UI/            Aplicación Windows Forms
│   │   ├── CurveCanvas.cs           Lienzo GDI+ y manejo de puntos con el mouse
│   │   └── MainForm.cs              Panel de control (grado, muestras, capas)
│   └── BezierApp.Benchmark/     Mediciones con Stopwatch, exporta CSV
│       ├── BenchmarkRunner.cs       Matriz de escenarios, 10 repeticiones mínimo
│       └── ComparisonResult.cs      Tiempos y diferencias numéricas por escenario
└── docs/
    ├── informe/                 Informe técnico y material de respaldo
    └── calculos-manuales/       Validación matemática a mano
```

`BezierCore` es compartido: la UI y el benchmark lo consumen, así que ambos
miden y dibujan exactamente los mismos algoritmos.

## Cómo ejecutar

Requiere **.NET 8 SDK**. La aplicación gráfica necesita Windows.

```bash
# Aplicación gráfica
dotnet run --project src/BezierApp.UI

# Mediciones de rendimiento (exporta un CSV con los resultados)
dotnet run --project src/BezierApp.Benchmark --configuration Release
```

Para las mediciones usar siempre `Release`: en `Debug` los tiempos no son
representativos.

## Requerimientos de la práctica

- [x] Estructura del proyecto y separación de responsabilidades
- [ ] Agregar, eliminar y modificar puntos de control
- [ ] Generar curvas de Bézier de diferentes grados
- [ ] Visualizar simultáneamente los resultados de ambos algoritmos
- [ ] Configurar la cantidad de muestras
- [ ] Visualizar las interpolaciones de De Casteljau
- [ ] Medir los tiempos de ejecución mediante `Stopwatch`
- [ ] Calcular las diferencias numéricas entre los puntos generados
- [ ] Cada prueba ejecutada al menos 10 veces, excluyendo el tiempo de dibujo
- [ ] Informe técnico con cálculos manuales, tablas, gráficos y conclusiones

**Restricción:** los algoritmos se implementan matemáticamente. Está prohibido
usar funciones gráficas que generen las curvas directamente
(`Graphics.DrawBezier`, `Graphics.DrawCurve`).

## Entregables

1. Código fuente (este repositorio)
2. Aplicación funcional
3. Informe técnico → `docs/informe/`
