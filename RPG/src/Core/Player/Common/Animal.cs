namespace RPG.Core.Player.Common
{
    /// <summary>
    /// Represents a creature that can accompany the player throughout their journey.
    /// </summary>
    public class Animal(AnimalType type)
    {
        /// <summary>
        /// Gets the species classification of the animal.
        /// </summary>
        public AnimalType Type { get; } = type;

        /// <summary>
        /// Gets or sets the physical power of the animal, determining its combat effectiveness.
        /// </summary>
        public int Strength { get; set; }
    }

    /// <summary>
    /// Defines the available species of animals that can accompany the player.
    /// </summary>
    public enum AnimalType
    {
        /// <summary>
        /// Represents a wolf.
        /// </summary>
        Wolf,
        /// <summary>
        /// Represents a bear.
        /// </summary>
        Bear,
        /// <summary>
        /// Represents an eagle.
        /// </summary>
        Eagle,
        /// <summary>
        /// Represents a snake.
        /// </summary>
        Snake
    }
}