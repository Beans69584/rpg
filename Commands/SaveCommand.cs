using RPG;

namespace RPG.Commands
{
    public class SaveCommand : ICommand
    {
        public string Name => "save";
        public string Description => "Save game to a slot (1-5)";
        public string[] Aliases => new[] { "s" };

        public void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args) || !int.TryParse(args, out int slot) || slot < 1 || slot > 5)
            {
                state.GameLog.Add("Usage: save <1-5>");
                return;
            }

            state.SaveGame(slot.ToString());
        }
    }
}
