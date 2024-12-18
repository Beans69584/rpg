using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using RPG.UI;
using RPG.Utils;

namespace RPG.Core
{
    /// <summary>
    /// Configuration for <see cref="ConsoleWindowManager"/> display settings. 
    /// </summary>
    public class ConsoleDisplayConfig
    {
        /// <summary>
        /// Whether to use colors in the console display.
        /// </summary>
        public bool UseColors { get; set; } = true;
        /// <summary>
        /// Whether to use Unicode box-drawing characters for borders.
        /// </summary>
        public bool UseUnicodeBorders { get; set; } = true;
        /// <summary>
        /// Whether to enable cursor blinking.
        /// </summary>
        public bool EnableCursorBlink { get; set; } = true;
        /// <summary>
        /// Whether to use bold text in the console display.
        /// </summary>
        public bool UseBold { get; set; } = true;
        /// <summary>
        /// The refresh rate for the console display, in milliseconds.
        /// </summary>
        public int RefreshRateMs { get; set; } = 16;
        /// <summary>
        /// The cursor blink rate, in milliseconds.
        /// </summary>
        public int CursorBlinkRateMs { get; set; } = 530;
        /// <summary>
        /// Whether to use curved borders in the console display.
        /// </summary>
        public bool UseCurvedBorders { get; set; } = true;
    }

    /// <summary>
    /// Game settings for the RPG game.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="GameSettings"/> class.
    /// </remarks>
    [method: JsonConstructor]
    public class GameSettings()
    {
        private static readonly string SettingsDirectory = PathUtilities.GetSettingsDirectory();
        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
        private static GameSettings? _instance;

        // Settings properties
        /// <summary>
        /// The language code for the game.
        /// </summary>
        public string Language { get; set; } = "en";
        /// <summary>
        /// The width of the game window.
        /// </summary>
        public int WindowWidth { get; set; } = 80;
        /// <summary>
        /// The height of the game window.
        /// </summary>
        public int WindowHeight { get; set; } = 24;
        /// <summary>
        /// Whether to run the game in full-screen mode.
        /// </summary>
        public bool FullScreen { get; set; } = false;
        /// <summary>
        /// The display configuration for the game.
        /// </summary>
        public ConsoleDisplayConfig Display { get; set; } = new ConsoleDisplayConfig();

        // Singleton access
        /// <summary>
        /// Gets the singleton instance of the <see cref="GameSettings"/> class.
        /// </summary>
        public static GameSettings Instance
        {
            get
            {
                _instance ??= Load();
                return _instance;
            }
        }

        /// <summary>
        /// Saves the game settings to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    if (!Directory.Exists(SettingsDirectory))
                    {
                        throw new DirectoryNotFoundException($"Could not create settings directory: {SettingsDirectory}");
                    }
                }

                JsonSerializerOptions options = new() { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsPath, jsonString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings to {SettingsPath}: {ex.Message}", ex);
            }
        }

        private static GameSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string jsonString = File.ReadAllText(SettingsPath);
                    GameSettings? settings = JsonSerializer.Deserialize<GameSettings>(jsonString);
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


        /// <summary>
        /// Gets or sets the current language for the game.
        /// </summary>
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


        /// <summary>
        /// Updates the language for the game settings.
        /// </summary>
        /// <param name="language"></param>
        public void UpdateLanguage(string language)
        {
            Language = language;
            Save();
        }

        internal static bool HasChanges()
        {
            throw new NotImplementedException();
        }
    }
}
