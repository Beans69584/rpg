using RPG.Core.Player.Common;

namespace RPG.Core.Player
{
    public class Warrior : Person
    {
        public int CombatSkill { get; set; }
        public Weapon? EquippedWeapon { get; set; }

        public virtual int Attack(Person target)
        {
            int damage = CombatSkill + (EquippedWeapon?.Damage ?? 0);
            target.HitPoints -= damage;
            return damage;
        }
    }
}