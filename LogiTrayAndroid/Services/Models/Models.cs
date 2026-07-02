using System.Text.Json.Serialization;

namespace LogiTrayAndroid.Services.Models
{
    public class SetLightRequest
    {
        public string Target { get; set; } = string.Empty;
        public int UpperR { get; set; }
        public int UpperG { get; set; }
        public int UpperB { get; set; }
        public int LowerR { get; set; }
        public int LowerG { get; set; }
        public int LowerB { get; set; }
        public LightMode Mode { get; set; }
    }

    public class SetAutoPowerOffRequest
    {
        [JsonPropertyName("Minutes")] 
        public int Minutes { get; set; }
    }
    
    public record StatusResponse(
        [property: JsonPropertyName("device")] string Device,
        [property: JsonPropertyName("state")] DeviceConnectionState State,
        [property: JsonPropertyName("battery")] BatteryInfo? Battery
    );
    
    public record MediaResponse(
        [property: JsonPropertyName("session")] string Session,
        [property: JsonPropertyName("media")] string Media,
        [property: JsonPropertyName("display")] string Display
    );
    
    public record SessionSwitchResponse(
        [property: JsonPropertyName("newSession")] string NewSession
    );
    
    public record SessionPowerStateResponse(
        [property: JsonPropertyName("sessionPwr")] bool SessionPwr
    );

    public class BatteryInfo
    {
        [JsonPropertyName("status")]
        public BatteryStatus Status { get; init; }

        [JsonPropertyName("level")]
        public int Level { get; init; }

        [JsonPropertyName("voltageMv")]
        public int VoltageMv { get; init; }
    }

    public enum LightMode : byte
    {
        Off = 0x00,
        Static = 0x01,
        // ReSharper disable once UnusedMember.Local
        Breathing = 0x02,
        // ReSharper disable once UnusedMember.Local
        Cycle = 0x03
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
}