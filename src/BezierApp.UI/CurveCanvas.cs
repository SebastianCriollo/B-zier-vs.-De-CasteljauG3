using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BezierCore;

namespace BezierApp.UI
{
    /// <summary>
    /// Lienzo de dibujo con GDI+. Responsable ÚNICAMENTE de representar:
    ///   - el polígono de control y sus puntos (arrastrables),
    ///   - la curva obtenida con Bernstein,
    ///   - la curva obtenida con De Casteljau,
    ///   - los niveles intermedios de interpolación de De Casteljau para un t dado.
    ///
    /// RESTRICCIÓN DEL ENUNCIADO: no se permite usar funciones gráficas que generen
    /// la curva directamente. Por eso aquí SÓLO se usa DrawLine entre puntos ya
    /// calculados por BezierCore; está prohibido usar Graphics.DrawBezier o
    /// DrawCurve para producir la curva.
    ///
    /// Responsable: Damian12T
    /// </summary>
    public sealed class CurveCanvas : Panel
    {
        private readonly BernsteinEvaluator _bernstein = new BernsteinEvaluator();
        private readonly DeCasteljauEvaluator _deCasteljau = new DeCasteljauEvaluator();

        public const int MinSampleCount = 2;
        public const int MaxSampleCount = 10000;

        /// <summary>Hasta esta cantidad de muestras se marcan los puntos calculados.</summary>
        public const int MaxMarkedSamples = 50;

        private const int HandleRadius = 6;
        private const int GridSpacing = 50;
        private readonly List<ControlPoint> _controlPoints = new List<ControlPoint>();
        private int _draggingIndex = -1;
        private int _selectedIndex = -1;
        private int _sampleCount = 100;
        private double _interpolationT = 0.5;

        // Muestras calculadas por BezierCore. Se recalculan sólo cuando cambian los
        // puntos de control o la cantidad de muestras, no en cada repintado.
        private IReadOnlyList<ControlPoint> _bernsteinSamples;
        private IReadOnlyList<ControlPoint> _deCasteljauSamples;

        public CurveCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = UiTheme.CanvasBackground;

            ShowBernstein = true;
            ShowDeCasteljau = true;
            ShowControlPolygon = true;
            InterpolationT = 0.5;
        }

        /// <summary>
        /// Puntos de control actuales, en orden. El grado de la curva es Count - 1.
        /// Es de sólo lectura: toda modificación pasa por los métodos públicos para
        /// que siempre se disparen los eventos y se recalculen las curvas.
        /// </summary>
        public IReadOnlyList<ControlPoint> ControlPoints
        {
            get { return _controlPoints; }
        }

        /// <summary>Índice del punto seleccionado, o -1 si no hay selección.</summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int index = value >= 0 && value < _controlPoints.Count ? value : -1;
                if (index == _selectedIndex) return;
                _selectedIndex = index;
                Invalidate();
                if (SelectionChanged != null)
                    SelectionChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Cantidad de muestras con las que se discretiza cada curva. Se limita a
        /// [MinSampleCount, MaxSampleCount]: con menos de 2 muestras no existe ningún
        /// segmento y BezierCore dividiría entre (muestras - 1) = 0.
        /// </summary>
        public int SampleCount
        {
            get { return _sampleCount; }
            set
            {
                int clamped = Math.Max(MinSampleCount, Math.Min(MaxSampleCount, value));
                if (clamped == _sampleCount) return;
                _sampleCount = clamped;
                InvalidateCurves();
            }
        }

        public bool ShowBernstein { get; set; }
        public bool ShowDeCasteljau { get; set; }
        public bool ShowControlPolygon { get; set; }

        /// <summary>Si es true, se dibujan los niveles intermedios de De Casteljau.</summary>
        public bool ShowInterpolationSteps { get; set; }

        /// <summary>Valor de t, limitado a [0, 1], en el que se muestran las interpolaciones.</summary>
        public double InterpolationT
        {
            get { return _interpolationT; }
            set
            {
                double clamped = double.IsNaN(value) ? 0.0 : Math.Max(0.0, Math.Min(1.0, value));
                if (clamped == _interpolationT) return;
                _interpolationT = clamped;
                Invalidate();
            }
        }

        /// <summary>Se dispara cuando se agrega, mueve o elimina un punto.</summary>
        public event EventHandler ControlPointsChanged;

        /// <summary>Se dispara cuando cambia el punto seleccionado.</summary>
        public event EventHandler SelectionChanged;

