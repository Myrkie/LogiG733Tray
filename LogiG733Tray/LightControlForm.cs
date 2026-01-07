using LogiG733Tray.G733;

namespace LogiG733Tray
{
    public class LightControlForm : Form
    {
        private readonly G733Device _g733;
        private ColorDialog? _colorDialog;
        private Button? _btnUpper;
        private Button? _btnLower;
        private Button? _btnBoth;
        private Button? _btnOff;
        private Button? _btnBestColor;
        private Button? _btnGetPowerOff;
        private TextBox? _txtAutoPowerOff;
        private Button? _btnSetAutoPowerOff;

        public LightControlForm(G733Device g733)
        {
            _g733 = g733;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "G733 Control";
            Size = new Size(350, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            _colorDialog = new ColorDialog { FullOpen = true, AnyColor = true };

            _btnUpper = new Button { Text = "Set Upper Bar", Dock = DockStyle.Fill };
            _btnLower = new Button { Text = "Set Lower Bar", Dock = DockStyle.Fill };
            _btnBoth = new Button { Text = "Set Both Bars", Dock = DockStyle.Fill };
            _btnOff = new Button { Text = "Turn Off Lights", Dock = DockStyle.Fill };
            _btnBestColor = new Button { Text = "Best Color", Dock = DockStyle.Fill };
            _btnGetPowerOff = new Button { Text = "Get Auto Poweroff time", Dock = DockStyle.Fill };
            _btnSetAutoPowerOff = new Button { Text = "Set Auto Power-Off", Dock = DockStyle.Fill };

            _txtAutoPowerOff = new TextBox { PlaceholderText = "Minutes (0 = Disabled)", Dock = DockStyle.Fill };

            _btnUpper.Click += (_, _) => SetLight(G733HidClient.LightTarget.Upper);
            _btnLower.Click += (_, _) => SetLight(G733HidClient.LightTarget.Lower);
            _btnBoth.Click += (_, _) => SetBothLights();
            _btnOff.Click += (_, _) => _g733.DisableLights();
            _btnBestColor.Click += (_, _) => _g733.SetLights(G733HidClient.Colors.Purple, G733HidClient.Colors.Purple);
            _btnGetPowerOff.Click += (_, _) => _txtAutoPowerOff.Text = _g733.GetAutoPowerOff().ToString();
            _btnSetAutoPowerOff.Click += BtnSetAutoPowerOff_Click;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                Padding = new Padding(10),
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Upper + Lower
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Set Both Bars
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Turn off Lights
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Best Color
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Get auto poweroff
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Minutes text + Set auto power off

            table.Controls.Add(_btnUpper, 0, 0);
            table.Controls.Add(_btnLower, 1, 0);
            table.Controls.Add(_btnBoth, 0, 1);
            table.SetColumnSpan(_btnBoth, 2); 
            table.Controls.Add(_btnOff, 0, 2);
            table.SetColumnSpan(_btnOff, 2);
            table.Controls.Add(_btnBestColor, 0, 3);
            table.SetColumnSpan(_btnBestColor, 2);
            table.Controls.Add(_btnGetPowerOff, 0, 4);
            table.SetColumnSpan(_btnGetPowerOff, 2);
            table.Controls.Add(_txtAutoPowerOff, 0, 5);
            table.Controls.Add(_btnSetAutoPowerOff, 1, 5);

            Controls.Add(table);
        }

        
        private void BtnSetAutoPowerOff_Click(object? sender, EventArgs e)
        {
            if (_txtAutoPowerOff == null) return;

            if (!int.TryParse(_txtAutoPowerOff.Text, out int minutes))
            {
                MessageBox.Show("Please enter a valid number of minutes (0 = Never).", "Invalid Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                _g733.SetAutoPowerOff(minutes);
                MessageBox.Show($"Auto Power-Off set to {minutes} minute(s).", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set Auto Power-Off: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetLight(G733HidClient.LightTarget target)
        {
            if (_colorDialog != null && _colorDialog.ShowDialog() != DialogResult.OK) return;

            if (_colorDialog == null) return;
            var color = _colorDialog.Color;
            var rgb = new G733HidClient.RgbColor(color.R, color.G, color.B);

            switch (target)
            {
                case G733HidClient.LightTarget.Upper:
                    _g733.SetUpperLightBar(rgb);
                    break;
                case G733HidClient.LightTarget.Lower:
                    _g733.SetLowerLightBar(rgb);
                    break;
            }
        }

        private void SetBothLights()
        {
            if (_colorDialog != null && _colorDialog.ShowDialog() != DialogResult.OK) return;

            if (_colorDialog == null) return;
            var color = _colorDialog.Color;
            var rgb = new G733HidClient.RgbColor(color.R, color.G, color.B);

            _g733.SetLights(rgb, rgb);
        }
    }
}
