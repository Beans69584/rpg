using RPG.Core;

namespace RPG.Commands.Interaction
{
    public class SayCommand : BaseCommand
    {
        public override string Name => "say";
        public override string Description => "Say something";
        public override string[] Aliases => ["s"];

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