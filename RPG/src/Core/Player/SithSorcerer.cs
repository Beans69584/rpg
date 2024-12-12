namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a Sith Sorcerer
    /// </summary>
    public class SithSorcerer : Sorcerer
    {
        /// <summary>
        /// The level of evilness
        /// </summary>
        public int DarkPower { get; set; } = 100;

        /// <summary>
        /// Initialises a new instance of the <see cref="SithSorcerer"/> class.
        /// </summary>
        public SithSorcerer()
        {
            EvilLevel = 100;
        }
    }
}