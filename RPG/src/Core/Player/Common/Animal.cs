namespace RPG.Core.Player.Common
{
    public class Animal(AnimalType type)
    {
        public AnimalType Type { get; } = type;
        public int Strength { get; set; }
    }

    public enum AnimalType
    {
        Wolf,
        Bear,
        Eagle,
        Snake
    }
}