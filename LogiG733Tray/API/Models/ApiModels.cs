using System.Text.Json.Serialization;
using LogiG733Tray.G733;

namespace LogiG733Tray.API.Models
{
    public record PowerOffRequest(int Minutes);
    
    public record LightRequest(
        string Target,
        byte? UpperR = null,
        byte? UpperG = null,
        byte? UpperB = null,
        byte? LowerR = null,
        byte? LowerG = null,
        byte? LowerB = null,
        G733HidClient.LightMode? Mode = null
    );
    public record StatusResponse(
        [property: JsonPropertyName("device")] string Device,
        [property: JsonPropertyName("state")] DeviceConnectionState State,
        [property: JsonPropertyName("battery")] BatteryInfo? Battery
    );
}