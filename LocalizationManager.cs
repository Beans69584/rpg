using System.Resources;
using System.Globalization;

namespace RPG
{
    public class LocalizationManager
    {
        public event Action<string> LanguageChanged;
        private readonly ResourceManager resourceManager;
        private CultureInfo currentCulture;

        public LocalizationManager()
        {
            try
            {
                resourceManager = new ResourceManager(
                    "RPG.Resources.Strings",
                    typeof(LocalizationManager).Assembly);
                    
                // Use settings instance
                currentCulture = CultureInfo.GetCultureInfo(GameSettings.Instance.Language);
                
                // Debug info
                Console.WriteLine($"Assembly: {typeof(LocalizationManager).Assembly.FullName}");
                Console.WriteLine($"Resource: {resourceManager.BaseName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize ResourceManager: {ex}");
                // Fallback to prevent crashes
                resourceManager = new ResourceManager(typeof(LocalizationManager));
            }
        }

        public string GetString(string key, params object[] args)
        {
            try
            {
                var value = resourceManager.GetString(key, currentCulture);
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

        public void SetLanguage(string cultureName)
        {
            currentCulture = CultureInfo.GetCultureInfo(cultureName);
            LanguageChanged?.Invoke(cultureName);
        }

        public static IEnumerable<CultureInfo> GetAvailableLanguages()
        {
            return new[]
            {
                CultureInfo.GetCultureInfo("en"), // English
                CultureInfo.GetCultureInfo("es"), // Spanish
                CultureInfo.GetCultureInfo("fr"), // French
                CultureInfo.GetCultureInfo("de"), // German
                CultureInfo.GetCultureInfo("it"), // Italian
                CultureInfo.GetCultureInfo("ja"), // Japanese
                CultureInfo.GetCultureInfo("ko"), // Korean
                CultureInfo.GetCultureInfo("ru"), // Russian
                CultureInfo.GetCultureInfo("zh"), // Chinese
            };
        }
    }
}
