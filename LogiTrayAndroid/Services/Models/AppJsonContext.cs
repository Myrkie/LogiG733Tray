using System.Text.Json.Serialization;

namespace LogiTrayAndroid.Services.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(StatusResponse))]
    [JsonSerializable(typeof(MediaResponse))]
    [JsonSerializable(typeof(SessionSwitchResponse))]
    [JsonSerializable(typeof(SessionStateResponse))]
    [JsonSerializable(typeof(SetLightRequest))]
    [JsonSerializable(typeof(SetAutoPowerOffRequest))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}