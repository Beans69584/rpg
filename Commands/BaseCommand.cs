namespace RPG.Commands
{
    /// <summary>
    /// Represents a command that can be executed by the player.
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// Gets a brief description of the command.
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// Gets a list of aliases for the command.
        /// </summary>
        public virtual string[] Aliases => [];
        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        public abstract void Execute(string args, GameState state);
    }
}
