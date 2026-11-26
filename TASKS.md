# Reparto de trabajo — Grupo 3

Cada integrante trabaja **principalmente en su propia carpeta**, para que los
commits no choquen entre sí y el aporte de cada uno quede claro en el historial
de GitHub.

> El reparto es una propuesta: si prefieren intercambiar áreas, cámbienlo aquí
> en el primer commit y manténganlo actualizado.

---

## @Mario-Anrrango — Núcleo matemático

**Carpeta:** `src/BezierCore/` · **Rama:** `feature/core-algoritmos`

Dueño de los algoritmos y de la parte teórica del informe.

- [ ] Revisar y validar `BernsteinEvaluator` (fórmula, coeficientes binomiales, potencias).
- [ ] Revisar y validar `DeCasteljauEvaluator` (niveles de interpolación).
- [ ] Verificar que ambos métodos soportan **cualquier grado**, no sólo cúbicas.
- [ ] Agregar validaciones y mensajes de error claros para entradas inválidas.
- [ ] Cálculos manuales de la curva cúbica con ambos métodos → `docs/calculos-manuales/`.
- [ ] Comparar los valores manuales con la salida del programa y documentar la coincidencia.
- [ ] Redactar: **Introducción**, **Marco teórico** (fundamentos, ventajas, desventajas) y **Análisis de complejidad** O(n) vs O(n²).

---

## @Damian12T — Aplicación gráfica

**Carpeta:** `src/BezierApp.UI/` · **Rama:** `feature/ui-winforms`

Dueño de la interfaz y de la visualización. **Restricción:** prohibido usar
`Graphics.DrawBezier` o `DrawCurve` para generar la curva; sólo `DrawLine`
entre puntos ya calculados por `BezierCore`.

- [ ] Dibujar la curva de **Bernstein** en el lienzo (TODO marcado en `CurveCanvas.OnPaint`).
- [ ] Dibujar la curva de **De Casteljau** con estilo distinguible (ambas simultáneas).
- [ ] Dibujar los **niveles intermedios de interpolación** de De Casteljau según el valor de t.
- [ ] Verificar que agregar / eliminar / modificar puntos funcione desde el mouse y desde la lista.
- [ ] Verificar que el selector de muestras afecte el trazado en tiempo real.
- [ ] Reemplazar el diálogo mínimo de coordenadas por un formulario propio con validación.
- [ ] Mostrar el grado de la curva y una leyenda de colores en la ventana.
- [ ] Capturas de pantalla para el informe → `docs/informe/capturas/`.
- [ ] Redactar: **Desarrollo / Implementación** y **Recomendaciones**.

---

## @SebastianCriollo — Experimentación y comparación

**Carpeta:** `src/BezierApp.Benchmark/` · **Rama:** `feature/benchmark-comparacion`

Dueño de las mediciones y de la consolidación del informe.

- [x] Ejecutar la matriz de escenarios variando puntos de control y muestras.
- [x] Confirmar que cada escenario corre **mínimo 10 veces** y sin tiempo de dibujo incluido.
- [x] Agregar escenario de **estabilidad numérica** con grados altos (30–50 puntos) y t en los extremos.
- [x] Generar tablas comparativas de tiempos (media, desviación, mín., máx.).
- [x] Generar gráfico de tiempo vs. grado (el de tiempo vs. muestras queda pendiente).
- [x] Generar gráfico de diferencias numéricas vs. grado.
- [x] Redactar: **Objetivos**, **Experimentación**, **Análisis de resultados**.
- [ ] Consolidar el informe final y la tabla "Elaborado por / Revisado por".

---

## Trabajo conjunto

- [ ] **Conclusiones** — responder la pregunta: ¿qué método es más eficiente y
      estable numéricamente según el escenario? Cada uno aporta desde su área.
- [ ] **Bibliografía** en APA 7.
- [ ] Revisión cruzada: cada integrante revisa el Pull Request de otro antes del merge.

---

## Orden sugerido

1. `BezierCore` queda validado (Mario) — los otros dos dependen de él.
2. En paralelo: UI (Damian) y benchmark (Sebastián) sobre el núcleo ya estable.
3. Experimentos y capturas una vez que ambas partes funcionan.
4. Informe consolidado al final, con los datos reales ya medidos.
