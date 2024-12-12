using System.Collections.Generic;
using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a magician player.
    /// </summary>
    public class Magician : Person
    {
        /// <summary>
        /// The magic power of the magician.
        /// </summary>
        public int MagicPower { get; set; }
        /// <summary>
        /// The known spells of the magician.
        /// </summary>
        public List<Spell> KnownSpells { get; set; } = [];

        /// <summary>
        /// Casts a spell on a target.
        /// </summary>
        /// <param name="spell">The spell to cast.</param>
        /// <param name="target">The target of the spell.</param>
        public virtual void CastSpell(Spell spell, Person target)
        {
            if (KnownSpells.Contains(spell))
            {
                spell.Cast(this, target);
            }
        }
    }
}