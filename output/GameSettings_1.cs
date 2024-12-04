using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG
{
    public class ConsoleDisplayConfig
    {
        public bool UseColors { get; set; } = true;
        public bool UseUnicodeBorders { get; set; } = true;
        public bool EnableCursorBlink { get; set; } = true;
        public bool UseBold { get; set; } = true;
        public int RefreshRateMs { get; set; } = 16;
        public int CursorBlinkRateMs { get; set; } = 530;
        public bool UseCurvedBorders { get; set; } = true;
    }

    public class GameSettings
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DemoRPG"
        );
        private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");
        private static GameSettings? _instance;

        // Settings properties
        public string Language { get; set; } = "en";
        public int WindowWidth { get; set; } = 80;
        public int WindowHeight { get; set; } = 24;
        public bool FullScreen { get; set; } = false;
        public ConsoleDisplayConfig Display { get; set; } = new ConsoleDisplayConfig();

        // Default constructor for JSON deserialization
        [JsonConstructor]
        public GameSettings() { }

        // Singleton access
        public static GameSettings Instance
        {
            get
            {
                _instance ??= Load();
                return _instance;
            }
        }

        public void Save()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(AppDataPath);

                // Serialize settings
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsPath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private static GameSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string jsonString = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(jsonString);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings: {ex.Message}");
            }

            // Return default settings if loading fails
            return new GameSettings();
        }

        // Compatibility property for existing code
        public static string CurrentLanguage
        {
            get => Instance.Language;
            set
            {
                if (Instance.Language != value)
                {
                    Instance.Language = value;
                    Instance.Save();
                }
            }
        }

        // Add a method to update the language directly
        public void UpdateLanguage(string language)
        {
            Language = language;
            Save();
        }
    }
}
