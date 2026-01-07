using HidSharp;
using LogiG733Tray.G733;

namespace LogiG733Tray
{
    internal static class Program
    {
        private static NotifyIcon _notifyIcon = new();
        private static ContextMenuStrip _contextMenu = new();
        private static readonly System.Windows.Forms.Timer UpdateTimer = new() { Interval = 5000 };
        private const int LowBatteryThreshold = 15;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Exit", null, (_, _) => Application.Exit());

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "Logi Battery Monitor"
            };
            var deviceItem = new ToolStripMenuItem("Device: N/A");
            _contextMenu.Items.Insert(0, deviceItem);
            
            var batteryItem = new ToolStripMenuItem("Battery: N/A");
            _contextMenu.Items.Insert(1, batteryItem);
            
            var batteryItemMv = new ToolStripMenuItem("Battery Voltage: N/A");
            _contextMenu.Items.Insert(2, batteryItemMv);
            
            var colorPickerItem = new ToolStripMenuItem("Open Color Picker", null, (_, _) =>
            {
                
                foreach (var device in DeviceList.Local.GetHidDevices())
                {
                    if (!G733Device.TryCreate(device, out var g733))
                        continue;
                    
                    var battery = g733!.GetBatteryInfo();

                    if (battery.Status == BatteryStatus.Unavailable)
                        continue;

                    var form = new LightControlForm(g733);
                    form.Show();
                }
            
            });
            _contextMenu.Items.Insert(3, colorPickerItem);
            
            UpdateTimer.Tick += (_, _) => UpdateBatteryInfo(deviceItem, batteryItem, batteryItemMv);
            UpdateTimer.Start();

            UpdateBatteryInfo(deviceItem, batteryItem, batteryItemMv);

            Application.Run();
        }

        private static void UpdateBatteryInfo(
            ToolStripMenuItem deviceItem,
            ToolStripMenuItem batteryItem,
            ToolStripMenuItem batteryItemMv)
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

                _notifyIcon.Icon = CreateBatteryIcon(battery.Level, battery.Status);
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

            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                g.FillEllipse(Brushes.Gray, 0, 0, 15, 15);

                using var pen = new Pen(Color.White, 2);
                g.DrawLine(pen, 3, 3, 12, 12);
                g.DrawLine(pen, 12, 3, 3, 12);
            }

            _notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
        }

        



        private static Icon CreateBatteryIcon(int level, BatteryStatus status)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                bool isLow = level is >= 0 and <= LowBatteryThreshold;
                bool isCharging = status == BatteryStatus.Charging;

                Brush fillBrush =
                    isCharging ? Brushes.LightBlue :
                    isLow ? Brushes.Red :
                    Brushes.LightGreen;

                int fillHeight = Math.Max(1, 16 * level / 100);

                g.FillRectangle(fillBrush, 0, 16 - fillHeight, 16, fillHeight);
                g.DrawRectangle(Pens.Black, 0, 0, 15, 15);


                if (isCharging)
                {
                    using var lightningPen = new Pen(Color.Yellow, 2);
                    Point[] bolt =
                    [
                        new(6, 2),
                        new(10, 2),
                        new(8, 8),
                        new(12, 8),
                        new(6, 14),
                        new(8, 10),
                        new(4, 10)
                    ];
                    g.DrawLines(lightningPen, bolt);
                }
                else if (isLow)
                {
                    using var warnPen = new Pen(Color.White, 2);
                    warnPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    warnPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    g.DrawLine(warnPen, 8, 3, 8, 9);

                    g.DrawLine(warnPen, 8, 11, 8, 12);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }
}
