namespace RPG.Commands
{
    public class LoadCommand : ICommand
    {
        public string Name => "load";
        public string Description => "Load game from a slot (1-5)";
        public string[] Aliases => new[] { "l" };

        public void Execute(string args, GameState state)
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
