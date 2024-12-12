namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a barbarian player.
    /// </summary>
    public class Barbarian : Warrior
    {
        /// <summary>
        /// The level of berserk.
        /// </summary>
        public int BerserkLevel { get; set; } = 0;
        /// <summary>
        /// The level of rage.
        /// </summary>
        /// <param name="target">The target to attack.</param>
        /// <returns>The amount of damage dealt.</returns>
        public override int Attack(Person target)
        {
            int damage = base.Attack(target);
            return damage * (1 + BerserkLevel);
        }
    }
}