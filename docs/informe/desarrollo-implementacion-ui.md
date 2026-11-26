# Desarrollo e implementación de la aplicación gráfica

Aporte individual al informe técnico — secciones **5. Desarrollo / Implementación**,
**9. Recomendaciones** y aporte a **8. Conclusiones**.

| Campo | Valor |
|---|---|
| Integrante | Edgar Damián Toscano Coro ([@Damian12T](https://github.com/Damian12T)) |
| Área | Aplicación gráfica (Windows Forms + GDI+) |
| Carpeta | `src/BezierApp.UI/` |
| Rama | `feature/ui-winforms` |
| Tecnología | C# 7.3, Windows Forms, GDI+ (`System.Drawing`), .NET Framework 4.8 |

> Este documento describe únicamente la capa gráfica. Los fundamentos matemáticos,
> el análisis de complejidad y la validación manual corresponden a Mario-Anrrango;
> las mediciones de rendimiento y de diferencias numéricas corresponden a
> SebastianCriollo. Aquí no se presentan resultados experimentales de rendimiento.

---

## 5. Desarrollo / Implementación

### 5.1 Arquitectura de la aplicación gráfica

La solución separa los algoritmos de la presentación. `BezierApp.UI` **consume**
el núcleo matemático `BezierCore`; no reimplementa ninguno de los dos métodos.

```
┌───────────────────────────── BezierApp.UI ─────────────────────────────┐
│                                                                        │
│  MainForm ──────────────── coordina ─────────────┐                     │
│   │  (panel de control, lista, selectores,       │                     │
│   │   información de la curva, barra de estado)  │                     │
│   │                                              ▼                     │
│   ├── CurveCanvas  (lienzo GDI+, mouse) ── usa ── CurveStyles          │
│   ├── LegendPanel  (leyenda de colores) ─ usa ── CurveStyles           │
│   ├── PointEditorForm (formulario modal) ─ usa ─ CoordinateValidator   │
│   └── CurveInfo (texto del grado)            UiTheme (colores UI)      │
│                                                                        │
└──────────────────────────────┬─────────────────────────────────────────┘
                               │  Sample(...)  EvaluateWithSteps(...)
                               ▼
┌────────────────────────────── BezierCore ──────────────────────────────┐
│  ControlPoint · IBezierEvaluator · BernsteinEvaluator ·                │
│  DeCasteljauEvaluator                                                  │
└────────────────────────────────────────────────────────────────────────┘
```

La dependencia es en un solo sentido: la interfaz conoce al núcleo, pero el
núcleo no conoce a la interfaz. Por eso el benchmark de SebastianCriollo puede
medir exactamente los mismos algoritmos que se dibujan en pantalla.

### 5.2 Programación orientada a objetos y responsabilidades

Cada clase tiene una responsabilidad concreta:

| Clase | Tipo | Responsabilidad |
|---|---|---|
| `MainForm` | `Form` | Construye la ventana, conecta los eventos de los controles y mantiene sincronizados lienzo, lista, información y leyenda. No dibuja ni calcula curvas. |
| `CurveCanvas` | `Panel` | Es la **única fuente de verdad** de los puntos de control. Gestiona el mouse (agregar, seleccionar, arrastrar, eliminar, doble clic) y dibuja todas las capas en `OnPaint`. |
| `CurveStyles` | clase estática | Define colores, grosores y estilos de trazo de cada capa. El lienzo y la leyenda crean sus plumas desde aquí, por lo que la leyenda siempre coincide con lo dibujado. |
| `LegendPanel` | `Control` | Dibuja la leyenda de colores en columnas y atenúa las capas ocultas. |
| `PointEditorForm` | `Form` | Formulario modal para agregar o modificar un punto; el mismo formulario sirve para ambos casos. |
| `CoordinateValidator` | clase estática | Convierte y valida el texto de una coordenada. No depende de controles gráficos. |
| `CurveInfo` | clase estática | Calcula el grado `n = puntos − 1` y su descripción (lineal, cuadrática, cúbica…). |
| `ControlPointEventArgs` | `EventArgs` | Transporta el índice del punto en el evento `EditRequested`. |
| `UiTheme` | clase estática | Colores generales de paneles, lienzo y textos. |

Principios aplicados:

- **Encapsulamiento.** `CurveCanvas.ControlPoints` se expone como
  `IReadOnlyList<ControlPoint>`. Ninguna clase externa puede modificar la lista
  directamente; toda modificación pasa por `AddControlPoint`,
  `UpdateControlPoint`, `RemoveControlPointAt`, `SetControlPoints` y
  `ClearControlPoints`, que siempre disparan `ControlPointsChanged` y descartan
  las muestras en caché. En la versión inicial la lista era una `List` pública y
  la curva de ejemplo se cargaba sin disparar eventos.
- **Separación de responsabilidades.** La validación (`CoordinateValidator`) está
  separada del formulario (`PointEditorForm`); los estilos (`CurveStyles`) están
  separados del dibujo; el texto del grado (`CurveInfo`) está separado de la
  ventana.
- **Reutilización.** `PointEditorForm.TryGetPoint(...)` atiende tanto "Agregar"
  como "Modificar". `DrawPolyline` dibuja tanto la curva de Bernstein como la de
  De Casteljau. Las mismas plumas de `CurveStyles` sirven al lienzo y a la leyenda.
- **Comunicación por eventos.** El lienzo publica `ControlPointsChanged`,
  `SelectionChanged` y `EditRequested`; `MainForm` se suscribe y actualiza el resto
  de la interfaz. El lienzo no conoce a la lista ni a la leyenda.

### 5.3 Integración de BezierApp.UI con BezierCore

La interfaz utiliza tres operaciones del núcleo:

| Operación de `BezierCore` | Uso en la interfaz |
|---|---|
| `BernsteinEvaluator.Sample(puntos, muestras)` | Puntos de la curva de Bernstein. |
| `DeCasteljauEvaluator.Sample(puntos, muestras)` | Puntos de la curva de De Casteljau. |
| `DeCasteljauEvaluator.EvaluateWithSteps(puntos, t)` | Todos los niveles intermedios para el t seleccionado. |

El núcleo exige al menos 2 puntos de control y al menos 2 muestras. La interfaz
garantiza esas precondiciones **antes** de llamar al núcleo:

- con 0 o 1 punto, `OnPaint` no llama a `BezierCore` y muestra un mensaje;
- `CurveCanvas.SampleCount` limita el valor al intervalo [2, 10 000];
- `CurveCanvas.InterpolationT` limita t al intervalo [0, 1] y convierte `NaN` en 0.

Las muestras calculadas se guardan en caché (`_bernsteinSamples`,
`_deCasteljauSamples`) y sólo se recalculan cuando cambian los puntos o la
cantidad de muestras (`InvalidateCurves`). Así, mover el deslizador de t o
activar una casilla no vuelve a evaluar las curvas.

### 5.4 Funcionamiento de `CurveCanvas.OnPaint`

`OnPaint` dibuja las capas en un orden fijo; cada capa queda encima de la anterior:

1. Cuadrícula tenue de referencia (cada 50 px).
2. Si hay menos de 2 puntos: mensaje explicativo, los puntos existentes y fin.
3. Polígono de control (línea discontinua gris), si está activo.
4. Curva de Bernstein (banda azul ancha), si está activa.
5. Curva de De Casteljau (línea discontinua naranja), encima de Bernstein, si está activa.
6. Marcadores de las muestras calculadas, cuando hay 50 muestras o menos.
7. Niveles intermedios de De Casteljau y el punto B(t), si están activos.
8. Puntos de control con su etiqueta `Pi` (el seleccionado, en amarillo y más grande).

El lienzo usa `DoubleBuffered = true` y `ResizeRedraw = true`, por lo que no
parpadea al arrastrar puntos ni al redimensionar la ventana.

### 5.5 Representación gráfica mediante segmentos de línea

El enunciado prohíbe `Graphics.DrawBezier` y `Graphics.DrawCurve`. La curva se
representa como una poligonal que une los puntos ya calculados por `BezierCore`:

```csharp
private static void DrawPolyline(Graphics g, IReadOnlyList<ControlPoint> samples, Pen pen)
{
    bool dashed = pen.DashStyle != DashStyle.Solid;
    double travelled = 0.0;

    for (int i = 0; i < samples.Count - 1; i++)
    {
        if (dashed)
            pen.DashOffset = (float)(travelled / pen.Width);

        g.DrawLine(pen,
            (float)samples[i].X, (float)samples[i].Y,
            (float)samples[i + 1].X, (float)samples[i + 1].Y);

        travelled += ControlPoint.Distance(samples[i], samples[i + 1]);
    }
}
```

Con `m` muestras se dibujan `m − 1` segmentos. Cada llamada a `DrawLine` reinicia
el patrón de guiones de la pluma; si no se corrigiera, con muchas muestras (segmentos
de 1 o 2 píxeles) la línea discontinua se vería continua. Por eso se ajusta
`DashOffset` con la longitud ya recorrida: el patrón continúa de un segmento al
siguiente sin modificar ninguna coordenada matemática.

Las coordenadas del núcleo están en `double`; sólo en el momento de dibujar se
convierten a `float`, que es el tipo que acepta GDI+.

### 5.6 Visualización de Bernstein

- Datos: `BernsteinEvaluator.Sample(ControlPoints, SampleCount)`.
- Estilo: banda continua de 6 px, color azul claro opaco `RGB(96, 150, 245)`,
  extremos y uniones redondeados.
- Se usa un color **opaco** porque, al dibujar segmento por segmento, un color
  semitransparente se superpone en cada unión y genera puntos más oscuros a lo
  largo de la curva.
- Con 50 muestras o menos se marcan los puntos calculados (azul oscuro con borde
  blanco), para mostrar qué une cada `DrawLine`.

### 5.7 Visualización de De Casteljau

- Datos: `DeCasteljauEvaluator.Sample(ControlPoints, SampleCount)`.
- Estilo: línea discontinua naranja `RGB(234, 88, 12)` de 2,2 px, con patrón 4 : 2,5.
- Se dibuja **después** y **encima** de la banda de Bernstein. Como es más delgada
  y discontinua, cuando ambas curvas coinciden se ve la banda azul con los guiones
  naranjas en su centro: ninguna oculta por completo a la otra (figura 8).
- Cada curva se activa o desactiva por separado con su casilla (figuras 6 y 7).

### 5.8 Niveles intermedios de interpolación

La construcción geométrica se obtiene de `EvaluateWithSteps`, que devuelve todos
los niveles para el t actual:

| Nivel | Contenido | Representación |
|---|---|---|
| 0 | Los n + 1 puntos de control | Polígono de control y manejadores |
| 1 … n − 1 | Puntos interpolados: `P(i,r) = (1 − t)·P(i,r−1) + t·P(i+1,r−1)` | Polígono y puntos de un color distinto por nivel |
| n | Un único punto, B(t) | Punto rojo con borde blanco y etiqueta `B(t) = (x, y)` |

Colores por nivel: verde, morado, cian, ámbar, gris pizarra y rosa; si el grado
exige más niveles, la paleta se repite y la leyenda lo indica. El punto B(t)
siempre es rojo para no confundirse con ningún nivel.

El parámetro t se controla con un deslizador (pasos de 0,001) y con un campo
numérico sincronizado para ingresar valores exactos (0, 0,25, 0,5, 0,75, 1). Ambos
pasan por un único método, `SetInterpolationT`, que actualiza el lienzo y el otro
control sin provocar eventos recursivos. La visualización se actualiza en tiempo
real y se activa con la casilla «Niveles de interpolación (De Casteljau)».

Para la curva cúbica de ejemplo (la misma de los cálculos manuales) la etiqueta
de la aplicación muestra B(0,5) = (390, 225), que coincide con
`(P0 + 3P1 + 3P2 + P3) / 8`.

### 5.9 Gestión de eventos del mouse

| Acción sobre el lienzo | Resultado | Método |
|---|---|---|
| Clic izquierdo en zona vacía | Agrega un punto al final y lo selecciona | `OnMouseDown` |
| Clic izquierdo sobre un punto | Lo selecciona (también en la lista) | `OnMouseDown` |
| Arrastrar un punto | Lo mueve; la curva se recalcula en cada movimiento | `OnMouseMove` |
| Soltar el botón | Termina el arrastre | `OnMouseUp` |
| Doble clic sobre un punto | Abre el formulario de coordenadas de ese punto | `OnMouseDoubleClick` → `EditRequested` |
| Clic derecho sobre un punto | Lo elimina | `OnMouseDown` |
| Mover el cursor | Cambia el cursor (mano / cruz) y actualiza la barra de estado | `OnMouseMove` |

Detalles de implementación:

- `HitTest` recorre los puntos **del último al primero**, porque los últimos se
  dibujan encima. Con puntos repetidos se selecciona el que el usuario ve.
- Durante el arrastre la posición se limita al área visible del lienzo, para que
  un punto no se pierda fuera de la ventana.
- El segundo clic de un doble clic (`e.Clicks == 2`) no agrega un punto adicional.

### 5.10 Gestión de puntos de control

- **Desde la lista:** botones *Agregar…*, *Modificar…* y *Eliminar*; doble clic o
  Enter para editar; tecla Supr para eliminar; *Limpiar todo* y *Curva de ejemplo*.
- **Sincronización:** la lista se reconstruye a partir del lienzo cada vez que se
  dispara `ControlPointsChanged`, conservando la selección y la posición de
  desplazamiento. La selección se sincroniza en ambos sentidos mediante
  `SelectionChanged` y una bandera `_syncingSelection` que evita ciclos de eventos.
- **Eliminación consistente:** `RemoveControlPointAt` conserva el orden de los
  demás puntos y ajusta la selección: si se elimina el punto seleccionado, se
  selecciona el que pasa a ocupar su posición (o el último), lo que permite
  eliminar varios seguidos; si se elimina otro anterior, el índice seleccionado
  se desplaza.
- **Sin puntos:** *Modificar* y *Eliminar* muestran un aviso en lugar de fallar.

### 5.11 Validación de coordenadas

El diálogo mínimo anterior (dos `NumericUpDown` creados dentro de `MainForm`) se
reemplazó por `PointEditorForm`, con título descriptivo, campos X e Y, botones
*Aceptar* y *Cancelar*, íconos de error (`ErrorProvider`) y un área de mensajes.

`CoordinateValidator.TryParse` aplica estas reglas:

| Regla | Ejemplo aceptado | Ejemplo rechazado y mensaje |
|---|---|---|
| Campo obligatorio | `120` | vacío o espacios → «La coordenada X es obligatoria.» |
| Valor numérico | `-35.5`, `1e3` | `abc`, `12a` → «… debe ser un número (por ejemplo 120, -35.5 o 12,75).» |
| Negativos y decimales | `-35.5`, `12,75` (coma o punto) | — |
| Valor finito | — | `NaN`, `Infinity` → «… debe ser un número finito.» |
| Límites [−10 000, 10 000] | `-10000` | `50001` → «… debe estar entre -10000 y 10000.» |

Se valida al pulsar *Aceptar*; se muestran los errores de ambos campos a la vez y
el foco pasa al primer campo incorrecto. El formulario sólo se cierra con datos
válidos. Al editar, los campos cargan las coordenadas actuales del punto;
*Cancelar* (o Esc) cierra sin modificar nada.

El sistema de coordenadas es el del lienzo: origen en la esquina superior
izquierda y el eje Y creciendo hacia abajo. Las coordenadas negativas son válidas
aunque el punto quede fuera de la zona visible.

### 5.12 Selector de muestras

- `NumericUpDown` con límites tomados de `CurveCanvas.MinSampleCount` (2) y
  `CurveCanvas.MaxSampleCount` (10 000); los valores escritos fuera de rango se
  ajustan al límite más cercano.
- El lienzo vuelve a limitar el valor como segunda barrera; con menos de 2
  muestras no existe ningún segmento y el núcleo dividiría entre `muestras − 1 = 0`.
- Junto al campo se muestra la cantidad de segmentos de recta (`muestras − 1`).
- Botones rápidos 4, 10, 50, 100 y 1000 para la exposición.
- El cambio se refleja de inmediato: con pocas muestras se ve la aproximación
  poligonal (figuras 13a y 13b); con muchas, la curva se ve suave (figura 13d).

### 5.13 Actualización dinámica de las curvas

```
Mouse / lista / formulario
        │
        ▼
CurveCanvas  (modifica los puntos)
        │  InvalidateCurves(): descarta la caché y llama a Invalidate()
        │  ControlPointsChanged
        ▼
MainForm ──► RefreshPointsList()  (lista)
        └──► UpdateCurveInfo()    (puntos, grado, muestras, t y leyenda)

Repintado: OnPaint → BezierCore.Sample(...) sólo si la caché está vacía → DrawLine
```

Cambiar t, las casillas o la ventana sólo repinta; cambiar puntos o muestras
recalcula. Como observación de fluidez de la interfaz (no es una medición del
benchmark): en el equipo de prueba, un repintado completo con 10 000 muestras por
curva tardó unos 60 ms y la ventana siguió respondiendo.

### 5.14 Grado de la curva

`CurveInfo.GetDegree` aplica `n = cantidad de puntos − 1` sólo cuando existen al
menos 2 puntos. Con 0 o 1 punto no hay curva y se muestra un estado explícito en
lugar de un grado 0 o negativo:

| Puntos | Texto mostrado |
|---|---|
| 0 | Sin curva (no hay puntos) |
| 1 | Sin curva (faltan puntos) |
| 2 | 1 (lineal) |
| 3 | 2 (cuadrática) |
| 4 | 3 (cúbica) |
| 5 | 4 (cuártica) |
| 6 | 5 (quíntica) |
| n + 1 ≥ 7 | n |

La sección «Información de la curva» muestra además la cantidad de puntos, la
cantidad de muestras y el valor actual de t, y se actualiza con cada cambio.

### 5.15 Leyenda de colores

`LegendPanel` se ubica en una franja bajo el lienzo e identifica: polígono de
control, punto de control y punto seleccionado, curva de Bernstein, curva de
De Casteljau, muestras calculadas, cada nivel de interpolación existente para el
grado actual (hasta 6 filas, luego se indica la repetición de la paleta) y el
punto B(t). Las capas desactivadas se muestran atenuadas con la marca «(oculto)».
Cada muestra se dibuja con las mismas plumas de `CurveStyles` que usa el lienzo.

### 5.16 Defectos de la versión inicial corregidos

Durante la implementación se detectaron y corrigieron estos defectos de la
estructura inicial de la interfaz:

| Defecto | Efecto | Corrección |
|---|---|---|
| El lienzo (`Dock = Fill`) se agregaba después del panel lateral | WinForms acopla en orden z inverso: el lienzo ocupaba también el área bajo el panel y se podían arrastrar puntos a una zona oculta | `BringToFront()` del lienzo para que se acople al final |
| `OnPaint` terminaba antes de dibujar los puntos cuando había menos de 2 | Con un solo punto, el punto era invisible | Se dibujan los puntos y un mensaje aunque no exista curva |
| `HitTest` devolvía el primer punto que coincidía | Con puntos repetidos se seleccionaba el punto de abajo | Recorrido del último al primero |
| `ControlPoints` era una `List` pública modificable | Se podían cambiar puntos sin disparar eventos ni actualizar la lista | `IReadOnlyList` + métodos públicos que notifican |

### 5.17 Pruebas realizadas

**Entorno:** Windows 11 Pro, .NET SDK 10.0.401 con destino `net48`, pantalla al
125 %. La solución completa (`BezierCore`, `BezierApp.UI`, `BezierApp.Benchmark`)
compila en Debug y Release con 0 errores y 0 advertencias.

**Método:** los casos 1 a 29 se ejecutaron con un programa de prueba local (no
incluido en el repositorio) que abre la `MainForm` real, envía mensajes reales de
Windows al lienzo (`WM_LBUTTONDOWN`, `WM_MOUSEMOVE`, `WM_RBUTTONDOWN`, …), pulsa
los botones reales, completa el formulario de coordenadas en su ciclo modal y
comprueba el estado resultante. El caso 30 se verificó con una búsqueda en el
código fuente. Las capturas de la sección 5.18 se tomaron durante esa ejecución.

| # | Caso evaluado | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|
| 1 | Aplicación sin puntos de control | Sin excepción; grado «Sin curva»; aviso al eliminar | 0 puntos; «Sin curva (no hay puntos)»; aviso mostrado | Aprobada |
| 2 | Un punto de control | Punto visible, sin curva, sin excepción | 1 punto visible; «Sin curva (faltan puntos)» | Aprobada |
| 3 | Dos puntos de control | Segmento recto, grado 1 | «1 (lineal)» | Aprobada |
| 4 | Curva cuadrática | Grado 2 | «2 (cuadrática)» | Aprobada |
| 5 | Curva cúbica | Grado 3 con 4 puntos | «3 (cúbica)», 4 puntos | Aprobada |
| 6 | Curva de grado superior | Grado 7 con 8 puntos | «7», 8 puntos | Aprobada |
| 7 | Puntos de control repetidos | Se dibuja sin error; el clic selecciona el punto superior | P1 = P2 = (450, 150); se seleccionó P2 | Aprobada |
| 8 | Agregar puntos con el mouse | P4 en (800, 300), seleccionado y listado | P4 = (800, 300) en lienzo y lista; grado 4 | Aprobada |
| 9 | Eliminar puntos con el mouse | P1 eliminado, orden conservado, lista renumerada | 4 puntos; nueva P1 = (520, 140) | Aprobada |
| 10 | Arrastrar puntos | P1 pasa a (330, 70); lista y curva actualizadas | P1 = (330, 70) en lienzo y lista | Aprobada |
| 11 | Editar puntos desde la lista | El formulario carga (520, 140); P2 pasa a (540, 180) | Valores iniciales (520, 140); P2 = (540, 180) | Aprobada |
| 12 | Coordenadas negativas | Se acepta X = −35,5 | Punto agregado (−35,5; 12,75) | Aprobada |
| 13 | Coordenadas decimales | Se acepta `12,75` (coma) y `-35.5` (punto) | Punto agregado (−35,5; 12,75) | Aprobada |
| 14 | Coordenadas inválidas | `abc`, `12a`, `50001` y `NaN` rechazados con mensaje; el formulario sigue abierto | Mensajes mostrados; formulario abierto; ningún punto agregado | Aprobada |
| 15 | Campos vacíos | «es obligatoria» para X e Y; no se agrega punto | «La coordenada X es obligatoria. / La coordenada Y es obligatoria.» | Aprobada |
| 16 | Cancelar una edición | P0 conserva (120, 480) | P0 = (120, 480) tras escribir 999 y cancelar | Aprobada |
| 17 | Cantidad mínima de muestras | Mínimo 2 (1 segmento) | Mínimo 2; lienzo con 2 muestras | Aprobada |
| 18 | Cantidad máxima de muestras | Máximo 10 000; valores fuera de rango limitados; sin bloqueo | Máximo 10 000; «1» → 2; «999999» → 10 000; repintado ≈ 60 ms | Aprobada |
| 19 | Modificación dinámica de muestras | Lienzo e información cambian al instante | 10 → 1 000 reflejado en lienzo e información | Aprobada |
| 20 | t = 0 | B(0) = (120, 480); controles sincronizados | B = (120, 480); deslizador 0; información 0.000 | Aprobada |
| 21 | t = 0,25 | B(0,25) = (243,75; 288,75) | B = (243,75; 288,75); deslizador 250 | Aprobada |
| 22 | t = 0,5 | B(0,5) = (390, 225) | B = (390, 225); deslizador 500 | Aprobada |
| 23 | t = 0,75 | B(0,75) = (536,25; 288,75) | B = (536,25; 288,75); deslizador 750 | Aprobada |
| 24 | t = 1 | B(1) = (660, 480) | B = (660, 480); deslizador 1000 | Aprobada |
| 25 | Visualización simultánea | Ambas curvas visibles con estilos distintos | Banda azul + discontinua naranja superpuestas (figura 8) | Aprobada |
| 26 | Activación y desactivación | Cada casilla oculta/muestra su capa; la leyenda marca «(oculto)» | Sólo Bernstein, sólo De Casteljau y sin polígono verificados | Aprobada |
| 27 | Niveles intermedios | Grado 5: niveles de 6, 5, 4, 3, 2 y 1 puntos | 6, 5, 4, 3, 2, 1 (figura 9b) | Aprobada |
| 28 | Actualización del grado | 3 → 4 → 1 → sin curva | 3 (cúbica) → 4 (cuártica) → 1 (lineal) → Sin curva | Aprobada |
| 29 | Redimensionamiento | Lienzo, leyenda y panel sin superponerse; el lienzo crece | 1100×720: lienzo 764×515; 1480×820: 1144×615; 1280×800: 944×595 | Aprobada |
| 30 | Ausencia de llamadas prohibidas | Ninguna llamada a `DrawBezier`/`DrawCurve` | Búsqueda en `src/BezierApp.UI/`: sólo aparecen en comentarios que explican la restricción; las curvas usan `g.DrawLine` | Aprobada |

Los valores esperados de los casos 20 a 24 son los de la curva cúbica de ejemplo
P0 = (120, 480), P1 = (260, 140), P2 = (520, 140), P3 = (660, 480). Su desarrollo
manual completo corresponde a `docs/calculos-manuales/` (Mario-Anrrango).

### 5.18 Capturas de pantalla

Todas las capturas son de la aplicación real en ejecución y están en
[`capturas/`](capturas/).

| Figura | Archivo | Contenido |
|---|---|---|
| 1 | `01-interfaz-principal.png` | Ventana principal al iniciar (curva cúbica de ejemplo) |
| 2 | `02-curva-lineal.png` | Curva lineal (2 puntos) |
| 3 | `03-curva-cuadratica.png` | Curva cuadrática (3 puntos) |
| 4 | `04-curva-cubica.png` | Curva cúbica (4 puntos) |
| 5 | `05-curva-grado-superior.png` | Curva de grado 7 (8 puntos) |
| 6 | `06-visualizacion-bernstein.png` | Sólo la curva de Bernstein |
| 7 | `07-visualizacion-de-casteljau.png` | Sólo la curva de De Casteljau |
| 8 | `08-comparacion-simultanea.png` | Ambos métodos superpuestos |
| 9a | `09a-niveles-t-025.png` | Niveles intermedios de la cúbica con t = 0,25 |
| 9b | `09b-niveles-grado-5.png` | Niveles intermedios de una curva de grado 5 con t = 0,4 |
| 10 | `10-formulario-coordenadas.png` | Formulario de coordenadas al editar P2 |
| 11 | `11-validacion-coordenadas.png` | Validación de entradas no numéricas |
| 11b | `11b-validacion-campos-vacios.png` | Validación de campos vacíos |
| 12 | `12-modificacion-puntos.png` | P1 arrastrado y seleccionado; lista actualizada |
| 13a–13d | `13a-muestras-2.png` … `13d-muestras-1000.png` | 2, 4, 10 y 1000 muestras |
| 14 | `14-grado-y-leyenda.png` | Información de la curva y leyenda completa |
| 15 | `15-redimensionamiento.png` | Ventana redimensionada |

---

## 9. Recomendaciones

1. **Mantener la regla de dibujo.** Cualquier ampliación debe seguir dibujando con
   `DrawLine` entre puntos calculados por `BezierCore`. Antes de cada entrega,
   buscar `DrawBezier` y `DrawCurve` en `src/BezierApp.UI/`.
2. **No calcular en la interfaz.** Si se necesita otra visualización (por ejemplo,
   la subdivisión de la curva en t), el cálculo debe agregarse a `BezierCore` y la
   interfaz sólo debe consumirlo.
3. **Escoger la cantidad de muestras según el propósito.** Para explicar la
   aproximación poligonal conviene usar entre 4 y 10 muestras; para observar la
   forma de la curva bastan 100. Valores cercanos a 10 000 sólo tienen sentido
   para comprobar el límite de la interfaz.
4. **Validación de t en el núcleo.** La interfaz limita t a [0, 1] y descarta `NaN`
   antes de llamar a `EvaluateWithSteps`, pero el núcleo hoy no valida t. Se
   sugiere a Mario-Anrrango evaluar esa validación dentro de su tarea «Agregar
   validaciones y mensajes de error claros para entradas inválidas». No bloquea a
   la interfaz.
5. **Escalado de pantalla (DPI).** La aplicación usa posiciones fijas en píxeles y
   no declara compatibilidad con DPI alto; en pantallas al 125 % o 150 % Windows
   la escala y puede verse levemente difuminada. Si se requiere nitidez total, se
   puede migrar el diseño a `TableLayoutPanel` con `AutoScaleMode.Dpi`.
6. **Sistema de coordenadas.** El lienzo usa coordenadas de pantalla (Y hacia
   abajo). Para comparar con figuras de libros, donde Y crece hacia arriba, se
   podría agregar una transformación de vista sin modificar los datos del núcleo.
7. **Pruebas automatizadas.** Separar la lógica en clases sin dependencias gráficas
   (`CoordinateValidator`, `CurveInfo`) permite crear un proyecto de pruebas
   unitarias si el grupo decide agregarlo a la solución (cambio que debe
   acordarse, porque modifica `BezierVsDeCasteljau.sln`).
8. **Revisión cruzada.** Integrar `feature/ui-winforms` en `main` sólo mediante
   Pull Request revisado por otro integrante, como establece `CONTRIBUTING.md`.

---

## 8. Aporte a las conclusiones generales (área gráfica)

> La respuesta a la pregunta de investigación sobre eficiencia y estabilidad
> numérica debe sostenerse con las mediciones de SebastianCriollo. Desde la
> aplicación gráfica se aporta lo siguiente:

1. Ambos métodos se integraron en una misma aplicación a través de `BezierCore`,
   de modo que lo que se dibuja es exactamente lo que se mide en el benchmark.
2. La curva puede representarse cumpliendo la restricción del enunciado: los
   puntos se calculan matemáticamente y GDI+ sólo une puntos consecutivos con
   segmentos de recta. Al variar la cantidad de muestras se observa directamente
   que la curva dibujada es una aproximación poligonal cuya calidad depende del
   muestreo.
3. Con los grados probados (1 a 8) y en la escala de la pantalla, las curvas de
   Bernstein y De Casteljau se dibujan superpuestas sin diferencia visible. Esta
   es una observación visual; la cuantificación de las diferencias numéricas
   corresponde al análisis experimental.
4. El algoritmo de De Casteljau tiene una ventaja didáctica: sus niveles
   intermedios son puntos con significado geométrico (interpolaciones lineales
   sucesivas) que pueden mostrarse en pantalla, como se ve en las figuras 9a y 9b.
   La evaluación directa con polinomios de Bernstein no ofrece una construcción
   intermedia equivalente.
5. Para la cúbica de ejemplo, la aplicación muestra B(t) para t = 0, 0,25, 0,5,
   0,75 y 1 con los mismos valores que la fórmula de Bernstein, lo que permite
   contrastar en clase los cálculos manuales con el programa.
