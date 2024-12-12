using System.Collections.Generic;
using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a conjurer player.
    /// </summary>
    public class Conjurer : Magician
    {
        /// <summary>
        /// The summoned animals of the conjurer.
        /// </summary>
        public List<Animal> SummonedAnimals { get; set; } = [];

        /// <summary>
        /// Summons an animal.
        /// </summary>
        /// <param name="type">The type of the animal to summon.</param>
        public void SummonAnimal(AnimalType type)
        {
            if (SummonedAnimals.Count < MagicPower / 10)
            {
                Animal newAnimal = new(type);
                SummonedAnimals.Add(newAnimal);
            }
        }
    }
}