using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using BezierCore;

namespace BezierApp.UI
{
    /// <summary>
    /// Ventana principal de la aplicación. Panel de control a la derecha y lienzo
    /// de dibujo a la izquierda.
    ///
    /// Requerimientos que cubre esta ventana:
    ///   - Agregar, eliminar y modificar puntos de control.
    ///   - Generar curvas de Bézier de diferentes grados (el grado se deriva del
    ///     número de puntos de control).
    ///   - Visualizar simultáneamente los resultados de ambos algoritmos.
    ///   - Configurar la cantidad de muestras.
    ///   - Visualizar las interpolaciones de De Casteljau.
    ///
    /// Responsable: Damian12T
    /// </summary>
    public sealed class MainForm : Form
    {
        private const int SidePanelWidth = 320;
        private const int SectionWidth = 286;
        private const int InnerWidth = SectionWidth - 24;

        private readonly CurveCanvas _canvas = new CurveCanvas();
        private readonly ListBox _pointsList = new ListBox();
        private readonly NumericUpDown _samplesInput = new NumericUpDown();
        private readonly TrackBar _tSlider = new TrackBar();
        private readonly NumericUpDown _tInput = new NumericUpDown();
        private readonly Label _degreeLabel = new Label();
        private readonly Label _tLabel = new Label();
        private readonly CheckBox _showBernstein = new CheckBox();
        private readonly CheckBox _showDeCasteljau = new CheckBox();
        private readonly CheckBox _showSteps = new CheckBox();
        private readonly CheckBox _showPolygon = new CheckBox();
        private bool _syncingT;
        private bool _syncingSelection;

        public MainForm()
        {
            Text = "Curvas de Bézier — Bernstein vs. De Casteljau";
            Font = new Font("Segoe UI", 9f);
            MinimumSize = new Size(1000, 680);
            Size = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            BuildLayout();
            WireEvents();
            LoadSampleCubicCurve();
            RefreshPointsList();
        }

        // ---------------------------------------------------------------------
        // Construcción de la interfaz
        // ---------------------------------------------------------------------

        private void BuildLayout()
        {
            var sidePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = SidePanelWidth,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10, 8, 0, 8),
                BackColor = UiTheme.PanelBackground
            };

            sidePanel.Controls.Add(BuildPointsSection());
            sidePanel.Controls.Add(BuildSamplesSection());
            sidePanel.Controls.Add(BuildVisualizationSection());
            sidePanel.Controls.Add(BuildParameterSection());

            _canvas.Dock = DockStyle.Fill;

