using HidSharp;
using Serilog;

namespace LogiG733Tray.G733
{
    public sealed class G733HidClient(HidDevice device)
    {
        private static readonly ILogger Logger = Log.ForContext<G733HidClient>();

        private bool IsOnline { get; set; } = true;

        public event Action<bool>? OnlineStateChanged;

        private void SetOffline()
        {
            if (!IsOnline)
                return;

            IsOnline = false;
            Logger.Information("Headset went offline");
            OnlineStateChanged?.Invoke(false);
        }

        private void SetOnline()
        {
            if (IsOnline)
                return;

            IsOnline = true;
            Logger.Information("Headset is back online");
            OnlineStateChanged?.Invoke(true);
        }

        private bool ValidateResponse(byte[] response, int read)
        {
            if (read < 7 || response[2] == 0xFF)
            {
                SetOffline();
                return false;
            }

            SetOnline();
            return true;
        }
        
        private const int HidppLongMessageLength = 20;

        public enum LightTarget : byte
        {
            Lower = 0x00,
            Upper = 0x01
        }

        private enum LightMode : byte
        {
            Off = 0x00,
            Static = 0x01,
            // ReSharper disable once UnusedMember.Local
            Breathing = 0x02,
            // ReSharper disable once UnusedMember.Local
            Cycle = 0x03
        }

        public struct RgbColor(byte r, byte g, byte b)
        {
            public readonly byte R = r;
            public readonly byte G = g;
            public readonly byte B = b;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static class Colors
        {
            public static readonly RgbColor Purple = new(255, 0, 255);
            public static readonly RgbColor Red = new(255, 0, 0);
            public static readonly RgbColor Green = new(0, 255, 0);
            public static readonly RgbColor Blue = new(0, 0, 255);
            public static readonly RgbColor White = new(255, 255, 255);
            public static readonly RgbColor Yellow = new(255, 255, 0);
            public static readonly RgbColor Off = new(0, 0, 0);
        }

        private byte[] BuildLightCommand(LightTarget target, LightMode mode, RgbColor color, byte brightness = 0x64)
        {
            byte[] command = new byte[HidppLongMessageLength];

            command[0] = 0x11;                // HIDPP_LONG_MESSAGE
            command[1] = 0xFF;                // Device Receiver
            command[2] = 0x04;                // Lights function
            command[3] = 0x3C;                // Sub-command
            command[4] = (byte)target;        // Target: Upper or Lower
            command[5] = (byte)mode;          // Mode: Static, Off, etc.

            command[6] = color.R;
            command[7] = color.G;
            command[8] = color.B;

            // Timing / animation bytes??
            command[9] = 0x0F;
            command[10] = 0xA0;
            command[11] = 0x00;

            command[12] = brightness;
            
            for (int i = 13; i < HidppLongMessageLength; i++)
                command[i] = 0x00;

            return command;
        }
        
        /// <summary>
        /// Sets the color or turns off the lights for a specific target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="color"></param>
        public void SetLight(LightTarget target, RgbColor? color = null)
        {
            using var stream = device.Open();

            LightMode mode = color.HasValue ? LightMode.Static : LightMode.Off;
            RgbColor rgb = color ?? Colors.Off;

            byte[] command = BuildLightCommand(target, mode, rgb);

            Thread.Sleep(10);
            stream.Write(command, 0, command.Length);
            byte[] response = new byte[HidppLongMessageLength];

            if (response[2] == 0xFF)
                SetOffline();
        }
        /// <summary>
        /// Gets auto power off timer
        /// </summary>
        /// <returns>time in minutes</returns>
        public int GetAutoPowerOff()
        {
            using var stream = device.Open();

            byte[] command = new byte[HidppLongMessageLength];
            command[0] = 0x11; // HIDPP_LONG_MESSAGE
            command[1] = 0xFF; // Device Receiver (host)
            command[2] = 0x08; // Battery / power function
            command[3] = 0x12; // Subcommand: Get Auto Power-Off

            // Fill remaining bytes with 0
            for (int i = 4; i < HidppLongMessageLength; i++)
                command[i] = 0x00;

            stream.Write(command, 0, command.Length);

            Thread.Sleep(10);

            byte[] response = new byte[HidppLongMessageLength];
            int read = stream.Read(response, 0, response.Length);

            if (read < 7)
                return -1; // invalid response

            if (response[2] != 0xFF) return response[4];
            SetOffline();
            return -1;
        }
        /// <summary>
        /// Sets auto power off timer
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>

        public int SetAutoPowerOff(int minutes)
        {
            byte value = (byte)minutes;
            byte[] command = new byte[HidppLongMessageLength];
            for (int i = 0; i < command.Length; i++)
                command[i] = 0x00;

            command[0] = 0x11;      // Report ID (HIDPP_LONG_MESSAGE)
            command[1] = 0xFF;      // Receiver = host
            command[2] = 0x08;      // Battery / Power function
            command[3] = 0x2A;      // Subcommand: Set Auto Power-Off
            command[4] = value;

            using var stream = device.Open();
            stream.Write(command, 0, command.Length);

            Thread.Sleep(10);

            byte[] response = new byte[HidppLongMessageLength];
            int read = stream.Read(response, 0, response.Length);
            if (read < 7)
                return -1;

            if (response[2] != 0xFF) return response[4];
            SetOffline();
            return -1;
        }

        /// <summary>
        /// Sets both upper and lower lights at once.
        /// Pass null to disable a bar.
        /// </summary>
        /// <param name="upperColor"></param>
        /// <param name="lowerColor"></param>
        public void SetLights(RgbColor? upperColor, RgbColor? lowerColor)
        {
            // Upper light
            {
                using var stream = device.Open();
                LightMode mode = upperColor.HasValue ? LightMode.Static : LightMode.Off;
                RgbColor rgb = upperColor ?? Colors.Off;
                byte[] command = BuildLightCommand(LightTarget.Upper, mode, rgb);
                stream.Write(command, 0, command.Length);
                byte[] response = new byte[HidppLongMessageLength];

                if (response[2] == 0xFF)
                    SetOffline();
            }

            Thread.Sleep(20);

            // Lower light
            {
                using var stream = device.Open();
                LightMode mode = lowerColor.HasValue ? LightMode.Static : LightMode.Off;
                RgbColor rgb = lowerColor ?? Colors.Off;
                byte[] command = BuildLightCommand(LightTarget.Lower, mode, rgb);
                stream.Write(command, 0, command.Length);
                byte[] response = new byte[HidppLongMessageLength];

                if (response[2] == 0xFF)
                    SetOffline();
            }
        }
        
        /// <summary>
        /// Turns off all lights.
        /// </summary>
        public void DisableLights()
        {
            SetLights(null, null);
        }
        public byte[] SendBatteryRequest()
        {
            using var stream = device.Open();

            byte[] dataRequest = new byte[HidppLongMessageLength];
            dataRequest[0] = 0x11; // HIDPP_LONG_MESSAGE
            dataRequest[1] = 0xFF; // Device Receiver
            dataRequest[2] = 0x08; // Battery / power function

            stream.Write(dataRequest, 0, dataRequest.Length);

            Thread.Sleep(10);

            byte[] response = new byte[HidppLongMessageLength];
            int read = stream.Read(response, 0, response.Length);

            return !ValidateResponse(response, read) ? [] : response;
        }
    }
}
