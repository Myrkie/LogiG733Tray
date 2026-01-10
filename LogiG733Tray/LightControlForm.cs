using LogiG733Tray.G733;
using Serilog;

namespace LogiG733Tray
{
    public class LightControlForm : Form
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext<LightControlForm>();
        
        private readonly G733Device? _g733;
        private ColorDialog? _colorDialog;
        private Label? _label;
        private Button? _btnUpperLight;
        private Button? _btnLowerLight;
        private Button? _btnBothLights;
        private Button? _btnLightsOff;
        private Button? _btnBestColor;
        private Button? _btnGetPowerOff;
        private TextBox? _txtAutoPowerOff;
        private Button? _btnSetAutoPowerOff;
        private ProgressBar? _batteryBar;
        private Label? _lblBatteryPercent;
        private Label? _lblBatteryVoltage;
        private Panel? _offlineOverlay;

        private readonly G733BatteryMonitor? _batteryMonitor;

        public LightControlForm(G733Device? g733, G733BatteryMonitor? batteryMonitor)
        {
            if (g733 != null) _g733 = g733;
            _batteryMonitor = batteryMonitor;
            InitializeComponents();
            SetupOfflineOverlay();
            StartPosition = FormStartPosition.CenterScreen;
            _batteryMonitor?.BatteryUpdated += UpdateBatteryUi;
            _batteryMonitor?.RefreshNow();
            _g733?.ConnectionStateChanged += state =>
            {
                if (InvokeRequired)
                {
                    Invoke(() => UpdateConnectionStateUi(state));
                    return;
                }
                UpdateConnectionStateUi(state);
            };
            if (_g733 != null) UpdateConnectionStateUi(_g733.ConnectionState);
        }

