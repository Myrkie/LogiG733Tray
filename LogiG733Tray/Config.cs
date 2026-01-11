using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogiG733Tray
{
    [JsonSerializable(typeof(Config))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true, AllowTrailingCommas = true)]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext;
    [Serializable]
    public class Config
    {
        static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "Config" ,"config.json");
        public static Config Instance { get; } = LoadConfig();
        
        public bool InitializeApi { get; set; }

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

        void SaveConfig()
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