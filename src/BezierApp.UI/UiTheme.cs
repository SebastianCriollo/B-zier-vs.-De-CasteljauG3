using System.Drawing;

namespace BezierApp.UI
{
    /// <summary>
    /// Colores de la interfaz (paneles, secciones y lienzo). Centralizarlos evita
    /// valores repetidos y mantiene una apariencia coherente en toda la ventana.
    /// </summary>
    internal static class UiTheme
    {
        public static readonly Color PanelBackground = Color.FromArgb(241, 243, 246);
        public static readonly Color SectionBackground = Color.FromArgb(241, 243, 246);
        public static readonly Color SectionTitle = Color.FromArgb(31, 41, 55);
        public static readonly Color CanvasBackground = Color.White;
        public static readonly Color CanvasGrid = Color.FromArgb(238, 241, 245);
        public static readonly Color MutedText = Color.FromArgb(107, 114, 128);
    }
}
