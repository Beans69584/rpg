using RPG.Core;

namespace RPG.Commands.Character
{
    /// <summary>
    /// Represents a command that allows players to rename their character.
    /// </summary>
    public class RenameCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command, which is used to invoke it.
        /// </summary>
        /// <value>The string "rename".</value>
        public override string Name => "rename";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>A string explaining the command's purpose.</value>
        public override string Description => "Rename your character";

        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array of strings containing alternative command names.</value>
        public override string[] Aliases => ["name"];

        /// <summary>
        /// Executes the rename command, changing the player character's name to the specified value.
        /// </summary>
        /// <param name="args">The new name to assign to the character. Must not be empty or whitespace.</param>
        /// <param name="state">The current game state containing the player's information.</param>
        /// <remarks>
        /// If the provided name is empty or consists only of whitespace, an error message will be added to the game log.
        /// Otherwise, the player's name will be updated and a confirmation message will be displayed.
        /// </remarks>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("You must enter a name!");
                return;
            }

            string newName = args.Trim();
            state.PlayerName = newName;
            state.GameLog.Add($"You are now known as {newName}");
        }
    }
}