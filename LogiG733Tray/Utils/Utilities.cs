using System.Runtime.InteropServices;
using LogiG733Tray.G733;
using Serilog;

namespace LogiG733Tray.Utils
{
    public static partial class Utilities
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(Utilities));
        private const string AppName = "LogiG733Tray";
        // ReSharper disable once NotAccessedField.Local
        private static Mutex _mutex = null!;
        private const int LowBatteryThreshold = 15;
        private const int CriticalBatteryThreshold = 5;

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void DestroyIcon(IntPtr hIcon);
        
        internal static void SingleInstanceCheck()
        {
            Thread.Sleep(2000); // let's wait a bit to let any previous ones close before checking.
            _mutex = new Mutex(true, AppName, out var createdNew);
            if (createdNew) return;
            var str = AppName + " is already running.";
            Logger.Information(str);
            MessageBox.Show(str, AppName);
            Environment.Exit(0);
        }
        
        internal static void SetNotifyIcon(Icon newIcon)
        {
            var oldIcon = Program.NotifyIcon().Icon;
            Program.NotifyIcon().Icon = newIcon;
            oldIcon?.Dispose();
        }
        
        internal static Icon CreateIconFromBitmap(Bitmap bmp)
        {
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var tempIcon = Icon.FromHandle(hIcon);
                return (Icon)tempIcon.Clone(); 
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
        
        internal static Icon CreateBatteryIcon(int level, BatteryStatus status)
        {
            using var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                bool isLow = level is >= 0 and <= LowBatteryThreshold;
                bool isLowCritical = level is >= 0 and <= CriticalBatteryThreshold;
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
                else if (isLowCritical)
                {
                    using var warnPen = new Pen(Color.White, 2);
                    warnPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    warnPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    g.DrawLine(warnPen, 8, 3, 8, 9);
                    g.DrawLine(warnPen, 8, 11, 8, 12);
                }
            }
            return CreateIconFromBitmap(bmp);
        }
    }
}