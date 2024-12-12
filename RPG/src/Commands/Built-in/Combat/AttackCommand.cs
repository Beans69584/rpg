using RPG.Core;
using RPG.Core.Player;

namespace RPG.Commands.Combat
{
    public class AttackCommand : BaseCommand
    {
        public override string Name => "attack";
        public override string Description => "Attack a target";
        public override string[] Aliases => ["a"];

        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("Attack what?");
                return;
            }

            string target = args.Trim();
            int damage = CalculateDamage(state);

            // Add profession modifiers
            if (state.CurrentPlayer is Warrior)
                damage = (int)(damage * 1.2f);
            else if (state.CurrentPlayer is Barbarian)
                damage = (int)(damage * 1.5f);
            else if (state.CurrentPlayer is Sorcerer sorcerer)
            {
                sorcerer.EvilLevel++;
                damage = (int)(damage * 0.8f);
            }

            state.GameLog.Add($"You attack {target} for {damage} damage!");
        }

        private static int CalculateDamage(GameState state)
        {
            return state.Level * 10;
        }
    }
}