using System.Collections.Generic;
using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    public class Conjurer : Magician
    {
        public List<Animal> SummonedAnimals { get; set; } = [];

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