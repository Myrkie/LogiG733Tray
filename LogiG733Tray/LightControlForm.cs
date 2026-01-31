using LogiG733Tray.API;
using LogiG733Tray.G733;
using LogiG733Tray.Utils;
using Serilog;

namespace LogiG733Tray
{
    public class LightControlForm : Form
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext<LightControlForm>();
        
        private readonly G733Device? _g733;
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
        private Label? _lblApiKey;
        private Label? _lblLocalIp;
        
        private readonly G733BatteryMonitor? _batteryMonitor;
        private readonly bool _apiEnabled;

        public LightControlForm(G733Device? g733, G733BatteryMonitor? batteryMonitor, bool apiEnabled)
        {
            if (g733 != null) _g733 = g733;
            _batteryMonitor = batteryMonitor;
            _apiEnabled = apiEnabled;
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
            Size = _apiEnabled ? new Size(380, 440) : new Size(380, 365);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = UiUtilities.Bg;

            _label = new Label
            {
                Text = $"Device: {_g733?.Name}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UiUtilities.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnUpperLight = UiUtilities.StyledButton("Set Upper LightBar");
            _btnLowerLight = UiUtilities.StyledButton("Set Lower LightBar");
            _btnBothLights = UiUtilities.StyledButton("Set Both LightBars");
            _btnLightsOff = UiUtilities.StyledButton("Turn Off LightBars");
            _btnBestColor = UiUtilities.StyledButton("Best Color");
            _btnGetPowerOff = UiUtilities.StyledButton("Get Auto Power-Off");
            _btnSetAutoPowerOff = UiUtilities.StyledButton("Set Auto Power-Off");

            var txtAutoPowerOffStyled = UiUtilities.StyledTextBox(out _txtAutoPowerOff, "Minutes (0 = Disabled)");

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
                ForeColor = UiUtilities.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblBatteryVoltage = new Label
            {
                Text = "Voltage: ---- mV",
                ForeColor = UiUtilities.Muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            _btnUpperLight.Click += (_, _) => SetLight(G733HidClient.LightTarget.Upper, G733HidClient.LightMode.Static);
            _btnLowerLight.Click += (_, _) => SetLight(G733HidClient.LightTarget.Lower, G733HidClient.LightMode.Static);
            _btnBothLights.Click += (_, _) => SetBothLights();
            _btnLightsOff.Click += (_, _) => _g733?.DisableLights();
            _btnBestColor.Click += (_, _) => _g733?.SetLights(G733HidClient.Colors.Purple, G733HidClient.Colors.Purple, G733HidClient.LightMode.Static);
            _btnGetPowerOff.Click += (_, _) => _txtAutoPowerOff.Text = _g733?.GetAutoPowerOff().ToString();
            _btnSetAutoPowerOff.Click += BtnSetAutoPowerOff_Click;

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = UiUtilities.Card
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

            var batteryPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true
            };
            batteryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            batteryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            batteryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            batteryPanel.Controls.Add(_batteryBar!, 0, 0);
            batteryPanel.SetColumnSpan(_batteryBar!, 2);

            batteryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            batteryPanel.Controls.Add(_lblBatteryPercent!, 0, 1);
            batteryPanel.Controls.Add(_lblBatteryVoltage!, 1, 1);

            table.Controls.Add(batteryPanel, 0, 8);
            table.SetColumnSpan(batteryPanel, 2);


            card.Controls.Add(table);
            
            Controls.Add(card);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(10, 6, 10, 6),
                BackColor = UiUtilities.Card
            };

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                AutoSize = true
            };

            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var apiHeader = new Label
            {
                Text = "API KEY",
                ForeColor = UiUtilities.Muted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            var hintLabel = new Label
            {
                Text = "<- CLICK TO COPY ->",
                ForeColor = UiUtilities.Muted,
                Font = new Font("Segoe UI", 7),
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            var ipHeader = new Label
            {
                Text = "LOCAL IP",
                ForeColor = UiUtilities.Muted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblApiKey = new Label
            {
                Text = ApiKeyGenerator.GetApiKey(),
                ForeColor = UiUtilities.Text,
                Font = new Font("Consolas", 8),
                AutoSize = true,
                MaximumSize = new Size(240, 0),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill
            };

            _lblLocalIp = new Label
            {
                Text = Utils.Utilities.GetLocalIp(),
                ForeColor = Utils.UiUtilities.Text,
                Font = new Font("Consolas", 8),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };

            footerLayout.Controls.Add(apiHeader, 0, 0);
            footerLayout.Controls.Add(hintLabel, 1, 0);
            footerLayout.Controls.Add(ipHeader, 2, 0);

            footerLayout.Controls.Add(_lblApiKey, 0, 1);
            footerLayout.Controls.Add(new Label(), 1, 1);
            footerLayout.Controls.Add(_lblLocalIp, 2, 1);

            footer.Controls.Add(footerLayout);
            if (_apiEnabled)
            {

                Controls.Add(footer);
                footer.BringToFront();
            }

            _lblApiKey.Click += (_, _) =>
            {
                Clipboard.SetText(_lblApiKey.Text);
                ShowCopiedHint(_lblApiKey);
            };

            _lblLocalIp.Click += (_, _) =>
            {
                Clipboard.SetText(_lblLocalIp.Text);
                ShowCopiedHint(_lblLocalIp);
            };
        }

        private void ShowCopiedHint(Control target)
        {
            var originalColor = target.ForeColor;
            target.ForeColor = Color.LimeGreen;

            var timer = new System.Windows.Forms.Timer { Interval = 800 };
            timer.Tick += (_, _) =>
            {
                target.ForeColor = originalColor;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
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

        private void SetLight(G733HidClient.LightTarget target, G733HidClient.LightMode mode)
        {
            var initialColor = new G733HidClient.RgbColor(255, 0, 0); // fallback initial color
            var picker = new ColorAndModePickerForm(initialColor, mode);
            if (picker.ShowDialog() != DialogResult.OK) return;

            var rgb = picker.SelectedColor;
            var selectedMode = picker.SelectedMode;

            switch (target)
            {
                case G733HidClient.LightTarget.Upper:
                    _g733?.SetUpperLightBar(rgb, selectedMode);
                    break;
                case G733HidClient.LightTarget.Lower:
                    _g733?.SetLowerLightBar(rgb, selectedMode);
                    break;
            }
        }


        private void SetBothLights()
        {
            var initialColor = new G733HidClient.RgbColor(255, 0, 0);
            var picker = new ColorAndModePickerForm(initialColor, G733HidClient.LightMode.Static);
            if (picker.ShowDialog() != DialogResult.OK) return;

            var rgb = picker.SelectedColor;
            var selectedMode = picker.SelectedMode;

            _g733?.SetLights(rgb, rgb, selectedMode);
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
