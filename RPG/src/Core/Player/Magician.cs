using System.Collections.Generic;
using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    public class Magician : Person
    {
        public int MagicPower { get; set; }
        public List<Spell> KnownSpells { get; set; } = [];

        public virtual void CastSpell(Spell spell, Person target)
        {
            if (KnownSpells.Contains(spell))
            {
                spell.Cast(this, target);
            }
        }
    }
}