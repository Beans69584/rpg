using System;
using System.Collections.Generic;
using RPG.Core.Player;
using RPG.World.Data;

namespace RPG.Core.Player.Common
{
    /// <summary>
    /// Factory for creating characters.
    /// </summary>
    public class CharacterFactory(GameState gameState)
    {
        private readonly GameState gameState = gameState;

        /// <summary>
        /// Creates a character.
        /// </summary>
        /// <param name="characterClass">The class of the character.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="attributes">The attributes of the character.</param>
        /// <returns>The created character.</returns>
        public Person CreateCharacter(string characterClass, string name, Dictionary<string, int> attributes)
        {
            Person character = characterClass.ToLower() switch
            {
                "warrior" => new Warrior(),
                "barbarian" => new Barbarian(),
                "paladin" => new Paladin(),
                "sorcerer" => new Sorcerer(),
                "conjurer" => new Conjurer(),
                "sithsorcerer" => new SithSorcerer(),
                _ => throw new System.ArgumentException($"Unknown character class: {characterClass}")
            };

            // Initialise base properties
            character.Name = name;
            character.Age = 20;
            character.HitPoints = 100;
            character.Gold = 100;

            // Set attributes from parameter
            character.Strength = attributes["Strength"];
            character.Dexterity = attributes["Dexterity"];
            character.Constitution = attributes["Constitution"];
            character.Intelligence = attributes["Intelligence"];
            character.Wisdom = attributes["Wisdom"];
            character.Charisma = attributes["Charisma"];

            switch (character)
            {
                case Paladin paladin:
                    paladin.CombatSkill = 40;
                    paladin.HolyPower = 30;
                    break;
                case Barbarian barbarian:
                    barbarian.CombatSkill = 60;
                    barbarian.BerserkLevel = 1;
                    break;
                case Warrior warrior:
                    warrior.CombatSkill = 50;
                    break;
                case Conjurer conjurer:
                    conjurer.MagicPower = 40;
                    break;
                case Sorcerer sorcerer:
                    sorcerer.Initialise(gameState);
                    sorcerer.MagicPower = 50;
                    break;
            }

            return character;
        }
    }
}
