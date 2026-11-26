using System.Drawing;
using System.Drawing.Drawing2D;

namespace BezierApp.UI
{
    /// <summary>
    /// Estilos de trazo de cada elemento del lienzo. El lienzo y la leyenda crean
    /// sus plumas desde aquí, de modo que la leyenda siempre coincide con lo dibujado.
    /// Quien llama a un método Create* es responsable de liberar la pluma (using).
    /// </summary>
    internal static class CurveStyles
    {
        public static readonly Color BernsteinColor = Color.FromArgb(96, 150, 245);
        public const float BernsteinWidth = 6f;
        public static readonly Color SampleMarkerColor = Color.FromArgb(29, 78, 216);
        public const float SampleMarkerRadius = 3.5f;

        public static readonly Color ControlPolygonColor = Color.FromArgb(148, 156, 170);
        public static readonly Color HandleBorderColor = Color.FromArgb(31, 41, 55);
        public static readonly Color HandleFillColor = Color.White;
        public static readonly Color SelectedHandleColor = Color.FromArgb(250, 204, 21);
        public static readonly Color HandleLabelColor = Color.FromArgb(31, 41, 55);

        /// <summary>
        /// Bernstein: banda ancha de color claro. Al ser más ancha que la curva de
        /// De Casteljau, ambas permanecen visibles cuando coinciden. Se usa un color
        /// opaco porque, al dibujar segmento por segmento, un color semitransparente
        /// se oscurecería en cada unión entre segmentos.
        /// </summary>
        public static Pen CreateBernsteinPen()
        {
            var pen = new Pen(BernsteinColor, BernsteinWidth);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            return pen;
        }

        public static readonly Color DeCasteljauColor = Color.FromArgb(234, 88, 12);
        public const float DeCasteljauWidth = 2.2f;

        /// <summary>
        /// De Casteljau: línea discontinua delgada y oscura que se dibuja encima de
        /// la banda de Bernstein; así se ven las dos aunque coincidan exactamente.
        /// </summary>
        public static Pen CreateDeCasteljauPen()
        {
            var pen = new Pen(DeCasteljauColor, DeCasteljauWidth);
            pen.DashPattern = new[] { 4f, 2.5f };
            return pen;
        }

        public static readonly Color CurvePointColor = Color.FromArgb(220, 38, 38);
        public const float LevelWidth = 1.6f;
        public const float LevelPointRadius = 3.5f;
        public const float CurvePointRadius = 6f;

        // Un color por nivel de interpolación; si el grado supera la cantidad de
        // colores, la paleta se repite de forma cíclica.
        private static readonly Color[] LevelColors =
        {
            Color.FromArgb(22, 163, 74),
            Color.FromArgb(147, 51, 234),
            Color.FromArgb(8, 145, 178),
            Color.FromArgb(202, 138, 4),
            Color.FromArgb(71, 85, 105),
            Color.FromArgb(219, 39, 119)
        };

        /// <summary>Color del nivel r (r = 1 es la primera interpolación).</summary>
        public static Color GetLevelColor(int level)
        {
            return LevelColors[(level - 1) % LevelColors.Length];
        }

        public static Pen CreateLevelPen(int level)
        {
            return new Pen(GetLevelColor(level), LevelWidth);
        }

        public static Pen CreateControlPolygonPen()
        {
            var pen = new Pen(ControlPolygonColor, 1.2f);
            pen.DashStyle = DashStyle.Dash;
            return pen;
        }
    }
}
