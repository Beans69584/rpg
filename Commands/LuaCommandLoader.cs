using NLua;

namespace RPG.Commands
{
    public class LuaCommandLoader
    {
        private readonly string _luaPath;
        private readonly Lua _lua;
        private readonly GameState _state;

        public LuaCommandLoader(string luaPath, GameState state)
        {
            _luaPath = luaPath;
            _state = state;
            _lua = new Lua();
            
            // Register game API
            _lua["game"] = new LuaGameAPI(state);

            // Load core library first
            string corePath = Path.Combine(_luaPath, "core.lua");
            if (File.Exists(corePath))
            {
                _lua["core"] = _lua.DoFile(corePath)[0];
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

        public IEnumerable<ICommand> LoadCommands()
        {
            Directory.CreateDirectory(_luaPath);
            foreach (var file in Directory.GetFiles(_luaPath, "*.lua"))
            {
                LuaTable chunk = null;
                try
                {
                    chunk = _lua.DoFile(file)[0] as LuaTable;
                }
                catch (Exception ex)
                {
                    _state.GameLog.Add($"Error loading Lua command {Path.GetFileName(file)}: {ex.Message}");
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
        }
    }
}
