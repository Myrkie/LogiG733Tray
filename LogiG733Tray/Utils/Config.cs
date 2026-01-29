using System.Text.Json;
using System.Text.Json.Serialization;
using LogiG733Tray.G733;

namespace LogiG733Tray.Utils
{
    [JsonSerializable(typeof(Config))]
    [JsonSerializable(typeof(OnConnectConfig))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true, AllowTrailingCommas = true)]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext;
    
    [Serializable]
    public class OnConnectConfig
    {
        public bool SetOnConnectColor { get; set; } = true;
        public G733HidClient.RgbColor OnConnectColorUpperColor { get; set; } = new(0,0,0);
        public G733HidClient.RgbColor OnConnectColorLowerColor { get; set; } = new(0,0,0);
        public G733HidClient.LightMode OnConnectLightMode { get; set; } = G733HidClient.LightMode.Off;
    }
    
    [Serializable]
    public class Config
    {
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "Config" ,"config.json");
        public static Config Instance { get; } = LoadConfig();
        
        public bool InitializeApi { get; set; }
        public OnConnectConfig OnConnectConfig { get; set; } = new();

        static Config LoadConfig()
        {
            Config? cfg = File.Exists(ConfigPath) ? JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), ConfigSourceGenerationContext.Default.Config) : null;
            if(cfg == null)
            {
                cfg = new Config();
                cfg.SaveConfig();
            }
            return cfg;
        }

        private void SaveConfig()
        {
            string json = JsonSerializer.Serialize(this, ConfigSourceGenerationContext.Default.Config);
            string? directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(ConfigPath, json);
        }
    }
}