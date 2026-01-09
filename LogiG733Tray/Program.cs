using LogiG733Tray.G733;
using Serilog;

namespace LogiG733Tray
{
    internal static class Program
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext(typeof(Program));
        private static NotifyIcon _notifyIcon = new();
        private static LightControlForm? _lightControlForm;
        public static NotifyIcon NotifyIcon() { return _notifyIcon; }
        

        [STAThread]
        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
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
            Utils.Utilities.SingleInstanceCheck();
            
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (_, _) => Application.Exit());

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Visible = true,
                Text = "LogiTray Battery Monitor"
            };
            
            var deviceItem = new ToolStripMenuItem("Device: N/A");
            contextMenu.Items.Insert(0, deviceItem);
            
            var batteryItem = new ToolStripMenuItem("Battery: N/A");
            contextMenu.Items.Insert(1, batteryItem);
            
            var batteryItemMv = new ToolStripMenuItem("Battery Voltage: N/A");
            contextMenu.Items.Insert(2, batteryItemMv);
            
            var g733 = G733Device.GetDevice();
            if (g733 == null)
            {
                ShowNoDeviceState(deviceItem, batteryItem, batteryItemMv);
            }
            
            var batteryMonitor = new G733BatteryMonitor(g733);
            
            batteryMonitor.BatteryUpdated += battery =>
            {
                if (battery is { Status: BatteryStatus.Timeout } or { Status: BatteryStatus.Unavailable })
                {
                    deviceItem.Text = $"Device: {g733?.Name}";
                    ShowSleepingState(batteryItem, batteryItemMv);
                    return;
                }
                
                deviceItem.Text = $"Device: {g733?.Name}";
                batteryItem.Text = $"Battery: {battery!.Level}% ({(battery.Status == BatteryStatus.Charging ? "Charging" : battery.Status.ToString())})";
                batteryItemMv.Text = $"Battery Voltage: {battery.VoltageMv} mV";
                _notifyIcon.Icon = Utils.Utilities.CreateBatteryIcon(battery.Level, battery.Status);
            };
            
            g733?.AvailabilityChanged += available =>
            {
                if (available) return;
                Logger.Information("headset is offline showing sleep state");
                ShowSleepingState(batteryItem, batteryItemMv);
            };
            
            var colorPickerItem = new ToolStripMenuItem("Open Headset Config", null, (_, _) =>
            {
                ShowLightControlForm(g733, batteryMonitor);
            });
            contextMenu.Items.Insert(3, colorPickerItem);
            
            batteryMonitor.RefreshNow();

            Application.Run();
        }
        
        
        private static void ShowSleepingState(ToolStripMenuItem batteryItem, ToolStripMenuItem batteryItemMv)
        {
            batteryItem.Text = "Battery: Device Asleep";
            batteryItemMv.Text = "Battery Voltage: Device Asleep";

            using var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using var moonBrush = new SolidBrush(Color.Yellow);
                g.FillEllipse(moonBrush, 2, 2, 12, 12);
                using var eraseBrush = new SolidBrush(Color.Transparent);
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.FillEllipse(eraseBrush, 6, 2, 8, 12); // "cut out" crescent
            }

            Utils.Utilities.SetNotifyIcon(Utils.Utilities.CreateIconFromBitmap(bmp));
        }


        
        private static void ShowNoDeviceState(
            ToolStripMenuItem deviceItem,
            ToolStripMenuItem batteryItem,
            ToolStripMenuItem batteryItemMv)
        {
            deviceItem.Text = "Device: Not detected";
            batteryItem.Text = "Battery: N/A";
            batteryItemMv.Text = "Battery Voltage: N/A";

            using var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Gray, 0, 0, 15, 15);

                using var pen = new Pen(Color.White, 2);
                g.DrawLine(pen, 3, 3, 12, 12);
                g.DrawLine(pen, 12, 3, 3, 12);
            }

            Utils.Utilities.SetNotifyIcon(Utils.Utilities.CreateIconFromBitmap(bmp));
        }

        private static void ShowLightControlForm(G733Device? g733, G733BatteryMonitor batteryMonitor)
        {
            if (_lightControlForm == null || _lightControlForm.IsDisposed)
            {
                _lightControlForm = new LightControlForm(g733, batteryMonitor);
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
