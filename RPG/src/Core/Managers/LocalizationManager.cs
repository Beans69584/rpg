using System.Resources;
using System.Globalization;
using System;
using System.Collections.Generic;

namespace RPG.Core.Managers
{
    /// <summary>
    /// Manages localisation of strings in game.
    /// </summary>
    public class LocalisationManager
    {
        /// <summary>
        /// Event that is triggered when the language is changed.
        /// </summary>
        public event Action<string>? LanguageChanged;
        /// <summary>
        /// The current culture of the game.
        /// </summary>
        public required CultureInfo CurrentCulture { get; set; }
        private readonly ResourceManager resourceManager;

        /// <summary>
        /// Initialises a new instance of the <see cref="LocalisationManager"/> class.
        /// </summary>
        public LocalisationManager()
        {
            try
            {
                resourceManager = new ResourceManager(
                    "RPG.Resources.Strings",
                    typeof(LocalisationManager).Assembly);

                // Use settings instance
                CurrentCulture = CultureInfo.GetCultureInfo(GameSettings.Instance.Language);

                // Debug info
                Console.WriteLine($"Assembly: {typeof(LocalisationManager).Assembly.FullName}");
                Console.WriteLine($"Resource: {resourceManager.BaseName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialise ResourceManager: {ex}");
                // Fallback to prevent crashes
                resourceManager = new ResourceManager(typeof(LocalisationManager));
            }
        }

        /// <summary>
        /// Gets a localised string from the resource file.
        /// </summary>
        /// <param name="key">The key of the string to get.</param>
        /// <param name="args">Optional arguments to format the string.</param>
        /// <returns>The localised string.</returns>
        public string GetString(string key, params object[] args)
        {
            try
            {
                string? value = resourceManager.GetString(key, CurrentCulture);
                if (value == null)
                {
                    Console.WriteLine($"Missing resource string: {key}");
                    return key;
                }
                return args.Length > 0 ? string.Format(value, args) : value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get string {key}: {ex.Message}");
                return key;
            }
        }

        /// <summary>
        /// Sets the current language of the game.
        /// </summary>
        /// <param name="cultureName">The name of the culture to set.</param>
        public void SetLanguage(string cultureName)
        {
            CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            LanguageChanged?.Invoke(cultureName);
        }

        /// <summary>
        /// Gets the available languages for the game.
        /// </summary>
        /// <returns>An enumerable of available languages.</returns>
        public static IEnumerable<CultureInfo> GetAvailableLanguages()
        {
            return
            [
                CultureInfo.GetCultureInfo("en"), // English
                CultureInfo.GetCultureInfo("es"), // Spanish
                CultureInfo.GetCultureInfo("fr"), // French
                CultureInfo.GetCultureInfo("de"), // German
                CultureInfo.GetCultureInfo("it"), // Italian
                CultureInfo.GetCultureInfo("ja"), // Japanese
                CultureInfo.GetCultureInfo("ko"), // Korean
                CultureInfo.GetCultureInfo("ru"), // Russian
                CultureInfo.GetCultureInfo("zh"), // Chinese
            ];
        }
    }
}