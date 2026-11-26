using System;
using System.Drawing;
using System.Windows.Forms;
using BezierCore;

namespace BezierApp.UI
{
    /// <summary>
    /// Formulario modal para ingresar o editar las coordenadas de un punto de
    /// control. Se usa tanto para agregar como para modificar: sólo cambia el
    /// título y los valores iniciales. Si el usuario cancela, no se modifica nada.
    /// </summary>
    public sealed class PointEditorForm : Form
    {
        private readonly TextBox _xInput = new TextBox();
        private readonly TextBox _yInput = new TextBox();
        private readonly Label _messageLabel = new Label();
        private readonly ErrorProvider _errors = new ErrorProvider();

        public PointEditorForm(string title, string description, ControlPoint initial)
        {
            Text = title;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(340, 246);
            BackColor = UiTheme.PanelBackground;

            _errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            _errors.ContainerControl = this;

            BuildLayout(description);

            _xInput.Text = CoordinateValidator.Format(initial.X);
            _yInput.Text = CoordinateValidator.Format(initial.Y);
        }

        /// <summary>Punto resultante; sólo es válido si ShowDialog devolvió OK.</summary>
        public ControlPoint Result { get; private set; }

        /// <summary>
        /// Muestra el formulario y devuelve true si el usuario aceptó coordenadas válidas.
        /// </summary>
        public static bool TryGetPoint(IWin32Window owner, string title, string description,
            ControlPoint initial, out ControlPoint result)
        {
            using (var form = new PointEditorForm(title, description, initial))
            {
                bool accepted = form.ShowDialog(owner) == DialogResult.OK;
                result = accepted ? form.Result : initial;
                return accepted;
            }
        }

        private void BuildLayout(string description)
        {
            var descriptionLabel = new Label
            {
                Text = description,
                Bounds = new Rectangle(16, 12, 308, 34),
                ForeColor = UiTheme.MutedText
            };
            Controls.Add(descriptionLabel);

            AddField("Coordenada X:", _xInput, 54);
            AddField("Coordenada Y:", _yInput, 88);

            _messageLabel.Bounds = new Rectangle(16, 120, 308, 76);
            _messageLabel.ForeColor = Color.FromArgb(185, 28, 28);
            Controls.Add(_messageLabel);

            var accept = new Button { Text = "Aceptar", Bounds = new Rectangle(148, 204, 85, 30) };
            var cancel = new Button { Text = "Cancelar", Bounds = new Rectangle(239, 204, 85, 30), DialogResult = DialogResult.Cancel };
            accept.Click += (s, e) => AcceptIfValid();
            Controls.Add(accept);
            Controls.Add(cancel);

            AcceptButton = accept;
            CancelButton = cancel;
        }

        private void AddField(string caption, TextBox input, int y)
        {
            Controls.Add(new Label { Text = caption, Bounds = new Rectangle(16, y + 3, 100, 20) });
            input.Bounds = new Rectangle(120, y, 180, 24);
            input.TextChanged += (s, e) => _errors.SetError(input, string.Empty);
            Controls.Add(input);
        }

        /// <summary>
        /// Valida ambos campos; muestra todos los errores a la vez y deja el foco en
        /// el primer campo incorrecto. Sólo cierra el formulario si todo es válido.
        /// </summary>
        private void AcceptIfValid()
        {
            double x, y;
            string xError, yError;
            bool xValid = CoordinateValidator.TryParse(_xInput.Text, "X", out x, out xError);
            bool yValid = CoordinateValidator.TryParse(_yInput.Text, "Y", out y, out yError);

            _errors.SetError(_xInput, xError ?? string.Empty);
            _errors.SetError(_yInput, yError ?? string.Empty);

            if (!xValid || !yValid)
            {
                _messageLabel.Text = xValid ? yError : (yValid ? xError : xError + Environment.NewLine + yError);
                TextBox firstInvalid = xValid ? _yInput : _xInput;
                firstInvalid.Focus();
                firstInvalid.SelectAll();
                return;
            }

            Result = new ControlPoint(x, y);
            DialogResult = DialogResult.OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _errors.Dispose();
            base.Dispose(disposing);
        }
    }
}
