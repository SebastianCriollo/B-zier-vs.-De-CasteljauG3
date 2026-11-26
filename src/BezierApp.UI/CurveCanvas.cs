using System;
using System.Collections.Generic;
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

        private const int HandleRadius = 6;
        private int _draggingIndex = -1;
        private int _sampleCount = 100;

        // Muestras calculadas por BezierCore. Se recalculan sólo cuando cambian los
        // puntos de control o la cantidad de muestras, no en cada repintado.
        private IReadOnlyList<ControlPoint> _bernsteinSamples;
        private IReadOnlyList<ControlPoint> _deCasteljauSamples;

        public CurveCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = UiTheme.CanvasBackground;
            ControlPoints = new List<ControlPoint>();

            ShowBernstein = true;
            ShowDeCasteljau = true;
            ShowControlPolygon = true;
            InterpolationT = 0.5;
        }

        /// <summary>Puntos de control actuales. El grado de la curva es Count - 1.</summary>
        public List<ControlPoint> ControlPoints { get; private set; }

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

        /// <summary>Valor de t en el que se muestran las interpolaciones intermedias.</summary>
        public double InterpolationT { get; set; }

        /// <summary>Se dispara cuando el usuario agrega, mueve o elimina un punto.</summary>
        public event EventHandler ControlPointsChanged;

        public void AddControlPoint(ControlPoint point)
        {
            ControlPoints.Add(point);
            OnControlPointsChanged();
        }

        public void RemoveControlPointAt(int index)
        {
            if (index < 0 || index >= ControlPoints.Count) return;
            ControlPoints.RemoveAt(index);
            OnControlPointsChanged();
        }

        public void UpdateControlPoint(int index, ControlPoint point)
        {
            if (index < 0 || index >= ControlPoints.Count) return;
            ControlPoints[index] = point;
            OnControlPointsChanged();
        }

        public void ClearControlPoints()
        {
            ControlPoints.Clear();
            OnControlPointsChanged();
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
                    _draggingIndex = hit;                               // modificar: arrastrar
                else
                    AddControlPoint(new ControlPoint(e.X, e.Y));        // agregar
            }
            else if (e.Button == MouseButtons.Right && hit >= 0)
            {
                RemoveControlPointAt(hit);                              // eliminar
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_draggingIndex >= 0)
            {
                UpdateControlPoint(_draggingIndex, new ControlPoint(e.X, e.Y));
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

        private int HitTest(Point location)
        {
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                var p = ControlPoints[i];
                double dx = p.X - location.X;
                double dy = p.Y - location.Y;
                if (dx * dx + dy * dy <= (HandleRadius + 3) * (HandleRadius + 3))
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
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ControlPoints.Count < 2)
            {
                DrawHint(g);
                return;
            }

            if (ShowControlPolygon)
                DrawControlPolygon(g);

            if (ShowBernstein)
            {
                if (_bernsteinSamples == null)
                    _bernsteinSamples = _bernstein.Sample(ControlPoints, SampleCount);

                using (var pen = CurveStyles.CreateBernsteinPen())
                {
                    DrawPolyline(g, _bernsteinSamples, pen);
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

            // TODO (Damian12T): dibujar los niveles intermedios de De Casteljau
            //   usando _deCasteljau.EvaluateWithSteps(ControlPoints, InterpolationT).
            //   Cada nivel se dibuja como un polígono de un color más claro y el
            //   último nivel (un solo punto) se marca sobre la curva.
            if (ShowInterpolationSteps)
            {
                // var levels = _deCasteljau.EvaluateWithSteps(ControlPoints, InterpolationT);
            }

            DrawControlHandles(g);
        }

        private void DrawHint(Graphics g)
        {
            const string text = "Clic izquierdo: agregar punto · Clic derecho sobre un punto: eliminar · Arrastrar: modificar";
            using (var brush = new SolidBrush(Color.Gray))
            {
                g.DrawString(text, Font, brush, 12, 12);
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

        private void DrawControlHandles(Graphics g)
        {
            using (var fill = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.Black, 1.5f))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < ControlPoints.Count; i++)
                {
                    var p = ControlPoints[i];
                    var rect = new RectangleF(
                        (float)p.X - HandleRadius, (float)p.Y - HandleRadius,
                        HandleRadius * 2, HandleRadius * 2);

                    g.FillEllipse(fill, rect);
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
