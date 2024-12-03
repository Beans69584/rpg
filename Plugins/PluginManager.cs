// Plugins/PluginManager.cs
namespace RPG.Plugins
{
    public class PluginManager : IPluginManager
    {
        private readonly Dictionary<string, IPlugin> _plugins = new();
        private readonly List<IMiddleware> _middleware = new();
        private readonly List<ITheme> _themes = new();
        private readonly Dictionary<string, bool> _enabledState = new();
        private PluginContext? _context;

        public void Initialize(PluginContext context)
        {
            _context = context;
        }

        public void LoadPlugin(string path)
        {
            IPlugin? plugin = null;
            var extension = Path.GetExtension(path).ToLowerInvariant();

            try
            {
                _context?.LogDebug($"Loading plugin: {Path.GetFileName(path)}");

                plugin = extension switch
                {
                    ".lua" => new LuaPlugin(File.ReadAllText(path)),
                    ".dll" => LoadNativePlugin(path),
                    _ => throw new NotSupportedException($"Unsupported plugin type: {extension}")
                };

                if (plugin != null)
                {
                    _context?.LogPlugin(plugin.Name, "Plugin loaded successfully");
                    _plugins[plugin.Name] = plugin;
                    _enabledState[plugin.Name] = true;
                }
            }
            catch (Exception ex)
            {
                _context?.LogDebug($"Failed to load plugin {path}: {ex.Message}");
            }
        }

        private IPlugin? LoadNativePlugin(string path)
        {
            try
            {
                var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                var pluginType = assembly.GetTypes().FirstOrDefault(t =>
                    typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                return pluginType != null ?
                    Activator.CreateInstance(pluginType) as IPlugin :
                    null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load native plugin {path}: {ex.Message}");
                return null;
            }
        }

        public void UnloadPlugin(string pluginName)
        {
            if (_plugins.TryGetValue(pluginName, out var plugin))
            {
                try
                {
                    plugin.Shutdown();
                    _plugins.Remove(pluginName);
                    _enabledState.Remove(pluginName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error unloading plugin {pluginName}: {ex.Message}");
                }
            }
        }

        public void EnablePlugin(string pluginName)
        {
            if (_plugins.ContainsKey(pluginName))
            {
                _enabledState[pluginName] = true;
            }
        }

        public void DisablePlugin(string pluginName)
        {
            if (_plugins.ContainsKey(pluginName))
            {
                _enabledState[pluginName] = false;
            }
        }

        public T? GetPlugin<T>(string name) where T : class, IPlugin
        {
            return _plugins.TryGetValue(name, out var plugin) ? plugin as T : null;
        }

        public IEnumerable<IPlugin> GetPlugins(PluginType? type = null)
        {
            return _plugins.Values
                .Where(p => type == null || p.Type == type)
                .Where(p => _enabledState[p.Name]);
        }

        public void RegisterMiddleware(IMiddleware middleware)
        {
            if (!_middleware.Any(m => m.Name == middleware.Name))
            {
                _middleware.Add(middleware);
            }
        }

        public void RegisterTheme(ITheme theme)
        {
            if (!_themes.Any(t => t.Name == theme.Name))
            {
                _themes.Add(theme);
            }
        }

        public bool ProcessCommandMiddleware(string command, GameState state)
        {
            try
            {
                // Log the number of middleware for debugging
                state.GameLog.Add($"[Debug] Processing {_middleware.Count} middleware items");

                int currentIndex = 0;

                void Next()
                {
                    if (currentIndex < _middleware.Count)
                    {
                        var current = _middleware[currentIndex];
                        state.GameLog.Add($"[Debug] Executing middleware: {current.Name}");
                        currentIndex++;
                        current.ProcessCommand(command, state, Next);
                    }
                }

                Next();
                return true;
            }
            catch (Exception ex)
            {
                state.GameLog.Add($"[Middleware Error] {ex.Message}");
                return false;
            }
        }

        public void ProcessRenderMiddleware(Region region, Action render)
        {
            int currentIndex = 0;

            void Next()
            {
                if (currentIndex < _middleware.Count)
                {
                    var current = _middleware[currentIndex];
                    currentIndex++;
                    current.ProcessRender(region, render, Next);
                }
                else
                {
                    render();
                }
            }

            Next();
        }

        public ITheme? GetActiveTheme()
        {
            // Could be enhanced to support theme selection
            return _themes.FirstOrDefault();
        }

        public void ListLoadedPlugins()
        {
            if (_context == null) return;

            _context.GameState.GameLog.Add("\n=== Loaded Plugins ===");
            foreach (var plugin in _plugins.Values)
            {
                _context.GameState.GameLog.Add(
                    $"- {plugin.Name} v{plugin.Version} by {plugin.Author}");
                _context.GameState.GameLog.Add(
                    $"  Type: {plugin.Type}, Status: {(_enabledState[plugin.Name] ? "Enabled" : "Disabled")}");
            }

            _context.GameState.GameLog.Add("\n=== Registered Middleware ===");
            foreach (var mw in _middleware)
            {
                _context.GameState.GameLog.Add($"- {mw.Name}");
            }

            _context.GameState.GameLog.Add("\n=== Registered Themes ===");
            foreach (var theme in _themes)
            {
                _context.GameState.GameLog.Add($"- {theme.Name}");
            }

            _context.GameState.GameLog.Add("");
        }
    }
}