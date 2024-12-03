// Plugins/PluginContext.cs
namespace RPG.Plugins
{
    public class PluginContext
    {
        public GameState GameState { get; }
        public ConsoleWindowManager WindowManager { get; }
        public CommandHandler CommandHandler { get; }
        public IPluginManager PluginManager { get; }
        public Dictionary<string, object> SharedState { get; }

        public PluginContext(GameState state, ConsoleWindowManager manager,
            CommandHandler cmdHandler, IPluginManager pluginManager)
        {
            GameState = state;
            WindowManager = manager;
            CommandHandler = cmdHandler;
            PluginManager = pluginManager;
            SharedState = new Dictionary<string, object>();
        }

        public void LogDebug(string message)
        {
            GameState.GameLog.Add($"[Debug] {message}");
        }

        public void LogPlugin(string pluginName, string message)
        {
            GameState.GameLog.Add($"[Plugin: {pluginName}] {message}");
        }
    }
}