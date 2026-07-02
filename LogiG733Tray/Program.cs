using System.Drawing.Drawing2D;
using LogiG733Tray.API;
using LogiG733Tray.G733;
using LogiG733Tray.Utils;
using LogiG733Tray.Win;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace LogiG733Tray
{
    internal static class Program
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext(typeof(Program));

        private static NotifyIcon _notifyIcon = new();
        private static LightControlForm? _lightControlForm;
        private static G733Device? _g733;
        private static G733BatteryMonitor? _batteryMonitor;
        private static ApiHost? _apiHost;
        private static WinMediaControl? _mediaControl;
        public static NotifyIcon NotifyIcon() => _notifyIcon;

        private static readonly bool ApiEnabled = Config.Instance.ApiConfig.InitializeApi;
        private static readonly bool MediaEnabled = Config.Instance.PwrButtonConfig.PwrPausesMedia;

        [STAThread]
        private static void Main()
        {
            var levelSwitch = new Serilog.Core.LoggingLevelSwitch
            {
                MinimumLevel = Config.Instance.DebugMode ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code)
                .WriteTo.File(
                    path: "logs/log-.txt",
                    outputTemplate:
                    "[{Timestamp:MM-dd-yyyy HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 4,
                    shared: true)
                .CreateLogger();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Utilities.SingleInstanceCheck();

            var contextMenu = new ContextMenuStrip();

            var deviceItem = new ToolStripMenuItem("Device: N/A");
            var batteryItem = new ToolStripMenuItem("Battery: N/A");
            var batteryItemMv = new ToolStripMenuItem("Battery Voltage: N/A");

            contextMenu.Items.Add(deviceItem);
            contextMenu.Items.Add(batteryItem);
            contextMenu.Items.Add(batteryItemMv);
            contextMenu.Items.Add(new ToolStripSeparator());

            var colorPickerItem = new ToolStripMenuItem(
                "Open Headset Config",
                null,
                (_, _) => ShowLightControlForm());

            var connectReceiverItem = new ToolStripMenuItem(
                "Connect to receiver",
                null,
                (_, _) => { AttachDevice(G733Device.TryConnectReceiver(_mediaControl, out var device) ? device : null); });

            contextMenu.Items.Add(colorPickerItem);
            contextMenu.Items.Add(connectReceiverItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (_, _) => Application.Exit());

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Visible = true,
                Text = "LogiTray Battery Monitor"
            };
            
            if (Config.Instance.PwrButtonConfig.PwrPausesMedia)
            { 
                _mediaControl = new WinMediaControl(); 
                _ = _mediaControl.InitializeAsync();
            }

            AttachDevice(G733Device.GetDevice(_mediaControl));

            Application.Run();
            return;
            
            void AttachDevice(G733Device? newDevice)
            {
                if (_batteryMonitor != null)
                {
                    _batteryMonitor.BatteryUpdated -= UpdateUi;
                    _batteryMonitor.Dispose();
                    _batteryMonitor = null;
                }

                if (_g733 != null)
                {
                    _g733.ConnectionStateChanged -= OnConnectionStateChanged;
                }

                _g733 = newDevice;

                if (_g733 == null)
                {
                    ShowNoDeviceState(deviceItem, batteryItem, batteryItemMv);
                    return;
                }

                _batteryMonitor = new G733BatteryMonitor(_g733);
                _batteryMonitor.BatteryUpdated += UpdateUi;

                if (ApiEnabled)
                {
                    if (_apiHost != null)
                    {
                        _apiHost.Dispose();
                        _apiHost = null;
                    }
                    _apiHost = new ApiHost(_g733, _batteryMonitor);
                    _apiHost.Start();
                }

                _g733.ConnectionStateChanged += OnConnectionStateChanged;

                _g733.UpdateConnectionState();
                _batteryMonitor.RefreshNow();
            }

            void OnConnectionStateChanged(DeviceConnectionState _)
            {
                UpdateUi(_g733?.GetBatteryInfo());
            }

            void UpdateUi(BatteryInfo? battery)
            {
                switch (_g733?.ConnectionState)
                {
                    case DeviceConnectionState.NoReceiver:
                        ShowNoDeviceState(deviceItem, batteryItem, batteryItemMv);
                        break;

                    case DeviceConnectionState.HeadsetSleeping:
                        deviceItem.Text = $"Device: {_g733.Name}";
                        ShowSleepingState(batteryItem, batteryItemMv);
                        break;

                    case DeviceConnectionState.ReceiverPresent:
                        deviceItem.Text = $"Device: {_g733.Name}";
                        batteryItem.Text = "Battery: N/A";
                        batteryItemMv.Text = "Battery Voltage: N/A";
                        break;

                    case DeviceConnectionState.HeadsetOnline:
                        if (battery != null)
                        {
                            deviceItem.Text = $"Device: {_g733.Name}";
                            batteryItem.Text =
                                $"Battery: {battery.Level}% ({(battery.Status == BatteryStatus.Charging ? "Charging" : battery.Status.ToString())})";
                            batteryItemMv.Text =
                                $"Battery Voltage: {battery.VoltageMv} mV";

                            _notifyIcon.Icon = Utilities.CreateBatteryIcon(
                                    battery.Level,
                                    battery.Status);
                        }
                        break;
                }
            }
        }

        private static void ShowSleepingState(ToolStripMenuItem batteryItem, ToolStripMenuItem batteryItemMv)
        {
            batteryItem.Text = "Battery: Device Asleep";
            batteryItemMv.Text = "Battery Voltage: Device Asleep";

            using var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);

            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var moonBrush = new SolidBrush(Color.Yellow);
            g.FillEllipse(moonBrush, 2, 2, 12, 12);

            using var eraseBrush = new SolidBrush(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillEllipse(eraseBrush, 6, 2, 8, 12);

            Utilities.SetNotifyIcon(Utilities.CreateIconFromBitmap(bmp));
        }

        private static void ShowNoDeviceState(ToolStripMenuItem deviceItem, ToolStripMenuItem batteryItem, ToolStripMenuItem batteryItemMv)
        {
            deviceItem.Text = "Device: Not detected";
            batteryItem.Text = "Battery: N/A";
            batteryItemMv.Text = "Battery Voltage: N/A";

            using var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);

            g.Clear(Color.Transparent);
            g.FillEllipse(Brushes.Gray, 0, 0, 15, 15);

            using var pen = new Pen(Color.White, 2);
            g.DrawLine(pen, 3, 3, 12, 12);
            g.DrawLine(pen, 12, 3, 3, 12);

            Utilities.SetNotifyIcon(Utilities.CreateIconFromBitmap(bmp));
        }

        private static void ShowLightControlForm()
        {
            if (_lightControlForm == null || _lightControlForm.IsDisposed)
            {
                _lightControlForm = new LightControlForm(_g733, _batteryMonitor, ApiEnabled, MediaEnabled, _mediaControl);
                _lightControlForm.Show();
            }
            else if (!_lightControlForm.Visible)
            {
                _lightControlForm.Show();
            }
            else
            {
                _lightControlForm.BringToFront();
                _lightControlForm.Activate();
            }
        }
    }
}