        private void InitializeComponents()
        {
            Text = "LogiTray Control";
            Size = new Size(380, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Utils.UiUtilities.Bg;

            _colorDialog = new ColorDialog { FullOpen = true, AnyColor = true };

            _label = new Label
            {
                Text = $"Device: {_g733?.Name}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Utils.UiUtilities.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnUpperLight = Utils.UiUtilities.StyledButton("Set Upper LightBar");
            _btnLowerLight = Utils.UiUtilities.StyledButton("Set Lower LightBar");
            _btnBothLights = Utils.UiUtilities.StyledButton("Set Both LightBars");
            _btnLightsOff = Utils.UiUtilities.StyledButton("Turn Off LightBars");
            _btnBestColor = Utils.UiUtilities.StyledButton("Best Color");
            _btnGetPowerOff = Utils.UiUtilities.StyledButton("Get Auto Power-Off");
            _btnSetAutoPowerOff = Utils.UiUtilities.StyledButton("Set Auto Power-Off");

            var txtAutoPowerOffStyled = Utils.UiUtilities.StyledTextBox(out _txtAutoPowerOff, "Minutes (0 = Disabled)");

            _batteryBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Dock = DockStyle.Fill,
                Height = 18
            };

            _lblBatteryPercent = new Label
            {
                Text = "Battery: --%",
                ForeColor = Utils.UiUtilities.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblBatteryVoltage = new Label
            {
                Text = "Voltage: ---- mV",
                ForeColor = Utils.UiUtilities.Muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            _btnUpperLight.Click += (_, _) => SetLight(G733HidClient.LightTarget.Upper);
            _btnLowerLight.Click += (_, _) => SetLight(G733HidClient.LightTarget.Lower);
            _btnBothLights.Click += (_, _) => SetBothLights();
            _btnLightsOff.Click += (_, _) => _g733?.DisableLights();
            _btnBestColor.Click += (_, _) => _g733?.SetLights(G733HidClient.Colors.Purple, G733HidClient.Colors.Purple);
            _btnGetPowerOff.Click += (_, _) => _txtAutoPowerOff.Text = _g733?.GetAutoPowerOff().ToString();
            _btnSetAutoPowerOff.Click += BtnSetAutoPowerOff_Click;

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Utils.UiUtilities.Card
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                AutoSize = true
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < table.RowCount; i++)
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            table.Controls.Add(_label, 0, 0);
            table.SetColumnSpan(_label, 2);

            table.Controls.Add(_btnUpperLight, 0, 1);
            table.Controls.Add(_btnLowerLight, 1, 1);

            table.Controls.Add(_btnBothLights, 0, 2);
            table.SetColumnSpan(_btnBothLights, 2);

            table.Controls.Add(_btnLightsOff, 0, 3);
            table.SetColumnSpan(_btnLightsOff, 2);

            table.Controls.Add(_btnBestColor, 0, 4);
            table.SetColumnSpan(_btnBestColor, 2);

            table.Controls.Add(_btnGetPowerOff, 0, 5);
            table.SetColumnSpan(_btnGetPowerOff, 2);

            table.Controls.Add(txtAutoPowerOffStyled, 0, 6);
            table.Controls.Add(_btnSetAutoPowerOff, 1, 6);
            
            var batteryHeader = new Label
            {
                Text = "Battery Status",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Utils.UiUtilities.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            table.Controls.Add(batteryHeader, 0, 7);
            table.SetColumnSpan(batteryHeader, 2);

            table.Controls.Add(_batteryBar, 0, 8);
            table.SetColumnSpan(_batteryBar, 2);

            table.Controls.Add(_lblBatteryPercent, 0, 9);
            table.Controls.Add(_lblBatteryVoltage, 1, 9);

            card.Controls.Add(table);
            Controls.Add(card);
        }


        private void UpdateBatteryUi(BatteryInfo? battery)
        {
            if (battery is { Status: BatteryStatus.Unavailable })
            {
                _batteryBar!.Value = 0;
                _lblBatteryPercent!.Text = "Battery: N/A";
                _lblBatteryVoltage!.Text = "Voltage: N/A";
                return;
            }

            _batteryBar!.Value = Math.Clamp(battery!.Level, 0, 100);
            var status = battery.Status == BatteryStatus.Charging ? "Charging" : battery.Status.ToString();
            _lblBatteryPercent!.Text = $"Battery: {battery.Level}% ({status})";
            _lblBatteryVoltage!.Text = $"Voltage: {battery.VoltageMv} mV";
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
                _g733?.SetAutoPowerOff(minutes);
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
                    _g733?.SetUpperLightBar(rgb);
                    break;
                case G733HidClient.LightTarget.Lower:
                    _g733?.SetLowerLightBar(rgb);
                    break;
            }
        }

        private void SetBothLights()
        {
            if (_colorDialog != null && _colorDialog.ShowDialog() != DialogResult.OK) return;

            if (_colorDialog == null) return;
            var color = _colorDialog.Color;
            var rgb = new G733HidClient.RgbColor(color.R, color.G, color.B);

            _g733?.SetLights(rgb, rgb);
        }
        
        private void UpdateConnectionStateUi(DeviceConnectionState state)
        {
            switch (state)
            {
                case DeviceConnectionState.NoReceiver:
                case DeviceConnectionState.HeadsetSleeping:
                    _offlineOverlay!.Visible = true;
                    break;
                case DeviceConnectionState.ReceiverPresent:
                case DeviceConnectionState.HeadsetOnline:
                    _offlineOverlay!.Visible = false;
                    break;
            }
        }


        
        private void SetupOfflineOverlay()
        {
            _offlineOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(180, 0, 0, 0),
                Visible = false
            };

            var card = new Panel
            {
                Size = new Size(260, 140),
                BackColor = Color.FromArgb(245, 32, 32, 32),
                Anchor = AnchorStyles.None
            };

            _offlineOverlay.Resize += (_, _) =>
            {
                card.Left = (_offlineOverlay.Width - card.Width) / 2;
                card.Top = (_offlineOverlay.Height - card.Height) / 2;
            };

            var icon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI Emoji", 32),
                ForeColor = Color.Orange,
                Dock = DockStyle.Top,
                Height = 55,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var title = new Label
            {
                Text = "Receiver connected",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var subtitle = new Label
            {
                Text = "Headset is Asleep.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gainsboro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(10, 5, 10, 0)
            };

            card.Controls.Add(subtitle);
            card.Controls.Add(title);
            card.Controls.Add(icon);

            _offlineOverlay.Controls.Add(card);
            Controls.Add(_offlineOverlay);
            _offlineOverlay.BringToFront();
        }
    }
}
