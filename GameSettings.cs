using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG
{
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
                Instance.Language = value;
                Instance.Save();
            }
        }
    }
}
