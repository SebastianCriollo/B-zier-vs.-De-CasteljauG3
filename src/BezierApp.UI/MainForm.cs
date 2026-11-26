using System;
using System.Drawing;
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
        private readonly CurveCanvas _canvas = new CurveCanvas();
        private readonly ListBox _pointsList = new ListBox();
        private readonly NumericUpDown _samplesInput = new NumericUpDown();
        private readonly TrackBar _tSlider = new TrackBar();
        private readonly Label _degreeLabel = new Label();
        private readonly Label _tLabel = new Label();
        private readonly CheckBox _showBernstein = new CheckBox();
        private readonly CheckBox _showDeCasteljau = new CheckBox();
        private readonly CheckBox _showSteps = new CheckBox();

        public MainForm()
        {
            Text = "Curvas de Bézier — Bernstein vs. De Casteljau";
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            WireEvents();
            LoadSampleCubicCurve();
            RefreshPointsList();
        }

        private void BuildLayout()
        {
            var sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                Padding = new Padding(12),
                BackColor = SystemColors.Control
            };

            _canvas.Dock = DockStyle.Fill;

            int y = 12;

            sidePanel.Controls.Add(MakeSectionLabel("Puntos de control", ref y));

            _pointsList.SetBounds(12, y, 264, 160);
            sidePanel.Controls.Add(_pointsList);
            y += 170;

            var addButton = MakeButton("Agregar…", 12, y, 84);
            var editButton = MakeButton("Modificar…", 102, y, 84);
            var deleteButton = MakeButton("Eliminar", 192, y, 84);
            addButton.Click += (s, e) => PromptAddPoint();
            editButton.Click += (s, e) => PromptEditSelectedPoint();
            deleteButton.Click += (s, e) => DeleteSelectedPoint();
            sidePanel.Controls.Add(addButton);
            sidePanel.Controls.Add(editButton);
            sidePanel.Controls.Add(deleteButton);
            y += 36;

            var clearButton = MakeButton("Limpiar todo", 12, y, 264);
            clearButton.Click += (s, e) => { _canvas.ClearControlPoints(); RefreshPointsList(); };
            sidePanel.Controls.Add(clearButton);
            y += 44;

            _degreeLabel.SetBounds(12, y, 264, 20);
            _degreeLabel.Font = new Font(Font, FontStyle.Bold);
            sidePanel.Controls.Add(_degreeLabel);
            y += 32;

            sidePanel.Controls.Add(MakeSectionLabel("Cantidad de muestras", ref y));
            _samplesInput.SetBounds(12, y, 264, 24);
            _samplesInput.Minimum = 2;
            _samplesInput.Maximum = 100000;
            _samplesInput.Value = 100;
            _samplesInput.Increment = 10;
            sidePanel.Controls.Add(_samplesInput);
            y += 40;

            sidePanel.Controls.Add(MakeSectionLabel("Visualización", ref y));

            _showBernstein.SetBounds(12, y, 264, 22);
            _showBernstein.Text = "Curva por Bernstein (directo)";
            _showBernstein.Checked = true;
            sidePanel.Controls.Add(_showBernstein);
            y += 26;

            _showDeCasteljau.SetBounds(12, y, 264, 22);
            _showDeCasteljau.Text = "Curva por De Casteljau";
            _showDeCasteljau.Checked = true;
            sidePanel.Controls.Add(_showDeCasteljau);
            y += 26;

            _showSteps.SetBounds(12, y, 264, 22);
            _showSteps.Text = "Interpolaciones de De Casteljau";
            sidePanel.Controls.Add(_showSteps);
            y += 32;

            _tLabel.SetBounds(12, y, 264, 20);
            sidePanel.Controls.Add(_tLabel);
            y += 22;

            _tSlider.SetBounds(12, y, 264, 40);
            _tSlider.Minimum = 0;
            _tSlider.Maximum = 1000;
            _tSlider.Value = 500;
            _tSlider.TickFrequency = 100;
            sidePanel.Controls.Add(_tSlider);

            // El orden importa: el panel acoplado a la derecha se agrega primero y el
            // lienzo con Dock=Fill al final, para que éste ocupe sólo el espacio restante.
            Controls.Add(sidePanel);
            Controls.Add(_canvas);
        }

        private Label MakeSectionLabel(string text, ref int y)
        {
            var label = new Label
            {
                Text = text,
                Bounds = new Rectangle(12, y, 264, 20),
                Font = new Font(Font, FontStyle.Bold)
            };
            y += 24;
            return label;
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Bounds = new Rectangle(x, y, width, 28)
            };
        }

        private void WireEvents()
        {
            _canvas.ControlPointsChanged += (s, e) => RefreshPointsList();

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

            _showSteps.CheckedChanged += (s, e) =>
            {
                _canvas.ShowInterpolationSteps = _showSteps.Checked;
                _canvas.Invalidate();
            };

            _tSlider.ValueChanged += (s, e) =>
            {
                _canvas.InterpolationT = _tSlider.Value / 1000.0;
                UpdateTLabel();
                _canvas.Invalidate();
            };

            _pointsList.SelectedIndexChanged += (s, e) => _canvas.Invalidate();
        }

        /// <summary>Curva cúbica inicial, útil para contrastar con los cálculos manuales del informe.</summary>
        private void LoadSampleCubicCurve()
        {
            _canvas.ControlPoints.Add(new ControlPoint(120, 480));
            _canvas.ControlPoints.Add(new ControlPoint(260, 140));
            _canvas.ControlPoints.Add(new ControlPoint(520, 140));
            _canvas.ControlPoints.Add(new ControlPoint(660, 480));

            _canvas.SampleCount = (int)_samplesInput.Value;
            _canvas.InterpolationT = _tSlider.Value / 1000.0;
        }

        private void RefreshPointsList()
        {
            int selected = _pointsList.SelectedIndex;

            _pointsList.BeginUpdate();
            _pointsList.Items.Clear();
            for (int i = 0; i < _canvas.ControlPoints.Count; i++)
            {
                var p = _canvas.ControlPoints[i];
                _pointsList.Items.Add(string.Format("P{0} = ({1:F1}, {2:F1})", i, p.X, p.Y));
            }
            _pointsList.EndUpdate();

            if (selected >= 0 && selected < _pointsList.Items.Count)
                _pointsList.SelectedIndex = selected;

            int degree = Math.Max(_canvas.ControlPoints.Count - 1, 0);
            _degreeLabel.Text = string.Format("Grado de la curva: {0}  ({1} puntos)", degree, _canvas.ControlPoints.Count);
            UpdateTLabel();
        }

        private void UpdateTLabel()
        {
            _tLabel.Text = string.Format("t = {0:F3}", _canvas.InterpolationT);
        }

        // TODO (Damian12T): reemplazar estos diálogos por un formulario propio
        //   (p. ej. PointEditorForm) con validación numérica.
        private void PromptAddPoint()
        {
            ControlPoint point;
            if (TryPromptCoordinates("Agregar punto de control", 0, 0, out point))
            {
                _canvas.AddControlPoint(point);
                RefreshPointsList();
            }
        }

        private void PromptEditSelectedPoint()
        {
            int index = _pointsList.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("Seleccione primero un punto de la lista.", "Modificar punto",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var current = _canvas.ControlPoints[index];
            ControlPoint point;
            if (TryPromptCoordinates("Modificar P" + index, current.X, current.Y, out point))
            {
                _canvas.UpdateControlPoint(index, point);
                RefreshPointsList();
            }
        }

        private void DeleteSelectedPoint()
        {
            int index = _pointsList.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("Seleccione primero un punto de la lista.", "Eliminar punto",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _canvas.RemoveControlPointAt(index);
            RefreshPointsList();
        }

        /// <summary>
        /// Diálogo mínimo para capturar coordenadas. Pendiente de mejorar (ver TODO).
        /// </summary>
        private bool TryPromptCoordinates(string title, double defaultX, double defaultY, out ControlPoint point)
        {
            using (var dialog = new Form())
            {
                dialog.Text = title;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(260, 130);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;

                var xInput = new NumericUpDown
                {
                    Bounds = new Rectangle(60, 16, 180, 24),
                    Minimum = -100000,
                    Maximum = 100000,
                    DecimalPlaces = 2,
                    Value = (decimal)defaultX
                };
                var yInput = new NumericUpDown
                {
                    Bounds = new Rectangle(60, 48, 180, 24),
                    Minimum = -100000,
                    Maximum = 100000,
                    DecimalPlaces = 2,
                    Value = (decimal)defaultY
                };
                var ok = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Bounds = new Rectangle(60, 88, 85, 28) };
                var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(155, 88, 85, 28) };

                dialog.Controls.Add(new Label { Text = "X:", Bounds = new Rectangle(16, 20, 40, 20) });
                dialog.Controls.Add(xInput);
                dialog.Controls.Add(new Label { Text = "Y:", Bounds = new Rectangle(16, 52, 40, 20) });
                dialog.Controls.Add(yInput);
                dialog.Controls.Add(ok);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    point = new ControlPoint((double)xInput.Value, (double)yInput.Value);
                    return true;
                }
            }

            point = default(ControlPoint);
            return false;
        }
    }
}
