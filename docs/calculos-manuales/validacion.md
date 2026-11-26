# Validación de la curva cúbica

Autor: Mario Santiago Anrrango Benavides (@Mario-Anrrango).

Se utiliza el caso acordado en el README de esta carpeta:
P0 = (120,480), P1 = (260,140), P2 = (520,140), P3 = (660,480).
El grado es n = 3. Los decimales de este desarrollo son exactos.

## Bernstein: desarrollo término por término

C(3,0) = 1; C(3,1) = 3; C(3,2) = 3; C(3,3) = 1.

B(t) = (1-t)^3 P0 + 3t(1-t)^2 P1 + 3t^2(1-t) P2 + t^3 P3.

Cada entrada de la siguiente tabla muestra coeficiente por potencias y su resultado:

| t | b0 | b1 | b2 | b3 |
|---|---|---|---|---|
| 0 | 1·1^3 = 1 | 3·0·1^2 = 0 | 3·0^2·1 = 0 | 1·0^3 = 0 |
| 0.25 | 1·0.75^3 = 0.421875 | 3·0.25·0.75^2 = 0.421875 | 3·0.25^2·0.75 = 0.140625 | 1·0.25^3 = 0.015625 |
| 0.5 | 1·0.5^3 = 0.125 | 3·0.5·0.5^2 = 0.375 | 3·0.5^2·0.5 = 0.375 | 1·0.5^3 = 0.125 |
| 0.75 | 1·0.25^3 = 0.015625 | 3·0.75·0.25^2 = 0.140625 | 3·0.75^2·0.25 = 0.421875 | 1·0.75^3 = 0.421875 |
| 1 | 1·0^3 = 0 | 3·1·0^2 = 0 | 3·1^2·0 = 0 | 1·1^3 = 1 |

Los pesos suman 1 en cada fila. Sustituyendo por coordenada:

- t = 0: x = 120 + 0 + 0 + 0 = 120; y = 480 + 0 + 0 + 0 = 480.
- t = 0.25: x = 50.625 + 109.6875 + 73.125 + 10.3125 = 243.75;
  y = 202.5 + 59.0625 + 19.6875 + 7.5 = 288.75.
- t = 0.5: x = 15 + 97.5 + 195 + 82.5 = 390;
  y = 60 + 52.5 + 52.5 + 60 = 225.
- t = 0.75: x = 1.875 + 36.5625 + 219.375 + 278.4375 = 536.25;
  y = 7.5 + 19.6875 + 59.0625 + 202.5 = 288.75.
- t = 1: x = 0 + 0 + 0 + 660 = 660; y = 0 + 0 + 0 + 480 = 480.

## De Casteljau: todos los niveles

Nivel 0: P0, P1, P2, P3 (los mismos puntos indicados arriba).
Para cada fila se aplica:

Q_i = (1-t)P_i + t P_(i+1), i = 0,1,2.
R_i = (1-t)Q_i + t Q_(i+1), i = 0,1.
B(t) = (1-t)R0 + t R1.

| t | Nivel 1: Q0; Q1; Q2 | Nivel 2: R0; R1 | Nivel 3: B(t) |
|---|---|---|---|
| 0 | (120,480); (260,140); (520,140) | (120,480); (260,140) | (120,480) |
| 0.25 | (155,395); (325,140); (555,225) | (197.5,331.25); (382.5,161.25) | (243.75,288.75) |
| 0.5 | (190,310); (390,140); (590,310) | (290,225); (490,225) | (390,225) |
| 0.75 | (225,225); (455,140); (625,395) | (397.5,161.25); (582.5,331.25) | (536.25,288.75) |
| 1 | (260,140); (520,140); (660,480) | (520,140); (660,480) | (660,480) |

Por ejemplo, para t = 0.25:

Q0 = 0.75(120,480) + 0.25(260,140) = (155,395).
Q1 = 0.75(260,140) + 0.25(520,140) = (325,140).
Q2 = 0.75(520,140) + 0.25(660,480) = (555,225).
R0 = 0.75(155,395) + 0.25(325,140) = (197.5,331.25).
R1 = 0.75(325,140) + 0.25(555,225) = (382.5,161.25).
B = 0.75(197.5,331.25) + 0.25(382.5,161.25) = (243.75,288.75).

## Contraste con ejecución real

La consola `VerificarCore.cs` evalúa ambos métodos y los compara con los cinco
resultados manuales escritos como constantes independientes de los algoritmos.

| t | Bernstein manual | Casteljau manual | Programa Bernstein | Programa Casteljau | Distancia entre métodos |
|---|---|---|---|---|---|
| 0 | (120,480) | (120,480) | (120,480) | (120,480) | 0 |
| 0.25 | (243.75,288.75) | (243.75,288.75) | (243.75,288.75) | (243.75,288.75) | 0 |
| 0.5 | (390,225) | (390,225) | (390,225) | (390,225) | 0 |
| 0.75 | (536.25,288.75) | (536.25,288.75) | (536.25,288.75) | (536.25,288.75) | 0 |
| 1 | (660,480) | (660,480) | (660,480) | (660,480) | 0 |

Coinciden los cinco casos. Los pesos y resultados de este ejemplo son fracciones
con denominador potencia de dos, representables exactamente en double; no se
extrapola esta igualdad exacta a otros puntos o parámetros.

## Reproducir la validación

En una terminal PowerShell situada en la raíz del repositorio:

```powershell
& ./docs/calculos-manuales/verificar.ps1
```

Requisitos: Visual Studio Community, Professional o Enterprise con compilador
C# y ensamblados de referencia de .NET Framework 4.8. El script compila con
`/langversion:7.3 /optimize+` y deposita el ejecutable en `src/BezierCore/bin/Validacion/`
(ignorado por Git). No requiere NuGet ni añade proyectos a la solución compartida.

Resultado de la ejecución del 26 de noviembre de 2026: **227357 comprobaciones
superadas**, usando el compilador de Visual Studio Community 2026 y .NET Framework
4.8. La solución completa también compiló en Release con C# 7.3.

Se comprueban resultados manuales, una recta, puntos coincidentes, grados 1 a 50
con semilla 280704, nueve valores de t incluyendo extremos y valores cercanos,
niveles intermedios, muestreo uniforme, precálculo activado/desactivado, ausencia
de cambios en la entrada y rechazo de entradas inválidas. Para comparaciones
generales se usa tolerancia por coordenada 1e-9·max(1, |x esperado|, |y esperado|).
Estas pruebas verifican consistencia; no sustituyen una referencia de precisión
arbitraria para medir error absoluto ni las mediciones de rendimiento del grupo.
