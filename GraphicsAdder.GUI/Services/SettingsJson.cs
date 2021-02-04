using GraphicsAdder.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphicsAdder.Services
{
    public class SettingsJson
    {
        private const string SettingsPath = "settings.json";

        public void SaveSettings(Settings settings) =>
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings.SettingsInternal, new JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true
            }));

        public Settings LoadSettings()
        {
            try
            {
                var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
                if (settings == null) throw new InvalidDataException();
                return settings;
            }
            catch
            {
                return new Settings();
            }
        }
    }
}
