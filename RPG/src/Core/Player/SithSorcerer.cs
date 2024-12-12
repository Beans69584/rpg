namespace RPG.Core.Player
{
    public class SithSorcerer : Sorcerer
    {
        public int DarkPower { get; set; } = 100;

        public SithSorcerer()
        {
            EvilLevel = 100; // Use the base class property
        }
    }
}