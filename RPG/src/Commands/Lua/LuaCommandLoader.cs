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
        private readonly string _baseScriptsPath;

        private static string EscapePath(string path)
        {
            // Normalize path separators for Lua
            return path.Replace("\\", "/").Replace("'", "\\'");
        }

        public LuaCommandLoader(GameState state)
        {
            _state = state;
            _lua = new NLua.Lua();
            _baseScriptsPath = Path.Combine(PathUtilities.GetAssemblyDirectory(), "scripts");

            InitializeLuaEnvironment();
        }

        private void InitializeLuaEnvironment()
        {
            _lua.LoadCLRPackage();
            _lua["KeepClrObject"] = true;
            _lua["game"] = new LuaGameApi(_state, _lua);

            // Setup module paths with proper escaping
            string commandsPath = EscapePath(Path.Combine(_baseScriptsPath, "commands"));
            string libsPath = EscapePath(Path.Combine(_baseScriptsPath, "libs"));
            string basePath = EscapePath(_baseScriptsPath);

            // Update package.path to support both direct files and dot notation
            _lua.DoString($@"
                package = package or {{}}
                package.path = '{commandsPath}/?.lua;' ..
                             '{commandsPath}/?/init.lua;' ..
                             '{libsPath}/?.lua;' ..
                             '{libsPath}/?/init.lua;' ..
                             '{basePath}/?.lua;' ..
                             '{libsPath}/?/?.lua;' ..              -- For dot notation (combat_helpers.messages)
                             '{libsPath}/?.lua;' ..                -- For direct requires
                             '{basePath}/?.lua'

                -- Custom require function to handle dot notation
                local original_require = require
                require = function(modname)
                    -- Convert dots to directory separators
                    local path = modname:gsub('%.', '/')
                    return original_require(path)
                end

                function CreateCommand(config)
                    if not config then error('CreateCommand requires a config table') end
                    if not config.name then error('Command requires a name') end
                    if not config.description then error('Command requires a description') end
                    if not config.execute then error('Command requires an execute function') end

                    return {{
                        name = config.name,
                        description = config.description,
                        execute = config.execute,
                        aliases = config.aliases or {{}},
                        usage = config.usage or '',
                        category = config.category or 'General'
                    }}
                end
            ");
        }

        public IEnumerable<ICommand> LoadCommands()
        {
            List<ICommand> commands = [];
            string commandsDir = Path.Combine(_baseScriptsPath, "commands");

            // First load utility modules from libs directory
            string libsDir = Path.Combine(_baseScriptsPath, "libs");
            if (Directory.Exists(libsDir))
            {
                // Load nested modules first
                foreach (string dir in Directory.GetDirectories(libsDir, "*", SearchOption.AllDirectories))
                {
                    foreach (string file in Directory.GetFiles(dir, "*.lua"))
                    {
                        try
                        {
                            Logger.Debug($"Pre-loading library: {file}");
                            _lua.DoFile(file);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error loading library {file}");
                        }
                    }
                }

                // Then load root level modules
                foreach (string file in Directory.GetFiles(libsDir, "*.lua"))
                {
                    try
                    {
                        Logger.Debug($"Pre-loading library: {file}");
                        _lua.DoFile(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error loading library {file}");
                    }
                }
            }

            // Then load commands
            foreach (string file in Directory.GetFiles(commandsDir, "*.lua", SearchOption.AllDirectories))
            {
                try
                {
                    Logger.Debug($"Loading command from: {file}");

                    string relativePath = Path.GetRelativePath(commandsDir, file);
                    string moduleDir = Path.GetDirectoryName(relativePath) ?? "";

                    if (!string.IsNullOrEmpty(moduleDir))
                    {
                        string escapedPath = EscapePath(Path.Combine(commandsDir, moduleDir));
                        _lua.DoString($"package.path = '{escapedPath}/?.lua;' .. package.path");
                    }

                    object[] result = _lua.DoFile(file);

                    if (!string.IsNullOrEmpty(moduleDir))
                    {
                        _lua.DoString("package.path = string.match(package.path, ';(.+)$') or ''");
                    }

                    if (result == null || result.Length == 0 || result[0] is not LuaTable commandTable)
                    {
                        continue;
                    }

                    LuaCommand command = new(
                        commandTable["name"] as string ?? "",
                        commandTable["description"] as string ?? "",
                        commandTable["execute"] as LuaFunction ?? throw new InvalidOperationException("Command missing execute function"),
                        (commandTable["aliases"] as LuaTable)?.Values.Cast<string>().ToArray() ?? [],
                        commandTable["usage"] as string ?? "",
                        commandTable["category"] as string ?? "General"
                    );

                    commands.Add(command);
                    Logger.Debug($"Successfully loaded command: {command.Name}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error loading command from {file}");
                    _state.GameLog.Add($"Error loading command {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return commands;
        }
    }
}