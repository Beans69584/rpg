using System;
using System.Collections.Generic;

namespace RPG.World.Data
{
    public class Header
    {
        public string Magic { get; set; } = "RPGW";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "1.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Seed { get; set; } = "";
        public int RegionCount { get; set; }
        public int NPCCount { get; set; }
        public int ItemCount { get; set; }
    }

    public class ResourceTable
    {
        public Dictionary<string, int> StringPool { get; set; } = [];
        public Dictionary<string, int> TextureRefs { get; set; } = [];
        public Dictionary<string, int> SoundRefs { get; set; } = [];
        public List<string> SharedDialogue { get; set; } = [];
    }

    public class Entity
    {
        public int NameId { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public List<int> DialogueRefs { get; set; } = [];
        public EntityStats Stats { get; set; } = new();
    }

    public class Item
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public ItemStats Stats { get; set; } = new();
    }

    public class RoutePoint
    {
        public int DescriptionId { get; set; }
        public int DirectionsId { get; set; }
        public List<Landmark> Landmarks { get; set; } = [];
    }

    public class EntityStats
    {
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int Defense { get; set; }
    }

    public class ItemStats
    {
        public int Value { get; set; }
        public int Weight { get; set; }
        public int Durability { get; set; }
        public ItemType Type { get; set; }
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Quest,
        Misc
    }
}