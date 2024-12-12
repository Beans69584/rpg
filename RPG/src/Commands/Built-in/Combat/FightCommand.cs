using System.Threading.Tasks;
using RPG.Combat;
using RPG.Core;
using RPG.Common;
using RPG.World.Data;

namespace RPG.Commands.Combat
{
    public class FightCommand(GameState state) : ICommand
    {
        private readonly CombatSystem _combatSystem = new(state);

        public string Name => "fight";
        public string Description => "Initiate combat with nearby enemies";
        public string Usage => "fight";
        public string[] Aliases => ["combat"];

        public void Execute(string args, GameState state)
        {
            // For testing, create a dummy enemy
            Entity enemy = new()
            {
                NameId = state.World?.GetWorldData()?.AddString("Goblin") ?? 0,
                Stats = new EntityStats
                {
                    Strength = 3,
                    Dexterity = 4,
                    Intelligence = 1,
                    Defense = 2,
                    Speed = 5
                }
            };

            _combatSystem.StartCombatAsync([enemy]).Wait();
        }
    }
}