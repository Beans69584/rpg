using RPG.Commands;

namespace RPG
{
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new();

        public void RegisterCommand(ICommand command)
        {
            _commands[command.Name.ToLower()] = command;
        }

        public bool ExecuteCommand(string input, GameState state)
        {
            var parts = input.Split(' ', 2);
            var commandName = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            if (_commands.TryGetValue(commandName, out var command))
            {
                command.Execute(args, state);
                return true;
            }

            return false;
        }

        public IEnumerable<ICommand> GetCommands()
        {
            // filter out any commands with no names
            return _commands.Values.Where(c => !string.IsNullOrEmpty(c.Name));
        }
    }
}
