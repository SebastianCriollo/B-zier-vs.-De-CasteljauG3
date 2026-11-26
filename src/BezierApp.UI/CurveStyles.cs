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

        public static readonly Color ControlPolygonColor = Color.FromArgb(148, 156, 170);
        public static readonly Color HandleBorderColor = Color.FromArgb(31, 41, 55);
        public static readonly Color HandleFillColor = Color.White;
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

        public static Pen CreateControlPolygonPen()
        {
            var pen = new Pen(ControlPolygonColor, 1.2f);
            pen.DashStyle = DashStyle.Dash;
            return pen;
        }
    }
}
