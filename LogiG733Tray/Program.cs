using HidSharp;
using LogiG733Tray.G733;
using Serilog;

namespace LogiG733Tray
{
    internal static class Program
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext(typeof(Program));
        private static NotifyIcon _notifyIcon = new();
        private static readonly System.Windows.Forms.Timer UpdateTimer = new() { Interval = 5000 };
        private static LightControlForm? _lightControlForm;
        public static NotifyIcon NotifyIcon() { return _notifyIcon; }
        

        [STAThread]
        static void Main()
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
            Utilities.SingleInstanceCheck();
            
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
            
            var colorPickerItem = new ToolStripMenuItem("Open Headset Config", null, (_, _) =>
            {
                foreach (var device in DeviceList.Local.GetHidDevices())
                {
                    if (!G733Device.TryCreate(device, out var g733))
                        continue;
                    
                    var battery = g733!.GetBatteryInfo();

                    if (battery.Status == BatteryStatus.Unavailable)
                        continue;

                    ShowLightControlForm(g733);
                }
            
            });
            contextMenu.Items.Insert(3, colorPickerItem);
            
            UpdateTimer.Tick += (_, _) => UpdateBatteryInfo(deviceItem, batteryItem, batteryItemMv);
            UpdateTimer.Start();

            UpdateBatteryInfo(deviceItem, batteryItem, batteryItemMv);

            Application.Run();
        }

        private static void UpdateBatteryInfo(ToolStripMenuItem deviceItem, ToolStripMenuItem batteryItem, ToolStripMenuItem batteryItemMv)
        {
            bool deviceFound = false;

            foreach (var device in DeviceList.Local.GetHidDevices())
            {
                if (!G733Device.TryCreate(device, out var g733))
                    continue;
                deviceFound = true;

                var battery = g733!.GetBatteryInfo();
                
                if (battery.Status == BatteryStatus.Unavailable)
                    continue;

                deviceItem.Text = $"Device: {g733.Name}";
                
                var statusText = battery.Status == BatteryStatus.Charging ? "Charging" : battery.Status.ToString();
                batteryItem.Text = $"Battery: {battery.Level}% ({statusText})";
                
                batteryItemMv.Text = $"Battery Voltage: {battery.VoltageMv} mV";

                _notifyIcon.Icon = Utilities.CreateBatteryIcon(battery.Level, battery.Status);
                break;
            }

            if (!deviceFound)
            {
                ShowNoDeviceState(deviceItem, batteryItem, batteryItemMv);
            }
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

            Utilities.SetNotifyIcon(Utilities.CreateIconFromBitmap(bmp));
        }

        private static void ShowLightControlForm(G733Device g733)
        {
            if (_lightControlForm == null || _lightControlForm.IsDisposed)
            {
                _lightControlForm = new LightControlForm(g733);
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
