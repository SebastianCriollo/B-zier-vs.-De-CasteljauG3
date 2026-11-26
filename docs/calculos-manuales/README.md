# Validación matemática manual

Fase de **validación matemática** del enunciado: calcular a mano los puntos de
una curva cúbica con ambos métodos y verificar que coinciden con la salida del
programa.

Responsable: **Mario-Anrrango**

## Caso de prueba acordado

Curva cúbica (n = 3, cuatro puntos de control), la misma que carga la aplicación
al abrirse, para poder contrastar directamente:

```
P0 = (120, 480)
P1 = (260, 140)
P2 = (520, 140)
P3 = (660, 480)
```

Valores de t a calcular a mano: **t = 0, 0.25, 0.5, 0.75, 1**

## Método 1 — Bernstein (evaluación directa)

Para n = 3 la curva es:

```
B(t) = (1-t)³·P0 + 3t(1-t)²·P1 + 3t²(1-t)·P2 + t³·P3
```

Desarrollar término por término para cada t, mostrando los coeficientes
binomiales C(3,i) = 1, 3, 3, 1 y las potencias.

## Método 2 — De Casteljau

Interpolación lineal por niveles:

```
Nivel 1:  Q0 = (1-t)P0 + tP1     Q1 = (1-t)P1 + tP2     Q2 = (1-t)P2 + tP3
Nivel 2:  R0 = (1-t)Q0 + tQ1     R1 = (1-t)Q1 + tQ2
Nivel 3:  B(t) = (1-t)R0 + tR1
```

Dejar escritos **todos** los niveles intermedios: son los mismos que la
aplicación debe dibujar cuando se activa la casilla de interpolaciones.

## Tabla de verificación

Completar con los valores obtenidos a mano y los que imprime el programa:

| t | Bernstein (manual) | De Casteljau (manual) | Programa | ¿Coincide? |
|---|---|---|---|---|
| 0.00 | | | | |
| 0.25 | | | | |
| 0.50 | | | | |
| 0.75 | | | | |
| 1.00 | | | | |

## Archivos a dejar aquí

- Fotos o escaneos del desarrollo a mano (si se hace manuscrito).
- `validacion.md` o el documento con el desarrollo transcrito.
