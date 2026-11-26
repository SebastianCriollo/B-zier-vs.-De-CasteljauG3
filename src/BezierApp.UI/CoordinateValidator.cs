using System;
using System.Globalization;

namespace BezierApp.UI
{
    /// <summary>
    /// Convierte y valida el texto ingresado para una coordenada. No depende de
    /// ningún control gráfico, por lo que la misma regla sirve para X y para Y.
    ///
    /// Reglas:
    ///   - el campo no puede estar vacío;
    ///   - se aceptan negativos, decimales y notación científica (1e3);
    ///   - el separador decimal puede ser punto o coma (configuración regional);
    ///   - no se aceptan separadores de miles, NaN ni infinito;
    ///   - el valor debe estar dentro de [MinValue, MaxValue].
    /// </summary>
    public static class CoordinateValidator
    {
        public const double MinValue = -10000;
        public const double MaxValue = 10000;

        public static bool TryParse(string text, string axisName, out double value, out string error)
        {
            value = 0;
            error = null;

            string trimmed = text == null ? string.Empty : text.Trim();
            if (trimmed.Length == 0)
            {
                error = string.Format("La coordenada {0} es obligatoria.", axisName);
                return false;
            }

            // Se normaliza la coma a punto para aceptar ambos separadores decimales.
            string normalized = trimmed.Replace(',', '.');
            double parsed;
            bool isNumber = double.TryParse(normalized,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture, out parsed);

            if (!isNumber)
            {
                error = string.Format("La coordenada {0} debe ser un número (por ejemplo 120, -35.5 o 12,75).", axisName);
                return false;
            }

            if (double.IsNaN(parsed) || double.IsInfinity(parsed))
            {
                error = string.Format("La coordenada {0} debe ser un número finito.", axisName);
                return false;
            }

            if (parsed < MinValue || parsed > MaxValue)
            {
                error = string.Format(CultureInfo.InvariantCulture,
                    "La coordenada {0} debe estar entre {1} y {2}.", axisName, MinValue, MaxValue);
                return false;
            }

            value = parsed;
            return true;
        }

        /// <summary>Texto con el que se muestra una coordenada existente al editarla.</summary>
        public static string Format(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
