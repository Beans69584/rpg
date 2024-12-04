namespace RPG.Commands
{
    /// <summary>
    /// Built-in command that shows a list of available commands.
    /// </summary>
    /// <param name="commandHandler">The command handler to use.</param>
    public class HelpCommand(CommandHandler commandHandler) : BaseCommand
    {
        private readonly CommandHandler _commandHandler = commandHandler;

        /// <summary>
        /// Sets the name of the command.
        /// </summary>
        public override string Name => "help";
        /// <summary>
        /// Sets the description of the command.
        /// </summary>
        public override string Description => "Shows list of available commands";

        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        public override void Execute(string args, GameState state)
        {
            state.GameLog.Add("Available commands:");
            foreach (ICommand cmd in _commandHandler.GetCommands())
            {
                state.GameLog.Add($"- {cmd.Name}: {cmd.Description}");
            }
        }
    }
}