            // WinForms acopla los controles en orden z inverso: el lienzo (Dock=Fill)
            // debe quedar al frente para acoplarse al final y ocupar sólo el espacio
            // que deja el panel lateral, sin quedar oculto debajo de él.
            Controls.Add(sidePanel);
            Controls.Add(_canvas);
            _canvas.BringToFront();
        }

        private Control BuildPointsSection()
        {
            var section = MakeSection("Puntos de control", 300);
            int y = 24;

            _pointsList.SetBounds(12, y, InnerWidth, 170);
            _pointsList.IntegralHeight = false;
            section.Controls.Add(_pointsList);
            y += 178;

            int buttonWidth = (InnerWidth - 12) / 3;
            var addButton = MakeButton("Agregar…", 12, y, buttonWidth);
            var editButton = MakeButton("Modificar…", 18 + buttonWidth, y, buttonWidth);
            var deleteButton = MakeButton("Eliminar", 24 + buttonWidth * 2, y, buttonWidth);
            addButton.Click += (s, e) => PromptAddPoint();
            editButton.Click += (s, e) => PromptEditSelectedPoint();
            deleteButton.Click += (s, e) => DeleteSelectedPoint();
            section.Controls.Add(addButton);
            section.Controls.Add(editButton);
            section.Controls.Add(deleteButton);
            y += 36;

            int halfWidth = (InnerWidth - 6) / 2;
            var clearButton = MakeButton("Limpiar todo", 12, y, halfWidth);
            var sampleButton = MakeButton("Curva de ejemplo", 18 + halfWidth, y, halfWidth);
            clearButton.Click += (s, e) => _canvas.ClearControlPoints();
            sampleButton.Click += (s, e) => LoadSampleCubicCurve();
            section.Controls.Add(clearButton);
            section.Controls.Add(sampleButton);
            y += 36;

            _degreeLabel.SetBounds(12, y, InnerWidth, 20);
            _degreeLabel.Font = new Font(Font, FontStyle.Bold);
            section.Controls.Add(_degreeLabel);

            return section;
        }

        private Control BuildSamplesSection()
        {
            var section = MakeSection("Cantidad de muestras", 64);

            _samplesInput.SetBounds(12, 26, InnerWidth, 24);
            _samplesInput.Minimum = 2;
            _samplesInput.Maximum = 100000;
            _samplesInput.Value = 100;
            _samplesInput.Increment = 10;
            section.Controls.Add(_samplesInput);

            return section;
        }

        private Control BuildVisualizationSection()
        {
            var section = MakeSection("Visualización", 136);
            int y = 24;

            _showBernstein.SetBounds(12, y, InnerWidth, 22);
            _showBernstein.Text = "Curva por Bernstein (directo)";
            _showBernstein.Checked = true;
            section.Controls.Add(_showBernstein);
            y += 26;

            _showDeCasteljau.SetBounds(12, y, InnerWidth, 22);
            _showDeCasteljau.Text = "Curva por De Casteljau";
            _showDeCasteljau.Checked = true;
            section.Controls.Add(_showDeCasteljau);
            y += 26;

            _showPolygon.SetBounds(12, y, InnerWidth, 22);
            _showPolygon.Text = "Polígono de control";
            _showPolygon.Checked = true;
            section.Controls.Add(_showPolygon);
            y += 26;

            _showSteps.SetBounds(12, y, InnerWidth, 22);
            _showSteps.Text = "Niveles de interpolación (De Casteljau)";
            section.Controls.Add(_showSteps);

            return section;
        }

        /// <summary>
        /// Selector del parámetro t: el deslizador permite recorrer la curva y el
        /// campo numérico ingresar valores exactos (0, 0.25, 0.5, 0.75, 1).
        /// </summary>
        private Control BuildParameterSection()
        {
            var section = MakeSection("Parámetro t", 104);

            _tLabel.SetBounds(12, 28, 150, 20);
            section.Controls.Add(_tLabel);

            _tInput.SetBounds(InnerWidth - 78, 24, 90, 24);
            _tInput.Minimum = 0;
            _tInput.Maximum = 1;
            _tInput.DecimalPlaces = 3;
            _tInput.Increment = 0.05m;
            _tInput.Value = 0.5m;
            _tInput.TextAlign = HorizontalAlignment.Right;
            section.Controls.Add(_tInput);

            _tSlider.SetBounds(12, 56, InnerWidth, 40);
            _tSlider.Minimum = 0;
            _tSlider.Maximum = 1000;
            _tSlider.Value = 500;
            _tSlider.TickFrequency = 50;
            _tSlider.SmallChange = 5;
            _tSlider.LargeChange = 50;
            section.Controls.Add(_tSlider);

            return section;
        }

        private static GroupBox MakeSection(string title, int height)
        {
            return new GroupBox
            {
                Text = title,
                Size = new Size(SectionWidth, height),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = UiTheme.SectionBackground,
                ForeColor = UiTheme.SectionTitle
            };
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Bounds = new Rectangle(x, y, width, 28),
                ForeColor = SystemColors.ControlText,
                BackColor = SystemColors.Control,
                UseVisualStyleBackColor = true
            };
        }

        // ---------------------------------------------------------------------
        // Eventos
        // ---------------------------------------------------------------------

        private void WireEvents()
        {
            _canvas.ControlPointsChanged += (s, e) => RefreshPointsList();
            _canvas.SelectionChanged += (s, e) => SyncListSelectionFromCanvas();
            _canvas.EditRequested += (s, e) => PromptEditPoint(e.Index);

            _samplesInput.ValueChanged += (s, e) =>
            {
                _canvas.SampleCount = (int)_samplesInput.Value;
                _canvas.Invalidate();
            };

            _showBernstein.CheckedChanged += (s, e) =>
            {
                _canvas.ShowBernstein = _showBernstein.Checked;
                _canvas.Invalidate();
            };

            _showDeCasteljau.CheckedChanged += (s, e) =>
            {
                _canvas.ShowDeCasteljau = _showDeCasteljau.Checked;
                _canvas.Invalidate();
            };

            _showPolygon.CheckedChanged += (s, e) =>
            {
                _canvas.ShowControlPolygon = _showPolygon.Checked;
                _canvas.Invalidate();
            };

            _showSteps.CheckedChanged += (s, e) =>
            {
                _canvas.ShowInterpolationSteps = _showSteps.Checked;
                _canvas.Invalidate();
            };

            _tSlider.ValueChanged += (s, e) => SetInterpolationT(_tSlider.Value / 1000.0);
            _tInput.ValueChanged += (s, e) => SetInterpolationT((double)_tInput.Value);

            _pointsList.SelectedIndexChanged += (s, e) =>
            {
                if (!_syncingSelection)
                    _canvas.SelectedIndex = _pointsList.SelectedIndex;
            };
            _pointsList.DoubleClick += (s, e) => PromptEditSelectedPoint();
            _pointsList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    DeleteSelectedPoint();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    PromptEditSelectedPoint();
                    e.Handled = true;
                }
            };
        }

        /// <summary>Curva cúbica inicial, útil para contrastar con los cálculos manuales del informe.</summary>
        private void LoadSampleCubicCurve()
        {
            _canvas.SetControlPoints(new[]
            {
                new ControlPoint(120, 480),
                new ControlPoint(260, 140),
                new ControlPoint(520, 140),
                new ControlPoint(660, 480)
            });
        }

        /// <summary>
        /// Reconstruye la lista a partir de los puntos del lienzo, que son la única
        /// fuente de verdad. Conserva la selección y la posición de desplazamiento.
        /// </summary>
        private void RefreshPointsList()
        {
            int topIndex = _pointsList.TopIndex;

            _syncingSelection = true;
            try
            {
                _pointsList.BeginUpdate();
                _pointsList.Items.Clear();
                for (int i = 0; i < _canvas.ControlPoints.Count; i++)
                    _pointsList.Items.Add(FormatPoint(i, _canvas.ControlPoints[i]));

                if (topIndex < _pointsList.Items.Count)
                    _pointsList.TopIndex = topIndex;
                _pointsList.SelectedIndex = _canvas.SelectedIndex;
                _pointsList.EndUpdate();
            }
            finally
            {
                _syncingSelection = false;
            }

            int degree = Math.Max(_canvas.ControlPoints.Count - 1, 0);
            _degreeLabel.Text = string.Format("Grado de la curva: {0}  ({1} puntos)", degree, _canvas.ControlPoints.Count);
            UpdateTLabel();
        }

        /// <summary>
        /// Punto único de cambio de t: actualiza el lienzo y sincroniza el deslizador
        /// con el campo numérico sin provocar eventos recursivos.
        /// </summary>
        private void SetInterpolationT(double t)
        {
            if (_syncingT) return;
            _syncingT = true;
            try
            {
                _canvas.InterpolationT = t;
                _tSlider.Value = (int)Math.Round(_canvas.InterpolationT * 1000.0);
                _tInput.Value = (decimal)_canvas.InterpolationT;
                UpdateTLabel();
            }
            finally
            {
                _syncingT = false;
            }
        }

        private static string FormatPoint(int index, ControlPoint p)
        {
            return string.Format(CultureInfo.InvariantCulture, "P{0} = ({1:0.##}, {2:0.##})", index, p.X, p.Y);
        }

        private void SyncListSelectionFromCanvas()
        {
            if (_pointsList.SelectedIndex == _canvas.SelectedIndex) return;

            _syncingSelection = true;
            try
            {
                _pointsList.SelectedIndex = _canvas.SelectedIndex;
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void UpdateTLabel()
        {
            _tLabel.Text = "t = " + _canvas.InterpolationT.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private void PromptAddPoint()
        {
            // Se propone el centro del lienzo para que el nuevo punto quede visible.
            var suggested = new ControlPoint(_canvas.ClientSize.Width / 2, _canvas.ClientSize.Height / 2);
            string description = string.Format("El punto se agregará al final como P{0}. Origen en la esquina superior izquierda; Y crece hacia abajo.",
                _canvas.ControlPoints.Count);

            ControlPoint point;
            if (PointEditorForm.TryGetPoint(this, "Agregar punto de control", description, suggested, out point))
                _canvas.AddControlPoint(point);
        }

        private void PromptEditSelectedPoint()
        {
            int index = _canvas.SelectedIndex;
            if (index < 0)
            {
                ShowSelectionRequired("Modificar punto");
                return;
            }

            PromptEditPoint(index);
        }

        private void PromptEditPoint(int index)
        {
            if (index < 0 || index >= _canvas.ControlPoints.Count) return;

            var current = _canvas.ControlPoints[index];
            ControlPoint point;
            string description = string.Format("Edite las coordenadas de P{0}. Cancelar conserva los valores actuales.", index);
            if (PointEditorForm.TryGetPoint(this, "Modificar punto P" + index, description, current, out point))
                _canvas.UpdateControlPoint(index, point);
        }

        private void DeleteSelectedPoint()
        {
            int index = _canvas.SelectedIndex;
            if (index < 0)
            {
                ShowSelectionRequired("Eliminar punto");
                return;
            }

            _canvas.RemoveControlPointAt(index);
        }

        private void ShowSelectionRequired(string caption)
        {
            string message = _canvas.ControlPoints.Count == 0
                ? "No existen puntos de control. Agregue uno con el botón «Agregar…» o con clic izquierdo sobre el lienzo."
                : "Seleccione primero un punto de la lista o haga clic sobre él en el lienzo.";
            MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
