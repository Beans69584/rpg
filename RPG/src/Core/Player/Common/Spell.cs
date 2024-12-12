namespace RPG.Core.Player.Common
{

    public class Spell
    {
        public required string Name { get; set; }
        public int PowerCost { get; set; }

        public virtual void Cast(Magician caster, Person target)
        {
            // Implement spell effects
        }
    }
}