        /// <summary>Se dispara al hacer doble clic sobre un punto (para editar sus coordenadas).</summary>
        public event EventHandler<ControlPointEventArgs> EditRequested;

        /// <summary>Agrega un punto al final de la lista y lo selecciona.</summary>
        public void AddControlPoint(ControlPoint point)
        {
            _controlPoints.Add(point);
            OnControlPointsChanged();
            SelectedIndex = _controlPoints.Count - 1;
        }

        /// <summary>
        /// Elimina un punto conservando el orden de los demás. La selección pasa al
        /// punto que ocupa ahora esa posición (o al último), para poder eliminar
        /// varios seguidos; si se eliminó otro punto, la selección se desplaza.
        /// </summary>
        public void RemoveControlPointAt(int index)
        {
            if (index < 0 || index >= _controlPoints.Count) return;

            _controlPoints.RemoveAt(index);
            _draggingIndex = -1;

            int selected = _selectedIndex;
            if (selected > index)
                selected--;
            else if (selected == index)
                selected = Math.Min(index, _controlPoints.Count - 1);

            _selectedIndex = -1;
            OnControlPointsChanged();
            SelectedIndex = selected;
        }

        public void UpdateControlPoint(int index, ControlPoint point)
        {
            if (index < 0 || index >= _controlPoints.Count) return;
            _controlPoints[index] = point;
            OnControlPointsChanged();
        }

        /// <summary>Reemplaza todos los puntos de control (p. ej. al cargar la curva de ejemplo).</summary>
        public void SetControlPoints(IEnumerable<ControlPoint> points)
        {
            _controlPoints.Clear();
            _controlPoints.AddRange(points);
            _draggingIndex = -1;
            _selectedIndex = -1;
            OnControlPointsChanged();
            SelectedIndex = -1;
        }

        public void ClearControlPoints()
        {
            SetControlPoints(new ControlPoint[0]);
        }

        /// <summary>Descarta las muestras en caché y solicita un repintado.</summary>
        private void InvalidateCurves()
        {
            _bernsteinSamples = null;
            _deCasteljauSamples = null;
            Invalidate();
        }

        private void OnControlPointsChanged()
        {
            InvalidateCurves();
            if (ControlPointsChanged != null)
                ControlPointsChanged(this, EventArgs.Empty);
        }

        // ---------------------------------------------------------------------
        // Interacción con el mouse: agregar / arrastrar / eliminar puntos
        // ---------------------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int hit = HitTest(e.Location);

