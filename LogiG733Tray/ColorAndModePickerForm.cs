using LogiG733Tray.G733;
using LogiG733Tray.Utils;

namespace LogiG733Tray
{
    public class ColorAndModePickerForm : Form
    {
        public G733HidClient.RgbColor SelectedColor { get; private set; } = G733HidClient.Colors.Purple;
        public G733HidClient.LightMode SelectedMode { get; private set; }

        private ComboBox? _comboMode;
        private Button? _btnColor;
        private Panel? _colorPreview;
        private Button? _btnOk;
        private Button? _btnCancel;
        private ColorDialog? _colorDialog;

        public ColorAndModePickerForm(G733HidClient.LightMode initialMode)
        {
            SelectedMode = initialMode;

            InitializeComponents();
            UpdateColorControls();
        }

        private void InitializeComponents()
        {
            Text = "Pick Color and Mode";
            Size = new Size(360, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiUtilities.Bg;
            Padding = new Padding(12);

            _colorDialog = new ColorDialog { FullOpen = true, AnyColor = true };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiUtilities.Card,
                Padding = new Padding(12)
            };
            Controls.Add(card);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            card.Controls.Add(layout);

            _comboMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                BackColor = UiUtilities.Bg,
                ForeColor = UiUtilities.Text,
                Font = UiUtilities.SegoeUi9Standard,
                FlatStyle = FlatStyle.Flat
            };
            
            // ReSharper disable once CoVariantArrayConversion
            _comboMode.Items.AddRange(Enum.GetNames<G733HidClient.LightMode>());
            _comboMode.SelectedItem = SelectedMode.ToString();
            _comboMode.SelectedIndexChanged += (_, _) => UpdateColorControls();
            layout.Controls.Add(_comboMode, 0, 0);
            layout.SetColumnSpan(_comboMode, 2);

            _btnColor = UiUtilities.StyledButton("Select Color...");
            _btnColor.Click += BtnColor_Click;
            _btnColor.Dock = DockStyle.Fill;
            layout.Controls.Add(_btnColor, 0, 1);

            _colorPreview = new Panel
            {
                BackColor = Color.FromArgb(SelectedColor.R, SelectedColor.G, SelectedColor.B),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
            
            layout.Controls.Add(_colorPreview, 1, 1);

            _btnOk = UiUtilities.StyledButton("OK");
            _btnOk.DialogResult = DialogResult.OK;
            _btnOk.Click += BtnOk_Click;
            layout.Controls.Add(_btnOk, 0, 2);

            _btnCancel = UiUtilities.StyledButton("Cancel");
            _btnCancel.DialogResult = DialogResult.Cancel;
            layout.Controls.Add(_btnCancel, 1, 2);
        }

        private void BtnColor_Click(object? sender, EventArgs e)
        {
            if (_colorDialog?.ShowDialog() != DialogResult.OK) return;
            SelectedColor = new G733HidClient.RgbColor(_colorDialog.Color.R, _colorDialog.Color.G, _colorDialog.Color.B);
            _colorPreview!.BackColor = _colorDialog.Color;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_comboMode is { SelectedItem: not null } &&
                Enum.TryParse<G733HidClient.LightMode>(_comboMode.SelectedItem.ToString(), out var mode))
            {
                SelectedMode = mode;
            }
        }

        private void UpdateColorControls()
        {
            if (_comboMode?.SelectedItem == null ||
                !Enum.TryParse<G733HidClient.LightMode>(_comboMode.SelectedItem.ToString(), out var mode)) return;

            SelectedMode = mode;
            bool isCycle = mode == G733HidClient.LightMode.Cycle;
            _btnColor!.Enabled = !isCycle;
            _colorPreview!.Enabled = !isCycle;
            _btnColor.Text = isCycle ? "Color Disabled for Cycle" : "Select Color...";
            _colorPreview.BackColor = isCycle ? UiUtilities.Card : Color.FromArgb(SelectedColor.R, SelectedColor.G, SelectedColor.B);
        }
    }
}
