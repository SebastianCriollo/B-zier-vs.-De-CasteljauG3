# Aporte al informe: núcleo matemático

Autor: Mario Santiago Anrrango Benavides (@Mario-Anrrango).
Rama: `feature/core-algoritmos`. Material para integrar en las secciones 1, 3 y 4;
la consolidación y revisión cruzada corresponden al grupo.

## 1. Introducción

El trabajo estudia dos formas de calcular puntos de una misma curva de Bézier:
la suma directa de polinomios de Bernstein y la construcción recursiva de De
Casteljau. La comparación conserva los puntos de control, el dominio y el
muestreo; así las diferencias observadas corresponden a la evaluación numérica.

La pregunta es qué método resulta más eficiente y estable bajo las condiciones
ensayadas. Para responder se separan validación matemática, medición de tiempo
y dibujo. Este aporte valida el núcleo compartido en C#; los tiempos y las
conclusiones experimentales se incorporarán desde el benchmark del equipo.

## 3. Marco teórico

### 3.1. Representación mediante Bernstein

Con n+1 puntos P_i = (x_i,y_i), la representación de grado n es:

B(t) = Σ desde i=0 hasta n de b_(i,n)(t) P_i, con 0 ≤ t ≤ 1.
b_(i,n)(t) = C(n,i) t^i (1-t)^(n-i).
C(n,i) = n! / (i!(n-i)!).

Los pesos son no negativos y suman uno. Esto explica la pertenencia a la
envolvente convexa y los extremos B(0)=P0 y B(1)=Pn. La representación se
aplica a cualquier grado matemático; el contrato del proyecto requiere n≥1.

El código calcula la fila binomial mediante c0=1 y
c_i=c_(i-1)·((n-i+1)/i), evitando factoriales. `Evaluate` prepara la fila en cada
llamada. `Sample` puede prepararla una vez y reutilizarla o recalcularla por
muestra con `UsePrecomputedCoefficients=false`.

La suma directa es sencilla de implementar y tiene un único recorrido de los
puntos por evaluación. Su limitación práctica es que combina potencias pequeñas
con coeficientes grandes; pueden aparecer redondeo, subdesbordamiento y
desbordamiento. El código detecta coeficientes infinitos y devuelve una excepción
clara. No promete exactitud ni soporte ilimitado de grados usando double.

### 3.2. Construcción de De Casteljau

P_i^(0)=P_i.
P_i^(r)=(1-t)P_i^(r-1)+tP_(i+1)^(r-1), r=1,...,n; i=0,...,n-r.
B(t)=P_0^(n).

Cada nivel elimina un punto hasta obtener el resultado. Esta construcción sirve
también para subdividir la curva, como describe el Hyperbook del MIT
(Patrikalakis et al., 2009, sección 1.3.5).

Ventajas: ofrece una interpretación geométrica y permite mostrar cada nivel.
En [0,1] emplea combinaciones convexas, que favorecen la estabilidad numérica
(Tychonievich, 2023). No elimina el redondeo ni garantiza error relativo pequeño
cuando el resultado está cerca de cero. Su desventaja principal es el número
cuadrático de interpolaciones.

### 3.3. Equivalencia y validación

La identidad de Pascal aplicada a las bases de Bernstein permite escribir:

b_(i,n)(t)=(1-t)b_(i,n-1)(t)+t b_(i-1,n-1)(t),

considerando cero los índices fuera del rango. Al sustituir y agrupar los puntos
adyacentes se obtiene la misma interpolación usada por De Casteljau. Repetir
la reducción demuestra que ambos calculan el mismo polinomio en aritmética exacta.

En double se compara con tolerancia. La distancia entre dos implementaciones
mide discrepancia, pero no identifica por sí sola cuál tiene menor error frente
al valor exacto: para ello hace falta una referencia independiente. El ejemplo
cúbico aporta esa referencia manual para cinco parámetros, desarrollada en
[validacion.md](../calculos-manuales/validacion.md).

### 3.4. Análisis de complejidad de esta implementación

Sea n el grado y m la cantidad de muestras. Se considera costo constante para
las operaciones double y `Math.Pow`, como modelo práctico del programa.

| Operación | Tiempo | Memoria auxiliar, sin salida |
|---|---|---|
| Fila de coeficientes | O(n) | O(n) |
| Bernstein, Evaluate | O(n) | O(n) |
| Bernstein, Sample | O(mn) | O(n) |
| Casteljau, Evaluate | O(n²) | O(n) |
| Casteljau, Sample | O(mn²) | O(n) |
| Casteljau, EvaluateWithSteps | O(n²) | O(n²), contando niveles devueltos |

De Casteljau ejecuta n+(n-1)+...+1=n(n+1)/2 interpolaciones para un t interior.
Su muestreo reutiliza un buffer por llamada, sin validar ni reservarlo por cada
muestra; no modifica los puntos recibidos. El arreglo de resultados de ambos
métodos requiere O(m) adicional. La validación inicial recorre O(n) puntos.

El precálculo binomial modifica las constantes y asignaciones, no el orden
O(mn): sin precálculo se ejecutan dos recorridos lineales por muestra. La
recurrencia binomial usada aquí no implica un triángulo completo O(n²).
Los extremos se devuelven directamente tras validar; el costo total de Evaluate
sigue incluyendo el recorrido de validación O(n). `EvaluateWithSteps` conserva
todos los niveles para la UI y debe excluirse del benchmark de evaluación.

## 4. Validación matemática y estado del aporte

El desarrollo transcrito y la tabla manual frente al programa están en
`docs/calculos-manuales/validacion.md`. El script `verificar.ps1` ejecuta pruebas
sin dependencias externas. Se conserva el contrato `IBezierEvaluator` para
integrarlo con la interfaz y el benchmark existentes.

Completado: revisión de fórmulas, grados 1 a 50, entradas inválidas, extremos,
niveles intermedios, desarrollo cúbico, contraste ejecutado y secciones teóricas.
Pendiente conjunto: revisión por otro integrante, mediciones reales del benchmark,
consolidación del informe y conclusión comparativa sustentada en esos datos.

Como aporte a las conclusiones, el ejemplo cúbico coincide en ambos métodos;
el conteo de operaciones predice crecimiento lineal frente a cuadrático con el
grado. Esta predicción no determina por sí sola qué método tarda menos para un
grado pequeño: se deben consultar los tiempos medidos por escenario.

## Referencias para integrar en APA 7

Patrikalakis, N. M., Maekawa, T., & Cho, W. (2009). *Shape interrogation for
computer aided design and manufacturing* (Hyperbook ed., sección 1.3.5).
Massachusetts Institute of Technology.
https://web.mit.edu/hyperbook/Patrikalakis-Maekawa-Cho/node13.html

Tychonievich, L. (2023, 9 de febrero). *Bézier curves*. CS 418, University of
Illinois Urbana-Champaign.
https://courses.grainger.illinois.edu/cs418/fa2024/text/bezier.html
