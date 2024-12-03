// Plugins/IPlugin.cs
namespace RPG.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        string Version { get; }
        PluginType Type { get; }
        void Initialize(PluginContext context);
        void Shutdown();
    }

    public enum PluginType
    {
        UI,
        Command,
        Middleware,
        Theme,
        Mixed
    }
}