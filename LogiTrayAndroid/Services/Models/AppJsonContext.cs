using System.Text.Json.Serialization;

namespace LogiTrayAndroid.Services.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(StatusResponse))]
    [JsonSerializable(typeof(BatteryInfo))]
    [JsonSerializable(typeof(BatteryStatus))]
    [JsonSerializable(typeof(DeviceConnectionState))]
    [JsonSerializable(typeof(MediaResponse))]
    [JsonSerializable(typeof(SessionSwitchResponse))]
    [JsonSerializable(typeof(SessionPowerStateResponse))]
    [JsonSerializable(typeof(SetLightRequest))]
    [JsonSerializable(typeof(SetAutoPowerOffRequest))]
    [JsonSerializable(typeof(LightMode))]
    [JsonSerializable(typeof(int))]
    internal partial class AppJsonContext : JsonSerializerContext;
}