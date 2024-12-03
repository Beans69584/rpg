// Plugins/LuaPlugin.cs
using NLua;

namespace RPG.Plugins
{
    public class LuaPlugin : IPlugin
    {
        private readonly Lua _lua;
        private readonly LuaTable _pluginTable;

        public string Name => _pluginTable["name"] as string ?? "Unknown Plugin";
        public string Author => _pluginTable["author"] as string ?? "Unknown";
        public string Version => _pluginTable["version"] as string ?? "1.0.0";
        public PluginType Type => PluginExtensions.ParsePluginType(_pluginTable["type"] as string);

        public LuaPlugin(string luaScript)
        {
            _lua = new Lua();
            _lua.LoadCLRPackage();
            
            // Add necessary types and APIs
            _lua["PluginType"] = typeof(PluginType);
            _lua["ConsoleColor"] = typeof(ConsoleColor);
            
            _pluginTable = _lua.DoString(luaScript)[0] as LuaTable 
                ?? throw new Exception("Invalid plugin script");
        }

        public void Initialize(PluginContext context)
        {
            var initFunc = _pluginTable["initialize"] as LuaFunction;
            initFunc?.Call(_pluginTable, context);
        }

        public void Shutdown()
        {
            var shutdownFunc = _pluginTable["shutdown"] as LuaFunction;
            shutdownFunc?.Call(_pluginTable);
            _lua.Dispose();
        }
    }
}