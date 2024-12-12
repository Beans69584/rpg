namespace RPG.Core.Player.Common
{

    /// <summary>
    /// Represents a spell
    /// </summary>
    public class Spell
    {
        /// <summary>
        /// The name of the spell
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The power cost of the spell
        /// </summary>
        public int PowerCost { get; set; }

        /// <summary>
        /// Casts the spell on a target.
        /// </summary>
        /// <param name="caster">The caster of the spell.</param>
        /// <param name="target">The target of the spell.</param>
        public virtual void Cast(Magician caster, Person target)
        {
            // Not implemented
        }
    }
}