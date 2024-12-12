using System;
using RPG.Core.Player;

namespace RPG.Core.Systems
{
    /// <summary>
    /// Represents the experience system of the game.
    /// </summary>
    public class ExperienceSystem(GameState state)
    {
        private readonly GameState _state = state;
        private static readonly int[] XpRequirements = new int[100];

        static ExperienceSystem()
        {
            // Initialise level 1 at 0 XP
            XpRequirements[0] = 0;

            // XP = baseXP * (level^2 - level + 600) / 200
            const int baseXP = 100;  // Base XP for early levels
            const double growthFactor = 1.1; // Slightly increasing curve

            for (int level = 1; level < 100; level++)
            {
                double levelModifier = Math.Pow(level, 2) - level + 600;
                double scaledXP = baseXP * levelModifier / 200;

                // Apply growth factor for higher levels
                if (level > 10)
                {
                    scaledXP *= Math.Pow(growthFactor, level - 10);
                }

                XpRequirements[level] = (int)Math.Round(scaledXP);
            }
        }

        /// <summary>
        /// Adds experience to the player and checks for level ups.
        /// </summary>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddExperience(int amount)
        {
            _state.CurrentExperience += amount;
            _state.GameLog.Add(new ColoredText($"Gained {amount} experience!", ConsoleColor.Green));

            // Check for level ups
            while (_state.Level < 100 && _state.CurrentExperience >= GetRequiredExperience(_state.Level))
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            _state.Level++;

            // Increase stats
            _state.MaxHP += 10;
            _state.HP = _state.MaxHP; // Heal on level up

            // Class-specific bonuses
            if (_state.CurrentPlayer is Warrior warrior)
            {
                warrior.CombatSkill += 2;
            }
            else if (_state.CurrentPlayer is Magician magician)
            {
                magician.MagicPower += 2;
            }
            else if (_state.CurrentPlayer is Sorcerer sorcerer)
            {
                sorcerer.MagicPower += 2;
            }

            _state.GameLog.Add(new ColoredText($"Level Up! You are now level {_state.Level}!", ConsoleColor.Yellow));
            _state.GameLog.Add(new ColoredText("Your health and abilities have increased!", ConsoleColor.Yellow));
        }

        /// <summary>
        /// Returns the required experience for the specified level.
        /// </summary>
        /// <param name="level">The level to get the required experience for.</param>
        /// <returns>The required experience for the specified level.</returns>
        public int GetRequiredExperience(int level)
        {
            return XpRequirements[Math.Min(level, XpRequirements.Length - 1)];
        }

        /// <summary>
        /// Returns the current level progress as a value between 0 and 1.
        /// </summary>
        public float GetLevelProgress()
        {
            int currentLevelXp = _state.Level == 1 ? 0 : GetRequiredExperience(_state.Level - 1);
            int nextLevelXp = GetRequiredExperience(_state.Level);
            int xpInCurrentLevel = _state.CurrentExperience - currentLevelXp;
            int xpRequiredForLevel = nextLevelXp - currentLevelXp;

            return (float)xpInCurrentLevel / xpRequiredForLevel;
        }
    }
}