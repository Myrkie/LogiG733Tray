using Serilog;
using Timer = System.Timers.Timer;

namespace LogiG733Tray.G733
{
    public class G733BatteryMonitor : IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<G733BatteryMonitor>();
        
        private readonly G733Device? _device;
        private readonly Timer _timer;
        private BatteryStatus? _lastStatus;
        
        public event Action<BatteryInfo?>? BatteryUpdated;
        public BatteryInfo? LatestBattery { get; private set; }
        public G733BatteryMonitor(G733Device? device, double intervalMs = 5000)
        {
            if (device != null) _device = device;

            _timer = new Timer(intervalMs);
            _timer.Elapsed += (_, _) => OnTimerElapsed();
            _timer.Start();
        }

        private void OnTimerElapsed()
        {
            try
            {
                var battery = _device?.GetBatteryInfo();
                if (_lastStatus != BatteryStatus.Charging && battery?.Status == BatteryStatus.Charging)
                {
                    Logger.Debug("Battery Status changed to Charging. Level: {level} | Voltage: {voltage}", battery.Level, battery.VoltageMv);
                }
                
                if (_lastStatus == BatteryStatus.Charging && battery?.Status != BatteryStatus.Charging)
                {
                    Logger.Debug("Battery Status changed to Discharging. Level: {level} | Voltage: {voltage}", battery?.Level, battery?.VoltageMv);
                }
                _lastStatus = battery?.Status;
                
                LatestBattery = battery;
                BatteryUpdated?.Invoke(battery);
            }
            catch
            {
                LatestBattery = new BatteryInfo
                {
                    Level = -1,
                    VoltageMv = -1,
                    Status = BatteryStatus.Unavailable
                };
                BatteryUpdated?.Invoke(new BatteryInfo
                {
                    Level = -1,
                    VoltageMv = -1,
                    Status = BatteryStatus.Unavailable
                });
            }
        }

        public void RefreshNow()
        {
            OnTimerElapsed();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}