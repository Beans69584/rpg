namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a Paladin
    /// </summary>
    public class Paladin : Warrior
    {
        /// <summary>
        /// The holy power of the paladin
        /// </summary>
        public int HolyPower { get; set; }

        /// <summary>
        /// Heals the specified target.
        /// </summary>
        /// <param name="target">The target to heal.</param>
        public void Heal(Person target)
        {
            target.HitPoints += HolyPower;
        }

        /// <summary>
        /// Prays to increase the holy power.
        /// </summary>
        public void Pray()
        {
            HolyPower += 1;
        }
    }
}