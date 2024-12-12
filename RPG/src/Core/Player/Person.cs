using RPG.Common;
using RPG.World.Data;
using System.Collections.Generic;

namespace RPG.Core.Player
{
    public abstract class Person
    {
        public string Name { get; set; } = "Unknown";
        public int Age { get; set; }
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public int HitPoints { get; set; }
        public int Gold { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public List<string> Inventory { get; set; } = [];
        public Dictionary<string, string> Equipment { get; set; } = [];
    }
}