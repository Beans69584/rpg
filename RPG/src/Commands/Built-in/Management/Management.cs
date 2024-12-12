using RPG.Core;

namespace RPG.Commands.Management
{
    public class ClearCommand : BaseCommand
    {
        public override string Name => "clear";
        public override string Description => "Clear the log";
        public override string[] Aliases => ["c"];

        public override void Execute(string args, GameState state)
        {
            state.GameLog.Clear();
        }
    }

    public class QuitCommand : BaseCommand
    {
        public override string Name => "exit";
        public override string Description => "Exit the game";
        public override string[] Aliases => ["q", "quit", "e"];

        public override void Execute(string args, GameState state)
        {
            state.Running = false;
        }
    }
}