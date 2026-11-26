namespace BezierApp.UI
{
    /// <summary>
    /// Textos informativos sobre la curva actual. El grado se deriva de la
    /// cantidad de puntos de control: n = puntos - 1.
    /// </summary>
    public static class CurveInfo
    {
        /// <summary>Grado de la curva, o -1 si no existe curva (menos de 2 puntos).</summary>
        public static int GetDegree(int pointCount)
        {
            return pointCount >= 2 ? pointCount - 1 : -1;
        }

        public static string DescribeDegree(int pointCount)
        {
            if (pointCount == 0)
                return "Sin curva (no hay puntos)";
            if (pointCount == 1)
                return "Sin curva (faltan puntos)";

            int degree = GetDegree(pointCount);
            string name = DegreeName(degree);
            return name == null ? degree.ToString() : string.Format("{0} ({1})", degree, name);
        }

        private static string DegreeName(int degree)
        {
            switch (degree)
            {
                case 1: return "lineal";
                case 2: return "cuadrática";
                case 3: return "cúbica";
                case 4: return "cuártica";
                case 5: return "quíntica";
                default: return null;
            }
        }
    }
}
