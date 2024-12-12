using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a warrior in the game.
    /// </summary>
    /// <seealso cref="Person" />
    public class Warrior : Person
    {
        /// <summary>
        /// The combat skill of the warrior.
        /// </summary>
        public int CombatSkill { get; set; }
        /// <summary>
        /// The equipped weapon of the warrior.
        /// </summary>
        public Weapon? EquippedWeapon { get; set; }

        /// <summary>
        /// Attacks the specified target.
        /// </summary>
        /// <param name="target">The target to attack.</param>
        /// <returns>The amount of damage dealt.</returns>
        public virtual int Attack(Person target)
        {
            int damage = CombatSkill + (EquippedWeapon?.Damage ?? 0);
            target.HitPoints -= damage;
            return damage;
        }
    }
}