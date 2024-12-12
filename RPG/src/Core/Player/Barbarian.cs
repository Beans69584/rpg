namespace RPG.Core.Player
{
    public class Barbarian : Warrior
    {
        public int BerserkLevel { get; set; } = 0;
        public override int Attack(Person target)
        {
            int damage = base.Attack(target);
            return damage * (1 + BerserkLevel);
        }
    }
}