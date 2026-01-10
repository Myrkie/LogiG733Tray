using HidSharp;
using Serilog;

namespace LogiG733Tray.G733
{
    public class G733Device
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext<G733Device>();
        
        private const int VendorId = 0x046D; // Logi
        private static readonly int[] SupportedProductIDs = [0x0afe, 0x0ab5, 0x0b1f, 0x0a5b];
        private static G733Device? _cachedDevice;
        private readonly G733HidClient _hid;
        public DeviceConnectionState ConnectionState { get; private set; } = DeviceConnectionState.NoReceiver;
        public event Action<DeviceConnectionState>? ConnectionStateChanged;

        public string Name { get; }

        private G733Device(HidDevice? device)
        {
            _hid = new G733HidClient(device!);
            _hid.OnlineStateChanged += _ => UpdateConnectionState();
            Name = device!.GetProductName(GetStringFlags.None);
        }
        
        private void SetConnectionState(DeviceConnectionState newState)
        {
            if (ConnectionState == newState) return;
            ConnectionState = newState;
            ConnectionStateChanged?.Invoke(newState);
        }
        
        private static bool IsReceiverPresent()
        {
            return DeviceList.Local.GetHidDevices()
                .Any(IsSupported);
        }
        
        private static bool IsSupported(HidDevice device)
        {
            return device.VendorID == VendorId &&
                   SupportedProductIDs.Contains(device.ProductID);
        }

        private static bool TryCreate(HidDevice device, out G733Device? g733)
        {
            if (!IsSupported(device))
            {
                g733 = null;
                return false;
            }

            g733 = new G733Device(device);
            return true;
        }
        
        public static G733Device? GetDevice()
        {
            if (_cachedDevice != null)
            {
                var battery = _cachedDevice.GetBatteryInfo();

                if (battery.Status is not BatteryStatus.Unavailable and not BatteryStatus.Timeout)
                    return _cachedDevice;

                _cachedDevice = null;
            }

            foreach (var device in DeviceList.Local.GetHidDevices())
            {
                if (!TryCreate(device, out var g733)) continue;

                var battery = g733!.GetBatteryInfo();

                if (battery.Status == BatteryStatus.Unavailable) 
                    continue;

                _cachedDevice = g733;
                return _cachedDevice;
            }

            return null;
        }

        public static bool TryConnectReceiver(out G733Device? device)
        {
            device = null;

            foreach (var hid in DeviceList.Local.GetHidDevices())
            {
                if (!IsSupported(hid))
                    continue;

                if (!TryCreate(hid, out var g733))
                    continue;

                var battery = g733!.GetBatteryInfo();
                if (battery.Status == BatteryStatus.Unavailable)
                    continue;

                _cachedDevice = g733;
                g733.UpdateConnectionState();

                device = g733;
                return true;
            }

            return false;
        }

        
        public void UpdateConnectionState()
        {
            if (!IsReceiverPresent())
            {
                SetConnectionState(DeviceConnectionState.NoReceiver);
                return;
            }

            var battery = GetBatteryInfo();

            switch (battery.Status)
            {
                case BatteryStatus.Unavailable:
                    SetConnectionState(DeviceConnectionState.ReceiverPresent);
                    return;
                case BatteryStatus.Timeout:
                    SetConnectionState(DeviceConnectionState.HeadsetSleeping);
                    return;
                default:
                    SetConnectionState(DeviceConnectionState.HeadsetOnline);
                    break;
            }
        }


        
        /// <summary>
        /// Set the upper light bar color.
        /// Pass null to disable the bar.
        /// </summary>
        public void SetUpperLightBar(G733HidClient.RgbColor? color)
        {
            _hid.SetLight(G733HidClient.LightTarget.Upper, color);
        }

        /// <summary>
        /// Set the lower light bar color.
        /// Pass null to disable the bar.
        /// </summary>
        public void SetLowerLightBar(G733HidClient.RgbColor? color)
        {
            _hid.SetLight(G733HidClient.LightTarget.Lower, color);
        }

        /// <summary>
        /// Set upper and lower light bars at once.
        /// Pass null to disable a bar.
        /// </summary>
        public void SetLights(G733HidClient.RgbColor? mainColor, G733HidClient.RgbColor? logoColor)
        {
            _hid.SetLights(mainColor, logoColor);
        }
        
        public int GetAutoPowerOff()
        {
            return _hid.GetAutoPowerOff();
        }

        public void SetAutoPowerOff(int minutes)
        {
            _hid.SetAutoPowerOff(minutes);
        }

        /// <summary>
        /// Turn off all lights.
        /// </summary>
        public void DisableLights()
        {
            _hid.DisableLights();
        }

        public BatteryInfo GetBatteryInfo()
        {
            try
            {
                var response = _hid.SendBatteryRequest();
                if (response.Length == 0)
                    return new BatteryInfo { Status = BatteryStatus.Timeout };

                ushort voltage = (ushort)((response[4] << 8) | response[5]);
                byte state = response[6];

                return new BatteryInfo
                {
                    Status = state == 0x03
                        ? BatteryStatus.Charging
                        : BatteryStatus.Detected,
                    Level = MapVoltageToPercent(voltage),
                    VoltageMv = voltage
                };
            }
            catch
            {
                return new BatteryInfo { Status = BatteryStatus.Unavailable };
            }
        }

        /// <summary>
        /// Map voltage in mV to percentage
        /// </summary>
        /// <param name="voltage"></param>
        /// <returns></returns>
        private static int MapVoltageToPercent(ushort voltage)
        {
            // my device arrived with a degraded battery so im using degraded values.
            // I don't want to have to make a battery map I am not at all experience with battery technology.
            // I also don't want to have to continue to have the awful Ghub software remaining on my machine to continue reverse engineering.
            const int max = 4200;
            const int min = 3500;

            switch (voltage)
            {
                case >= max:
                    return 100;
                case <= min:
                    return 0;
            }

            double percent = (double)(voltage - min) / (max - min);
            percent = Math.Pow(percent, 1.7);
            return (int)(percent * 100);
        }
    }
    public enum BatteryStatus
    {
        Unavailable,
        Detected,
        Charging,
        Timeout
    }
    public enum DeviceConnectionState
    {
        NoReceiver,
        ReceiverPresent,
        HeadsetSleeping,
        HeadsetOnline
    }

    public class BatteryInfo
    {
        public BatteryStatus Status { get; init; }
        public int Level { get; init; } = -1;
        public int VoltageMv { get; init; } = -1;
    }
}