using HidSharp;
using LogiG733Tray.G733.Controllers;
using LogiG733Tray.Utils;
using LogiG733Tray.Win;
using Serilog;

namespace LogiG733Tray.G733
{
    public class G733Device
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Log.ForContext<G733Device>();
        
        private const int VendorId = 0x046D; // Logi
        private static readonly int[] SupportedProductIDs = [0x0afe, 0x0ab5, 0x0b1f];
        private static G733Device? _cachedDevice;
        private readonly G733HidClient _hid;

        public DeviceConnectionState ConnectionState { get; private set; } = DeviceConnectionState.NoReceiver;
        public event Action<DeviceConnectionState>? ConnectionStateChanged;
        public string Name { get; }

        private G733Device(HidDevice? device, WinMediaControl? media)
        {
            _hid = new G733HidClient(device!);
            _hid.OnlineStateChanged += _ => UpdateConnectionState();
            
            if (Config.Instance.PwrButtonConfig.PwrPausesMedia)
            {
                var powerButtonController = new G733PowerButtonController(media!, Logger);

                _hid.PowerButtonPressed += powerButtonController.OnButtonPressed;

                _hid.StartListening();
            }
            
            Name = device!.GetProductName(GetStringFlags.None);
        }
        
        private void SetConnectionState(DeviceConnectionState newState)
        {
            if (ConnectionState == newState) return;
            ConnectionState = newState;
            ConnectionStateChanged?.Invoke(newState);
        }
        
        private static bool IsSupported(HidDevice device)
        {
            bool match = device.VendorID == VendorId && Enumerable.Contains(SupportedProductIDs, device.ProductID);
            return match && device.DevicePath.Contains("col02", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCreate(HidDevice hidDevice, WinMediaControl? media, out G733Device? g733)
        {
            if (!IsSupported(hidDevice))
            {
                g733 = null;
                return false;
            }
            
            g733 = new G733Device(hidDevice, media);
            return true;
        }
        
        public static G733Device? GetDevice(WinMediaControl? media)
        {
            if (_cachedDevice != null)
                return _cachedDevice;

            var devices = DeviceList.Local.GetHidDevices().ToList();

            Logger.Debug("Found {Count} HID devices", devices.Count);

            foreach (var d in devices)
            {
                Logger.Debug("HID: VID={VID:X4} PID={PID:X4} Path={Path}",
                    d.VendorID,
                    d.ProductID,
                    d.DevicePath);

                if (!IsSupported(d))
                    continue;

                if (!TryCreate(d, media, out var g733) || g733 is null)
                    continue;

                _cachedDevice = g733;

                Logger.Debug("Selected device: VID={VID:X4} PID={PID:X4} Path={Path}",
                    d.VendorID,
                    d.ProductID,
                    d.DevicePath);

                return g733;
            }

            return null;
        }

        public static bool TryConnectReceiver(WinMediaControl? media, out G733Device? hidDevice)
        {
            hidDevice = null;

            foreach (var hid in DeviceList.Local.GetHidDevices())
            {
                if (!IsSupported(hid))
                    continue;

                if (!TryCreate(hid, media, out var g733) || g733 is null)
                    continue;

                _cachedDevice = g733;
                g733.UpdateConnectionState();

                hidDevice = g733;
                Logger.Debug("Selected device {Path}", hid.DevicePath);
                return true;
            }

            return false;
        }

        
        public void UpdateConnectionState()
        {
            try
            {
                var battery = GetBatteryInfo();

                if (battery.Status == BatteryStatus.Timeout)
                {
                    SetConnectionState(DeviceConnectionState.HeadsetSleeping);
                    return;
                }

                SetConnectionState(DeviceConnectionState.HeadsetOnline);
                if (!Config.Instance.OnConnectConfig.SetOnConnectColor) return;
                Logger.Debug("Setting OnConnectColor to: {UR}|{UG}|{UB}, {LR}|{LG}|{LB}, {OnConnectLightMode}", 
                    Config.Instance.OnConnectConfig.OnConnectColorUpperColor.R, 
                    Config.Instance.OnConnectConfig.OnConnectColorUpperColor.G, 
                    Config.Instance.OnConnectConfig.OnConnectColorUpperColor.B, 
                    Config.Instance.OnConnectConfig.OnConnectColorLowerColor.R, 
                    Config.Instance.OnConnectConfig.OnConnectColorLowerColor.G, 
                    Config.Instance.OnConnectConfig.OnConnectColorLowerColor.B, 
                    Config.Instance.OnConnectConfig.OnConnectLightMode);

                SetLights(Config.Instance.OnConnectConfig.OnConnectColorUpperColor, 
                    Config.Instance.OnConnectConfig.OnConnectColorLowerColor, 
                    Config.Instance.OnConnectConfig.OnConnectLightMode);
            }
            catch
            {
                SetConnectionState(DeviceConnectionState.ReceiverPresent);
            }
        }
        
        /// <summary>
        /// Set the upper light bar color.
        /// Pass null to disable the bar.
        /// </summary>
        public void SetUpperLightBar(G733HidClient.RgbColor color, G733HidClient.LightMode? mode)
        {
            _hid.SetLight(G733HidClient.LightTarget.Upper, color, mode);
        }

        /// <summary>
        /// Set the lower light bar color.
        /// Pass null to disable the bar.
        /// </summary>
        public void SetLowerLightBar(G733HidClient.RgbColor color, G733HidClient.LightMode? mode)
        {
            _hid.SetLight(G733HidClient.LightTarget.Lower, color, mode);
        }

        /// <summary>
        /// Set upper and lower light bars at once.
        /// Pass null to disable a bar.
        /// </summary>
        public void SetLights(G733HidClient.RgbColor upperColor, G733HidClient.RgbColor lowerColor, G733HidClient.LightMode? mode)
        {
            _hid.SetLights(upperColor, lowerColor, mode);
        }
        
        /// <summary>
        /// Get auto power off time stored in device memory
        /// </summary>
        /// <returns></returns>
        
        public int GetAutoPowerOff()
        {
            return _hid.GetAutoPowerOff();
        }
        /// <summary>
        /// Set auto power off time stored in device memory
        /// </summary>
        /// <param name="minutes"></param>

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

        private readonly Lock _ioLock = new();

        public BatteryInfo GetBatteryInfo()
        {
            lock (_ioLock)
            {
                try
                {
                    var response = _hid.SendBatteryRequest();
                    if (response.Length == 0)
                        return new BatteryInfo { Status = BatteryStatus.Timeout };

                    Logger.Debug("Raw battery response: {Hex}", Convert.ToHexString(response));
                    ushort voltage = (ushort)((response[4] << 8) | response[5]);
                    byte state = response[6];

                    if (voltage != 0)
                        return new BatteryInfo
                        {
                            Status = state == 0x03 ? BatteryStatus.Charging : BatteryStatus.Detected,
                            Level = MapVoltageToPercent(voltage),
                            VoltageMv = voltage
                        };
                    Logger.Warning("Discarded invalid battery reading: Voltage was 0.");
                    return new BatteryInfo { Status = BatteryStatus.Timeout };
                }
                catch
                {
                    return new BatteryInfo { Status = BatteryStatus.Unavailable };
                }
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
            const int max = 3990;
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