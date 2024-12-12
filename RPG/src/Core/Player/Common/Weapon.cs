namespace RPG.Core.Player.Common
{
    /// <summary>
    /// Represents a weapon
    /// </summary>
    public class Weapon
    {
        /// <summary>
        /// The name of the weapon
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The damage of the weapon
        /// </summary>
        public int Damage { get; set; }
        /// <summary>
        /// The value of the weapon
        /// </summary>
        public int Value { get; set; }
    }
}