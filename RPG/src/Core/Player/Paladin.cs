namespace RPG.Core.Player
{
    public class Paladin : Warrior
    {
        public int HolyPower { get; set; }

        public void Heal(Person target)
        {
            target.HitPoints += HolyPower;
        }

        public void Pray()
        {
            HolyPower += 1;
        }
    }
}