using RPG.Core;

namespace RPG.Commands.Builtin
{
    /// <summary>
    /// Command that loads a game from a save slot.
    /// </summary>
    public class LoadCommand : BaseCommand
    {
        /// <summary>
        /// Sets the name of the command.
        /// </summary>
        public override string Name => "load";
        /// <summary>
        /// Sets the description of the command.
        /// </summary>
        public override string Description => "Load game from a slot (1-5)";

        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args) || !int.TryParse(args, out int slot) || slot < 1 || slot > 5)
            {
                state.GameLog.Add("Usage: load <1-5>");
                return;
            }

            if (state.LoadGame(slot.ToString()))
            {
                state.GameLog.Add($"Game loaded from slot {slot}");
            }
            else
            {
                state.GameLog.Add($"No save found in slot {slot}");
            }
        }
    }
}
