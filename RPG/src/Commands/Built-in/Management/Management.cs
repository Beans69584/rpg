using RPG.Core;

namespace RPG.Commands.Management
{
    /// <summary>
    /// Represents a command to clear the game log.
    /// </summary>
    public class ClearCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>The string "clear".</value>
        public override string Name => "clear";

        /// <summary>
        /// Gets the description of what the command does.
        /// </summary>
        /// <value>A description explaining the command clears the log.</value>
        public override string Description => "Clear the log";

        /// <summary>
        /// Gets the alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array containing the alias "c".</value>
        public override string[] Aliases => ["c"];

        /// <summary>
        /// Executes the clear command, removing all entries from the game log.
        /// </summary>
        /// <param name="args">The arguments passed to the command (not used).</param>
        /// <param name="state">The current game state containing the log to clear.</param>
        public override void Execute(string args, GameState state)
        {
            state.GameLog.Clear();
        }
    }

    /// <summary>
    /// Represents a command to exit the game.
    /// </summary>
    public class QuitCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>The string "exit".</value>
        public override string Name => "exit";

        /// <summary>
        /// Gets the description of what the command does.
        /// </summary>
        /// <value>A description explaining the command exits the game.</value>
        public override string Description => "Exit the game";

        /// <summary>
        /// Gets the alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array containing the aliases "q", "quit", and "e".</value>
        public override string[] Aliases => ["q", "quit", "e"];

        /// <summary>
        /// Executes the quit command, setting the game's running state to false.
        /// </summary>
        /// <param name="args">The arguments passed to the command (not used).</param>
        /// <param name="state">The current game state to modify.</param>
        public override void Execute(string args, GameState state)
        {
            state.Running = false;
        }
    }
}