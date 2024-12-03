// Plugins/PluginExtensions.cs
namespace RPG.Plugins
{
    internal static class PluginExtensions
    {
        public static PluginType ParsePluginType(string? typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return PluginType.Mixed;
            
            return Enum.TryParse<PluginType>(typeStr, true, out var result) 
                ? result 
                : PluginType.Mixed;
        }
    }
}