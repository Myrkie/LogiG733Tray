using HidSharp;

namespace LogiG733Tray.G733
{
    public class G733Device
    {
        private const int VendorId = 0x046D; // Logi
        private static readonly int[] SupportedProductIDs = [0x0afe];
        
        public string Name { get; }
        private readonly G733HidClient _hid;
        
        private G733Device(HidDevice device)
        {
            _hid = new G733HidClient(device);
            Name = device.GetProductName(GetStringFlags.None);
        }

        private static bool IsSupported(HidDevice device)
        {
            return device.VendorID == VendorId &&
                   SupportedProductIDs.Contains(device.ProductID);
        }

        public static bool TryCreate(HidDevice device, out G733Device? g733)
        {
            if (!IsSupported(device))
            {
                g733 = null;
                return false;
            }

            g733 = new G733Device(device);
            return true;
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

        private static int MapVoltageToPercent(ushort voltage)
        {
            // my device arrived with a degraded battery so im using degraded values.
            const int max = 3900;
            const int min = 3300;

            switch (voltage)
            {
                case >= max:
                    return 100;
                case <= min:
                    return 0;
            }

            double percent = (double)(voltage - min) / (max - min);
            percent = Math.Pow(percent, 1.7);
            return (int)Math.Round(percent * 100);
        }
    }
    public enum BatteryStatus
    {
        Unavailable,
        Detected,
        Charging,
        Timeout
    }

    public class BatteryInfo
    {
        public BatteryStatus Status { get; init; }
        public int Level { get; init; } = -1;
        public int VoltageMv { get; init; } = -1;
    }
}