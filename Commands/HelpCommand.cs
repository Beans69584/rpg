namespace RPG.Commands
{
    public class HelpCommand : BaseCommand
    {
        private readonly CommandHandler _commandHandler;

        public HelpCommand(CommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        public override string Name => "help";
        public override string Description => "Shows list of available commands";

        public override void Execute(string args, GameState state)
        {
            state.GameLog.Add("Available commands:");
            foreach (var cmd in _commandHandler.GetCommands())
            {
                state.GameLog.Add($"- {cmd.Name}: {cmd.Description}");
            }
        }
    }
}
