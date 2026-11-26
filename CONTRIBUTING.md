# Cómo trabajar en este repositorio

El objetivo es que el aporte de cada integrante quede registrado con **sus
propios commits**, para que el historial y el gráfico de contribuyentes de
GitHub reflejen el trabajo real de cada uno.

## 0. Una sola vez: identifícate en Git

Cada uno, en **su** computadora, con el correo de su cuenta de GitHub
(si el correo no coincide, los commits no se asocian a tu perfil):

```bash
git config --global user.name "Tu Nombre"
git config --global user.email "tu-correo-de-github@ejemplo.com"
```

## 1. Clonar el repositorio

```bash
git clone https://github.com/SebastianCriollo/B-zier-vs.-De-CasteljauG3.git
cd B-zier-vs.-De-CasteljauG3
```

## 2. Trabajar en tu rama

Nunca trabajes directo en `main`. Cada uno tiene su rama asignada en
[`TASKS.md`](TASKS.md):

| Integrante | Rama |
|---|---|
| @Mario-Anrrango | `feature/core-algoritmos` |
| @Damian12T | `feature/ui-winforms` |
| @SebastianCriollo | `feature/benchmark-comparacion` |

```bash
git checkout feature/tu-rama        # las ramas ya están creadas en el remoto
git pull                            # traer lo último antes de empezar
```

## 3. Commits

Haz commits **pequeños y frecuentes** — muchos commits que cuentan una historia
valen más que uno gigante al final.

```bash
git add src/TuProyecto/Archivo.cs
git commit -m "feat(core): implementar coeficientes binomiales con triángulo de Pascal"
```

Convención de mensajes:

| Prefijo | Para |
|---|---|
| `feat:` | funcionalidad nueva |
| `fix:` | corrección de un error |
| `refactor:` | reorganizar código sin cambiar comportamiento |
| `docs:` | informe, README, cálculos manuales |
| `test:` | pruebas y mediciones |
| `chore:` | configuración, limpieza |

Entre paréntesis, el área: `core`, `ui`, `benchmark`, `docs`.

## 4. Subir y abrir Pull Request

```bash
git push
```

Luego en GitHub: **Pull requests → New pull request**, de tu rama hacia `main`.
Describe qué hiciste y pide revisión a otro integrante. **Cada uno mergea su
propio PR** una vez revisado.

## 5. Mantenerte al día

Antes de seguir trabajando, trae los cambios que ya están en `main`:

```bash
git checkout main
git pull
git checkout feature/tu-rama
git merge main
```

## Reglas para no pisarse

- Edita **sólo** los archivos de tu carpeta asignada. Si necesitas tocar
  `src/BezierCore/` y no es tu área, avisa al grupo primero.
- `BezierVsDeCasteljau.sln`, `.gitignore` y los `README` se cambian de común
  acuerdo: son los archivos que más conflictos generan.
- No subas `bin/`, `obj/` ni carpetas de Visual Studio — ya están en
  `.gitignore`.
- Si aparece un conflicto, resuélvelo en tu rama antes de abrir el PR.

## Requisitos de entorno

- Windows con **Visual Studio 2022** y la carga de trabajo *Desarrollo de
  escritorio con .NET*.
- **.NET Framework 4.8** (todos los proyectos apuntan a `net48`, igual que los
  demás trabajos de Computación Gráfica).
- Escribir código compatible con C# 7.3 (el predeterminado de .NET Framework):
  sin `record`, `init`, `required`, `new()` abreviado ni `using var`.
