using RPG.Core;

namespace RPG.Commands.Character
{
    public class RenameCommand : BaseCommand
    {
        public override string Name => "rename";
        public override string Description => "Rename your character";
        public override string[] Aliases => ["name"];

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