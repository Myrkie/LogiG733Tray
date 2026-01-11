using LogiG733Tray.G733;
using System.Text.Json.Serialization;

namespace LogiG733Tray.API
{
    public record StatusResponse(
        [property: JsonPropertyName("device")] string Device,
        [property: JsonPropertyName("state")] DeviceConnectionState State,
        [property: JsonPropertyName("battery")] BatteryInfo? Battery
    );
}