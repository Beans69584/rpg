// Plugins/IPluginManager.cs
namespace RPG.Plugins
{
    public interface IPluginManager
    {
        void LoadPlugin(string path);
        void UnloadPlugin(string pluginName);
        void EnablePlugin(string pluginName);
        void DisablePlugin(string pluginName);
        T? GetPlugin<T>(string name) where T : class, IPlugin;
        IEnumerable<IPlugin> GetPlugins(PluginType? type = null);
        void RegisterMiddleware(IMiddleware middleware);
        void RegisterTheme(ITheme theme);
        void Initialize(PluginContext context);
        bool ProcessCommandMiddleware(string command, GameState state);
        void ProcessRenderMiddleware(Region region, Action render);
    }
}