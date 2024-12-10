using RPG.Core;

namespace RPG.Commands
{
    /// <summary>
    /// Interface implemented by all commands.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets a brief description of the command.
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Gets a list of aliases for the command.
        /// </summary>
        string[] Aliases { get; }
        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        void Execute(string args, GameState state);
    }
}
