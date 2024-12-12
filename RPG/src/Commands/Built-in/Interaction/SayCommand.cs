using RPG.Core;

namespace RPG.Commands.Interaction
{
    /// <summary>
    /// Represents a command that allows players to speak in-game dialogue.
    /// </summary>
    public class SayCommand : BaseCommand
    {
        /// <summary>
        /// Gets the primary name of the command.
        /// </summary>
        /// <value>Returns "say" as the command identifier.</value>
        public override string Name => "say";

        /// <summary>
        /// Gets the description of the command's functionality.
        /// </summary>
        /// <value>Returns a brief explanation of the command's purpose.</value>
        public override string Description => "Say something";

        /// <summary>
        /// Gets the alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>Returns an array of aliases, including "s".</value>
        public override string[] Aliases => ["s"];

        /// <summary>
        /// Executes the say command, displaying the player's spoken dialogue in the game log.
        /// </summary>
        /// <param name="args">The text content that the player wishes to speak. Must not be empty.</param>
        /// <param name="state">The current game state containing player information and the game log.</param>
        /// <remarks>
        /// If no text is provided, a prompt asking "Say what?" will be displayed.
        /// The spoken text will be trimmed of leading and trailing whitespace.
        /// </remarks>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("Say what?");
                return;
            }

            state.GameLog.Add($"{state.PlayerName} says: {args.Trim()}");
        }
    }
}