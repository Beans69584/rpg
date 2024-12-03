using RPG.Commands;
using RPG.Plugins;

namespace RPG
{
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new();
        private IPluginManager? _pluginManager;

        public void Initialize(IPluginManager pluginManager)
        {
            _pluginManager = pluginManager;
        }

        public void RegisterCommand(ICommand command)
        {
            _commands[command.Name.ToLower()] = command;

            // Register aliases
            foreach (var alias in command.Aliases)
            {
                _commands[alias.ToLower()] = command;
            }
        }

        public async Task<bool> ExecuteCommand(string input, GameState state)
        {
            var parts = input.Split(' ', 2);
            var commandName = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    if (_pluginManager != null)
                    {
                        state.GameLog.Add("[Debug] Running command through middleware");
                        bool middlewareResult = await Task.FromResult(
                            _pluginManager.ProcessCommandMiddleware(input, state));
                        if (!middlewareResult)
                        {
                            state.GameLog.Add("[Debug] Middleware chain failed");
                            return false;
                        }
                    }

                    command.Execute(args, state);
                    return true;
                }
                catch (Exception ex)
                {
                    state.GameLog.Add($"[Error] {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        public IEnumerable<ICommand> GetCommands()
        {
            // create set of unique commands
            IEnumerable<ICommand> commands = _commands.Values.Distinct();

            return commands.Where(c => !string.IsNullOrEmpty(c.Name));
        }
    }
}
