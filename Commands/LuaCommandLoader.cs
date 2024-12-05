using NLua;
using RPG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RPG.Commands
{
    /// <summary>
    /// Lua command loader, in charge of loading user and system commands from Lua scripts.
    /// </summary>
    public class LuaCommandLoader
    {
        private readonly GameState _state;
        private readonly Lua _lua;
        private readonly Assembly _assembly;
        private readonly string _userScriptsPath;

        /// <summary>
        /// Initialises a new instance of the <see cref="LuaCommandLoader"/> class.
        /// </summary>
        /// <param name="state">The current game state.</param>
        public LuaCommandLoader(GameState state)
        {
            _state = state;
            _lua = new Lua();
            _assembly = Assembly.GetExecutingAssembly();

            _userScriptsPath = Path.Combine(
                PathUtilities.GetSettingsDirectory(),
                "Scripts"
            );

            LuaGameApi gameApi = new(_state, _lua);

            _lua.LoadCLRPackage();
            _lua["game"] = gameApi;
            _lua["KeepClrObject"] = true;

            // Load core library first
            string? coreScript = GetEmbeddedScript("core.lua");
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

        private string? GetEmbeddedScript(string filename)
        {
            string? resourcePath = Array.Find(_assembly.GetManifestResourceNames(), r => r.EndsWith(filename));
            if (resourcePath == null) return null;

            using Stream? stream = _assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return null;
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private IEnumerable<ICommand> LoadUserCommands()
        {
            if (!Directory.Exists(_userScriptsPath))
            {
                Directory.CreateDirectory(_userScriptsPath);
                yield break;
            }

            foreach (string file in Directory.GetFiles(_userScriptsPath, "*.lua"))
            {
                LuaCommand? command = null;
                try
                {
                    if (_lua.DoFile(file)[0] is not LuaTable chunk) continue;

                    if (chunk["execute"] is not LuaFunction executeFunction) continue;

                    command = new LuaCommand(
                        chunk["name"] as string ?? "",
                        chunk["description"] as string ?? "",
                        executeFunction,
                        (chunk["aliases"] as LuaTable)?.Values.Cast<string>().ToArray() ?? [],
                        chunk["usage"] as string ?? "",
                        chunk["category"] as string ?? "User Commands"
                    );
                }
                catch (Exception ex)
                {
                    _state.GameLog.Add($"Error loading user command {Path.GetFileName(file)}: {ex.Message}");
                }

                if (command != null)
                {
                    yield return command;
                }
            }
        }

        /// <summary>
        /// Loads all available commands from embedded and user scripts.
        /// </summary>
        /// <returns>An enumerable collection of commands.</returns>
        public IEnumerable<ICommand> LoadCommands()
        {
            // Load embedded system commands
            IEnumerable<string> resources = _assembly.GetManifestResourceNames()
                .Where(r => r.EndsWith(".lua"))
                .Where(r => !r.EndsWith("core.lua"));

            foreach (string resource in resources)
            {
                LuaTable? chunk = null;
                LuaCommand? command = null;
                try
                {
                    string? script = GetEmbeddedScript(Path.GetFileName(resource));
                    if (script != null)
                    {
                        chunk = _lua.DoString(script)[0] as LuaTable;
                        if (chunk == null) continue;

                        if (chunk["execute"] is not LuaFunction executeFunction) continue;

                        command = new LuaCommand(
                            chunk["name"] as string ?? "",
                            chunk["description"] as string ?? "",
                            executeFunction,
                            (chunk["aliases"] as LuaTable)?.Values.Cast<string>().ToArray() ?? [],
                            chunk["usage"] as string ?? "",
                            chunk["category"] as string ?? "General"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _state.GameLog.Add($"Error loading Lua command {resource}: {ex.Message}");
                }

                if (command != null)
                {
                    yield return command;
                }
            }

            // Load user commands
            foreach (ICommand command in LoadUserCommands())
            {
                yield return command;
            }
        }
    }
}