            if (e.Button == MouseButtons.Left)
            {
                if (hit >= 0)
                {
                    SelectedIndex = hit;                                // seleccionar
                    _draggingIndex = hit;                               // modificar: arrastrar
                }
                else if (e.Clicks == 1)
                {
                    // El segundo clic de un doble clic no agrega otro punto.
                    AddControlPoint(new ControlPoint(e.X, e.Y));        // agregar
                }
            }
            else if (e.Button == MouseButtons.Right && hit >= 0)
            {
                RemoveControlPointAt(hit);                              // eliminar
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            int hit = HitTest(e.Location);
            if (e.Button == MouseButtons.Left && hit >= 0)
            {
                _draggingIndex = -1;
                if (EditRequested != null)
                    EditRequested(this, new ControlPointEventArgs(hit));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_draggingIndex >= 0)
            {
                // Se limita al área visible para que un punto no se pierda fuera del lienzo.
                int x = Math.Max(0, Math.Min(ClientSize.Width - 1, e.X));
                int y = Math.Max(0, Math.Min(ClientSize.Height - 1, e.Y));
                UpdateControlPoint(_draggingIndex, new ControlPoint(x, y));
            }
            else
            {
                Cursor = HitTest(e.Location) >= 0 ? Cursors.Hand : Cursors.Cross;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _draggingIndex = -1;
        }

        /// <summary>
        /// Devuelve el índice del punto bajo el cursor. Se recorre del último al
        /// primero porque los últimos se dibujan encima: con puntos repetidos se
        /// toma el que el usuario ve.
        /// </summary>
        private int HitTest(Point location)
        {
            const int tolerance = HandleRadius + 3;

            for (int i = _controlPoints.Count - 1; i >= 0; i--)
            {
                var p = _controlPoints[i];
                double dx = p.X - location.X;
                double dy = p.Y - location.Y;
                if (dx * dx + dy * dy <= tolerance * tolerance)
                    return i;
            }

            return -1;
        }

        // ---------------------------------------------------------------------
        // Dibujo
        // ---------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            DrawGrid(g);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ControlPoints.Count < 2)
            {
                // Con 0 o 1 punto no existe curva (BezierCore exige al menos 2),
                // pero el punto existente debe seguir visible y editable.
                DrawHint(g);
                DrawControlHandles(g);
                return;
            }

            if (ShowControlPolygon)
                DrawControlPolygon(g);

            string bernsteinError = null;
            if (ShowBernstein)
            {
                try
                {
                    if (_bernsteinSamples == null)
                        _bernsteinSamples = _bernstein.Sample(ControlPoints, SampleCount);

                    using (var pen = CurveStyles.CreateBernsteinPen())
                    {
                        DrawPolyline(g, _bernsteinSamples, pen);
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // BezierCore rechaza grados cuyos coeficientes binomiales exceden
                    // double; se informa y se continúa con De Casteljau.
                    _bernsteinSamples = null;
                    bernsteinError = "Bernstein no disponible: " + ex.Message.Split('\n')[0].Trim();
                }
            }

            // Se dibuja después de Bernstein para quedar encima de su banda ancha.
            if (ShowDeCasteljau)
            {
                if (_deCasteljauSamples == null)
                    _deCasteljauSamples = _deCasteljau.Sample(ControlPoints, SampleCount);

                using (var pen = CurveStyles.CreateDeCasteljauPen())
                {
                    DrawPolyline(g, _deCasteljauSamples, pen);
                }
            }

            if (ShowBernstein && _bernsteinSamples != null && SampleCount <= MaxMarkedSamples)
                DrawSampleMarkers(g, _bernsteinSamples);

            if (ShowInterpolationSteps)
                DrawInterpolationLevels(g);

            DrawControlHandles(g);

            if (bernsteinError != null)
                DrawLabel(g, bernsteinError, 12, 12, CurveStyles.CurvePointColor);
        }

        /// <summary>Cuadrícula tenue de referencia (cada GridSpacing píxeles).</summary>
        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(UiTheme.CanvasGrid, 1f))
            {
                for (int x = GridSpacing; x < ClientSize.Width; x += GridSpacing)
                    g.DrawLine(pen, x, 0, x, ClientSize.Height);
                for (int y = GridSpacing; y < ClientSize.Height; y += GridSpacing)
                    g.DrawLine(pen, 0, y, ClientSize.Width, y);
            }
        }

        private void DrawHint(Graphics g)
        {
            string text = ControlPoints.Count == 0
                ? "Sin puntos de control. Clic izquierdo para agregar el primero."
                : "Agregue al menos otro punto: una curva de Bézier necesita 2 o más puntos de control.";

            using (var brush = new SolidBrush(UiTheme.MutedText))
            {
                g.DrawString(text, Font, brush, 12, 12);
                g.DrawString("Clic izquierdo: agregar · Arrastrar: mover · Doble clic: editar · Clic derecho: eliminar",
                    Font, brush, 12, 32);
            }
        }

        private void DrawControlPolygon(Graphics g)
        {
            using (var pen = CurveStyles.CreateControlPolygonPen())
            {
                for (int i = 0; i < ControlPoints.Count - 1; i++)
                {
                    g.DrawLine(pen,
                        (float)ControlPoints[i].X, (float)ControlPoints[i].Y,
                        (float)ControlPoints[i + 1].X, (float)ControlPoints[i + 1].Y);
                }
            }
        }

        /// <summary>
        /// Construcción geométrica de De Casteljau para el t actual. BezierCore
        /// devuelve todos los niveles: el nivel 0 son los puntos de control (ya
        /// dibujados como polígono), los niveles 1..n-1 son los polígonos
        /// intermedios y el último nivel contiene el único punto B(t) de la curva.
        /// </summary>
        private void DrawInterpolationLevels(Graphics g)
        {
            var levels = _deCasteljau.EvaluateWithSteps(ControlPoints, InterpolationT);

            for (int r = 1; r < levels.Count - 1; r++)
            {
                var level = levels[r];
                using (var pen = CurveStyles.CreateLevelPen(r))
                using (var brush = new SolidBrush(pen.Color))
                {
                    for (int i = 0; i < level.Count - 1; i++)
                    {
                        g.DrawLine(pen,
                            (float)level[i].X, (float)level[i].Y,
                            (float)level[i + 1].X, (float)level[i + 1].Y);
                    }

                    for (int i = 0; i < level.Count; i++)
                        FillCircle(g, brush, level[i], CurveStyles.LevelPointRadius);
                }
            }

            var curvePoint = levels[levels.Count - 1][0];
            using (var fill = new SolidBrush(CurveStyles.CurvePointColor))
            using (var border = new Pen(Color.White, 2f))
            {
                FillCircle(g, fill, curvePoint, CurveStyles.CurvePointRadius);
                g.DrawEllipse(border, CircleBounds(curvePoint, CurveStyles.CurvePointRadius));
            }

            string label = string.Format(CultureInfo.InvariantCulture,
                "B({0:0.###}) = ({1:0.##}, {2:0.##})", InterpolationT, curvePoint.X, curvePoint.Y);
            DrawLabel(g, label, (float)curvePoint.X + 10, (float)curvePoint.Y + 8, CurveStyles.CurvePointColor);
        }

        /// <summary>
        /// Marca los puntos calculados por BezierCore. Con pocas muestras deja ver
        /// que la curva es una aproximación por segmentos entre esos puntos.
        /// </summary>
        private static void DrawSampleMarkers(Graphics g, IReadOnlyList<ControlPoint> samples)
        {
            using (var brush = new SolidBrush(CurveStyles.SampleMarkerColor))
            using (var border = new Pen(Color.White, 1f))
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    FillCircle(g, brush, samples[i], CurveStyles.SampleMarkerRadius);
                    g.DrawEllipse(border, CircleBounds(samples[i], CurveStyles.SampleMarkerRadius));
                }
            }
        }

        private static RectangleF CircleBounds(ControlPoint center, float radius)
        {
            return new RectangleF((float)center.X - radius, (float)center.Y - radius, radius * 2, radius * 2);
        }

        private static void FillCircle(Graphics g, Brush brush, ControlPoint center, float radius)
        {
            g.FillEllipse(brush, CircleBounds(center, radius));
        }

        /// <summary>Texto con fondo blanco translúcido para que se lea sobre las curvas.</summary>
        private void DrawLabel(Graphics g, string text, float x, float y, Color color)
        {
            SizeF size = g.MeasureString(text, Font);
            using (var background = new SolidBrush(Color.FromArgb(220, Color.White)))
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(background, x - 2, y - 1, size.Width + 4, size.Height + 2);
                g.DrawString(text, Font, brush, x, y);
            }
        }

        private void DrawControlHandles(Graphics g)
        {
            using (var fill = new SolidBrush(CurveStyles.HandleFillColor))
            using (var selectedFill = new SolidBrush(CurveStyles.SelectedHandleColor))
            using (var pen = new Pen(CurveStyles.HandleBorderColor, 1.5f))
            using (var textBrush = new SolidBrush(CurveStyles.HandleLabelColor))
            {
                for (int i = 0; i < ControlPoints.Count; i++)
                {
                    var p = ControlPoints[i];
                    bool selected = i == _selectedIndex;
                    float radius = selected ? HandleRadius + 2 : HandleRadius;
                    var rect = CircleBounds(p, radius);

                    g.FillEllipse(selected ? selectedFill : fill, rect);
                    g.DrawEllipse(pen, rect);
                    g.DrawString("P" + i, Font, textBrush, (float)p.X + HandleRadius, (float)p.Y - HandleRadius - 14);
                }
            }
        }

        /// <summary>
        /// Pinta una curva ya muestreada por BezierCore uniendo cada par de muestras
        /// consecutivas con un segmento de recta (DrawLine). Es la única primitiva
        /// permitida: no se usa DrawCurve ni DrawBezier.
        /// </summary>
        private static void DrawPolyline(Graphics g, IReadOnlyList<ControlPoint> samples, Pen pen)
        {
            bool dashed = pen.DashStyle != DashStyle.Solid;
            double travelled = 0.0;

            for (int i = 0; i < samples.Count - 1; i++)
            {
                // Cada DrawLine reinicia el patrón de guiones; desplazándolo según la
                // longitud ya recorrida, la línea discontinua se ve continua aunque
                // los segmentos sean muy cortos (muchas muestras).
                if (dashed)
                    pen.DashOffset = (float)(travelled / pen.Width);

                g.DrawLine(pen,
                    (float)samples[i].X, (float)samples[i].Y,
                    (float)samples[i + 1].X, (float)samples[i + 1].Y);

                travelled += ControlPoint.Distance(samples[i], samples[i + 1]);
            }
        }
    }
}
