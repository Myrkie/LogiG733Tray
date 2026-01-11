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

    public class BatteryInfo
    {
        [JsonPropertyName("status")]
        public BatteryStatus Status { get; init; }

        [JsonPropertyName("level")]
        public int Level { get; init; }

        [JsonPropertyName("voltageMv")]
        public int VoltageMv { get; init; }
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