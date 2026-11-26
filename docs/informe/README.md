# Informe técnico

Entregable escrito de la práctica. Responsable de la consolidación: **SebastianCriollo**
(con aportes de los tres integrantes en sus secciones).

## Portada (formato ESPE)

```
UNIVERSIDAD DE LAS FUERZAS ARMADAS ESPE
DEPARTAMENTO DE CIENCIAS DE LA COMPUTACIÓN
INGENIERÍA EN SOFTWARE

COMPUTACIÓN GRÁFICA

Curvas de Bézier: evaluación directa mediante polinomios de Bernstein
frente al algoritmo de De Casteljau

Brayan Sebastian Criollo Vega
<integrante 2>
<integrante 3>

UNIDAD <n>
NRC <nrc>

Sangolquí, noviembre del 2026
```

## Estructura

| Sección | Contenido | Autor principal |
|---|---|---|
| CONTENIDO | Índice automático | SebastianCriollo |
| 1. INTRODUCCIÓN | Contexto de las curvas de Bézier en computación gráfica y planteamiento de la pregunta de investigación | Mario-Anrrango |
| 2. OBJETIVOS | General y específicos (tomados del enunciado) | SebastianCriollo |
| 3. MARCO TEÓRICO | Fundamentos matemáticos de ambos métodos, ventajas, desventajas y análisis de complejidad | Mario-Anrrango |
| 4. VALIDACIÓN MATEMÁTICA | Cálculo manual de la curva cúbica con ambos métodos (ver `../calculos-manuales/`) | Mario-Anrrango |
| 5. DESARROLLO / IMPLEMENTACIÓN | Arquitectura de la solución, decisiones de diseño y capturas de la aplicación | Damian12T |
| 6. EXPERIMENTACIÓN | Metodología de medición, tablas de tiempos y gráficos comparativos | SebastianCriollo |
| 7. ANÁLISIS DE RESULTADOS | Comparación de tiempos, diferencias numéricas y complejidad computacional | SebastianCriollo |
| 8. CONCLUSIONES | Respuesta a la pregunta de investigación: qué método conviene en cada escenario | Los tres |
| 9. RECOMENDACIONES | — | Damian12T |
| 10. BIBLIOGRAFÍA | APA 7 | Los tres |
| Tabla final | Elaborado por / Revisado por | SebastianCriollo |

## Material que debe quedar aquí

- `informe.docx` (o `.pdf` final) — documento consolidado.
- `resultados/` — CSV generados por `BezierApp.Benchmark` (ignorados por git;
  subir sólo los que se citen en el informe).
- `capturas/` — capturas numeradas como figuras, en el orden en que se citan.
- `graficos/` — gráficos de tiempos y de diferencias numéricas.

## Pregunta a responder

> ¿Qué método de evaluación de curvas de Bézier resulta más eficiente y estable
> numéricamente bajo diferentes condiciones de ejecución?

La respuesta debe sostenerse con los datos medidos, no sólo con la teoría:
tiempos promedio por escenario, desviación estándar, diferencias numéricas
máximas y promedio, y el análisis de complejidad O(n) vs O(n²).
