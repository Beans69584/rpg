using NLua;
using System.Reflection;

namespace RPG.Commands
{
    public class LuaCommandLoader
    {
        private readonly GameState _state;
        private readonly LuaGameAPI _gameApi;
        private readonly Lua _lua;
        private readonly Assembly _assembly;
        private readonly string _userScriptsPath;

        public LuaCommandLoader(GameState state)
        {
            _state = state;
            _lua = new Lua();
            _assembly = Assembly.GetExecutingAssembly();
            
            // Cross-platform app data path
            string appDataPath = Environment.GetFolderPath(
                Environment.OSVersion.Platform == PlatformID.Unix || 
                Environment.OSVersion.Platform == PlatformID.MacOSX
                    ? Environment.SpecialFolder.Personal
                    : Environment.SpecialFolder.ApplicationData
            );
            
            _userScriptsPath = Path.Combine(
                appDataPath,
                Environment.OSVersion.Platform == PlatformID.Unix || 
                Environment.OSVersion.Platform == PlatformID.MacOSX
                    ? "Library/Application Support/DemoRPG/Scripts"
                    : "DemoRPG/Scripts"
            );
            
            // Create game API with our Lua instance
            _gameApi = new LuaGameAPI(_state, _lua);
            
            // Register game API and types
            _lua.LoadCLRPackage();
            _lua["game"] = _gameApi;
            _lua["KeepClrObject"] = true;

            // Load core library first
            var coreScript = GetEmbeddedScript("core.lua");
            if (coreScript != null)
            {
                _lua["core"] = _lua.DoString(coreScript)[0];
            }

            // Add helper functions and command creation API
            _lua.DoString(@"
                -- Command creation helper
                function CreateCommand(config)
                    assert(type(config) == 'table', 'CreateCommand requires a config table')
                    assert(config.name, 'Command requires a name')
                    assert(config.description, 'Command requires a description')
                    assert(config.execute, 'Command requires an execute function')

                    return {
                        name = config.name,
                        description = config.description,
                        execute = config.execute,
                        aliases = config.aliases or {},
                        usage = config.usage or '',
                        category = config.category or 'General'
                    }
                end

                -- String split helper
                function split(str, sep)
                    sep = sep or '%s'
                    local t = {}
                    for field in string.gmatch(str, '[^'..sep..']+') do
                        table.insert(t, field)
                    end
                    return t
                end

                -- Arguments parser helper
                function parseArgs(argStr)
                    local args = split(argStr or '')
                    return {
                        raw = argStr or '',
                        list = args,
                        count = #args,
                        get = function(self, index)
                            return self.list[index]
                        end
                    }
                end
            ");
        }

        private string GetEmbeddedScript(string filename)
        {
            var resourcePath = _assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(filename));
            if (resourcePath == null) return null;
            
            using var stream = _assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private IEnumerable<ICommand> LoadUserCommands()
        {
            if (!Directory.Exists(_userScriptsPath))
            {
                Directory.CreateDirectory(_userScriptsPath);
                yield break;
            }

            foreach (var file in Directory.GetFiles(_userScriptsPath, "*.lua"))
            {
                LuaTable chunk = null;
                try
                {
                    chunk = _lua.DoFile(file)[0] as LuaTable;
                }
                catch (Exception ex)
                {
                    _state.GameLog.Add($"Error loading user command {Path.GetFileName(file)}: {ex.Message}");
                }

                if (chunk != null)
                {
                    yield return new LuaCommand(
                        chunk["name"] as string ?? "",
                        chunk["description"] as string ?? "",
                        chunk["execute"] as LuaFunction,
                        (chunk["aliases"] as LuaTable)?.Values.Cast<string>().ToArray() ?? Array.Empty<string>(),
                        chunk["usage"] as string ?? "",
                        chunk["category"] as string ?? "User Commands"
                    );
                }
            }
        }

        public IEnumerable<ICommand> LoadCommands()
        {
            // Load embedded system commands
            var resources = _assembly.GetManifestResourceNames()
                .Where(r => r.EndsWith(".lua"))
                .Where(r => !r.EndsWith("core.lua"));

            foreach (var resource in resources)
            {
                LuaTable chunk = null;
                try
                {
                    string script = GetEmbeddedScript(Path.GetFileName(resource));
                    if (script != null)
                    {
                        chunk = _lua.DoString(script)[0] as LuaTable;
                    }
                }
                catch (Exception ex)
                {
                    _state.GameLog.Add($"Error loading Lua command {resource}: {ex.Message}");
                }

                if (chunk != null)
                {
                    yield return new LuaCommand(
                        chunk["name"] as string ?? "",
                        chunk["description"] as string ?? "",
                        chunk["execute"] as LuaFunction,
                        (chunk["aliases"] as LuaTable)?.Values.Cast<string>().ToArray() ?? Array.Empty<string>(),
                        chunk["usage"] as string ?? "",
                        chunk["category"] as string ?? "General"
                    );
                }
            }

            // Load user commands
            foreach (var command in LoadUserCommands())
            {
                yield return command;
            }
        }
    }
}
