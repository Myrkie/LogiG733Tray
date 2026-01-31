using System.Text.Json.Serialization;
using LogiG733Tray.G733;

namespace LogiG733Tray.API.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(StatusResponse))]
    [JsonSerializable(typeof(PowerOffRequest))]
    [JsonSerializable(typeof(LightRequest))]
    [JsonSerializable(typeof(BatteryInfo))]
    [JsonSerializable(typeof(DeviceConnectionState))]
    [JsonSerializable(typeof(BatteryStatus))]
    public partial class ApiJsonContext : JsonSerializerContext;
}