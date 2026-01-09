using Timer = System.Timers.Timer;

namespace LogiG733Tray.G733
{
    public class G733BatteryMonitor : IDisposable
    {
        private readonly G733Device? _device;
        private readonly Timer _timer;

        public event Action<BatteryInfo?>? BatteryUpdated;

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
                BatteryUpdated?.Invoke(battery);
            }
            catch
            {
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