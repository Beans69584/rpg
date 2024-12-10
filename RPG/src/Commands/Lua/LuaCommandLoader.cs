using NLua;
using RPG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using RPG.Core;

namespace RPG.Commands.Lua
{
    /// <summary>
    /// Lua command loader, in charge of loading user and system commands from Lua scripts.
    /// </summary>
    public class LuaCommandLoader
    {
        private readonly GameState _state;
        private readonly NLua.Lua _lua;
        private readonly string _systemScriptsPath;
        private readonly string _userScriptsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaCommandLoader"/> class.
        /// </summary>
        /// <param name="state">The current game state.</param>
        public LuaCommandLoader(GameState state)
        {
            try
            {
                _state = state;
                _lua = new NLua.Lua();

                _systemScriptsPath = Path.Combine(
                    PathUtilities.GetAssemblyDirectory(),
                    "scripts"
                );

                _userScriptsPath = Path.Combine(
                    PathUtilities.GetSettingsDirectory(),
                    "Scripts"
                );

                LuaGameApi gameApi = new(_state, _lua);

                _lua.LoadCLRPackage();
                _lua["game"] = gameApi;
                _lua["KeepClrObject"] = true;

                Logger.Info("Initializing LuaCommandLoader");

                // Load core library first
                string? coreScriptPath = Path.Combine(_systemScriptsPath, "core.lua");
                if (!File.Exists(coreScriptPath))
                {
                    Logger.Warning($"Core script not found at: {coreScriptPath}");
                }
                else
                {
                    _lua["core"] = _lua.DoFile(coreScriptPath)[0];
                    Logger.Debug("Core script loaded successfully");
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize LuaCommandLoader");
                throw;
            }
        }

        private IEnumerable<ICommand> LoadUserCommands()
        {
            if (!Directory.Exists(_userScriptsPath))
            {
                Logger.Info($"Creating user scripts directory: {_userScriptsPath}");
                Directory.CreateDirectory(_userScriptsPath);
                yield break;
            }

            foreach (string file in Directory.GetFiles(_userScriptsPath, "*.lua"))
            {
                Logger.Debug($"Loading user command from: {file}");
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
                    Logger.Error(ex, $"Error loading user command {Path.GetFileName(file)}");
                    _state.GameLog.Add($"Error loading user command {Path.GetFileName(file)}: {ex.Message}");
                }

                if (command != null)
                {
                    Logger.Debug($"Successfully loaded command: {command.Name}");
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
            Logger.Info("Loading commands");

            List<ICommand> commands = [];

            try
            {
                // Load system commands
                foreach (string file in Directory.GetFiles(_systemScriptsPath, "*.lua"))
                {
                    Logger.Debug($"Loading system command from: {file}");
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
                            chunk["category"] as string ?? "General"
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error loading system command {Path.GetFileName(file)}");
                        _state.GameLog.Add($"Error loading system command {Path.GetFileName(file)}: {ex.Message}");
                    }

                    if (command != null)
                    {
                        Logger.Debug($"Successfully loaded command: {command.Name}");
                        commands.Add(command);
                    }
                }

                // Load user commands
                foreach (ICommand command in LoadUserCommands())
                {
                    commands.Add(command);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error while loading commands");
                throw;
            }

            foreach (ICommand command in commands)
            {
                yield return command;
            }
        }
    }
}