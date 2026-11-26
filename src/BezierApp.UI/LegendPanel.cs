using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BezierApp.UI
{
    /// <summary>
    /// Leyenda de colores del lienzo. Cada muestra se dibuja con las mismas plumas
    /// de CurveStyles que usa CurveCanvas, por lo que siempre coincide con lo que
    /// se ve. Las capas desactivadas aparecen atenuadas con la marca «(oculto)».
    /// Las entradas se distribuyen en columnas según el alto disponible.
    /// </summary>
    public sealed class LegendPanel : Control
    {
        public const int RowHeight = 22;
        private const int ColumnWidth = 255;
        private const int SwatchWidth = 34;
        private const int MaxLevelRows = 6;

        private int _degree = -1;
        private bool _showBernstein = true;
        private bool _showDeCasteljau = true;
        private bool _showPolygon = true;
        private bool _showLevels;
        private bool _showSampleMarkers = true;

        /// <summary>Una fila de la leyenda: muestra gráfica + texto.</summary>
        private sealed class Entry
        {
            public Action<Graphics, RectangleF> DrawSwatch;
            public string Text;
            public bool Active;
        }

        public LegendPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.CanvasBackground;
        }

        /// <summary>
        /// Actualiza el estado de la leyenda. degree = -1 indica que no hay curva.
        /// </summary>
        public void SetState(int degree, bool bernstein, bool deCasteljau, bool polygon, bool levels, bool sampleMarkers)
        {
            _degree = degree;
            _showBernstein = bernstein;
            _showDeCasteljau = deCasteljau;
            _showPolygon = polygon;
            _showLevels = levels;
            _showSampleMarkers = sampleMarkers;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var entries = BuildEntries();
            int rowsPerColumn = Math.Max(1, ClientSize.Height / RowHeight);

            for (int i = 0; i < entries.Count; i++)
            {
                int column = i / rowsPerColumn;
                int row = i % rowsPerColumn;
                var bounds = new Rectangle(column * ColumnWidth, row * RowHeight, ColumnWidth, RowHeight);
                DrawEntry(g, entries[i], bounds);
            }
        }

        private List<Entry> BuildEntries()
        {
            var entries = new List<Entry>();

            entries.Add(LineEntry(CurveStyles.CreateControlPolygonPen, "Polígono de control", _showPolygon));
            entries.Add(new Entry { DrawSwatch = DrawHandleSwatch, Text = "Punto de control / seleccionado", Active = true });
            entries.Add(LineEntry(CurveStyles.CreateBernsteinPen, "Curva de Bernstein (directo)", _showBernstein));
            entries.Add(LineEntry(CurveStyles.CreateDeCasteljauPen, "Curva de De Casteljau", _showDeCasteljau));
            entries.Add(PointEntry(CurveStyles.SampleMarkerColor, CurveStyles.SampleMarkerRadius,
                string.Format("Muestras calculadas (≤ {0})", CurveCanvas.MaxMarkedSamples),
                _showBernstein && _showSampleMarkers));

            // Para una curva de grado n existen n - 1 niveles intermedios.
            int levelCount = Math.Max(_degree - 1, 0);
            for (int r = 1; r <= Math.Min(levelCount, MaxLevelRows); r++)
            {
                int level = r;
                entries.Add(LineEntry(() => CurveStyles.CreateLevelPen(level),
                    string.Format("Nivel {0} de interpolación", level), _showLevels));
            }

            if (levelCount > MaxLevelRows)
            {
                entries.Add(new Entry
                {
                    Text = levelCount == MaxLevelRows + 1
                        ? string.Format("Nivel {0}: se repite la paleta", levelCount)
                        : string.Format("Niveles {0} a {1}: se repite la paleta", MaxLevelRows + 1, levelCount),
                    Active = _showLevels
                });
            }

            entries.Add(PointEntry(CurveStyles.CurvePointColor, CurveStyles.CurvePointRadius,
                "Punto B(t) sobre la curva", _showLevels && _degree >= 1));

            return entries;
        }

        private static Entry LineEntry(Func<Pen> createPen, string text, bool active)
        {
            return new Entry
            {
                Text = text,
                Active = active,
                DrawSwatch = (g, r) =>
                {
                    using (var pen = createPen())
                        g.DrawLine(pen, r.Left, r.Top + r.Height / 2, r.Right, r.Top + r.Height / 2);
                }
            };
        }

        private static Entry PointEntry(Color color, float radius, string text, bool active)
        {
            return new Entry
            {
                Text = text,
                Active = active,
                DrawSwatch = (g, r) =>
                {
                    float cx = r.Left + r.Width / 2, cy = r.Top + r.Height / 2;
                    using (var brush = new SolidBrush(color))
                        g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
                }
            };
        }

        private static void DrawHandleSwatch(Graphics g, RectangleF r)
        {
            float cy = r.Top + r.Height / 2;
            using (var fill = new SolidBrush(CurveStyles.HandleFillColor))
            using (var selected = new SolidBrush(CurveStyles.SelectedHandleColor))
            using (var border = new Pen(CurveStyles.HandleBorderColor, 1.5f))
            {
                var normal = new RectangleF(r.Left + 2, cy - 6, 12, 12);
                var highlighted = new RectangleF(r.Left + 19, cy - 7, 14, 14);
                g.FillEllipse(fill, normal);
                g.DrawEllipse(border, normal);
                g.FillEllipse(selected, highlighted);
                g.DrawEllipse(border, highlighted);
            }
        }

        private void DrawEntry(Graphics g, Entry entry, Rectangle bounds)
        {
            if (entry.DrawSwatch != null)
                entry.DrawSwatch(g, new RectangleF(bounds.Left + 4, bounds.Top, SwatchWidth, bounds.Height));

            Color color = entry.Active ? UiTheme.SectionTitle : UiTheme.MutedText;
            string caption = entry.Active ? entry.Text : entry.Text + " (oculto)";
            var textBounds = new Rectangle(bounds.Left + SwatchWidth + 12, bounds.Top, bounds.Width - SwatchWidth - 14, bounds.Height);
            TextRenderer.DrawText(g, caption, Font, textBounds, color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
