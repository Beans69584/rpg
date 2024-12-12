using System.Threading.Tasks;
using RPG.Combat;
using RPG.Core;
using RPG.Common;
using RPG.World.Data;

namespace RPG.Commands.Combat
{
    /// <summary>
    /// Represents a command that initiates combat encounters with nearby enemies.
    /// </summary>
    public class FightCommand(GameState state) : ICommand
    {
        private readonly CombatSystem _combatSystem = new(state);

        /// <summary>
        /// Gets the name of the command used to trigger combat.
        /// </summary>
        /// <value>The string "fight".</value>
        public string Name => "fight";
        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>A description explaining the command's purpose.</value>
        public string Description => "Initiate combat with nearby enemies";
        /// <summary>
        /// Gets the proper usage syntax for the command.
        /// </summary>
        /// <value>The proper format for using the command.</value>
        public string Usage => "fight";
        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array of strings containing valid command aliases.</value>
        public string[] Aliases => ["combat"];

        /// <summary>
        /// Executes the fight command, initiating combat with enemies in the vicinity.
        /// Currently creates a test enemy (Goblin) for development purposes.
        /// </summary>
        /// <param name="args">The arguments passed with the command (currently unused).</param>
        /// <param name="state">The current game state containing player and world information.</param>
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