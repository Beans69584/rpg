using System;
using System.Collections.Generic;

namespace RPG.World.Data
{
    public class Header
    {
        public string Magic { get; set; } = "RPGW";
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public string Version { get; set; } = "1.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Seed { get; set; } = "";
        public int RegionCount { get; set; }
        public int NPCCount { get; set; }
        public int ItemCount { get; set; }
    }

    public enum NPCRole
    {
        Villager, // 0
        Shopkeeper, // 1
        Innkeeper, // 2
        Guard, // 3
        QuestGiver, // 4
        Craftsman, // 5
        Priest, // 6
        Merchant, // 7
        Trainer, // 8
        Scholar // 9
    }

    public class Entity
    {
        public int NameId { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public NPCRole Role { get; set; }
        public ProfessionType Profession { get; set; }
        public EntityStats Stats { get; set; } = new();
        public Dictionary<string, int> ProfessionStats { get; set; } = [];
        public List<int> DialogueTreeRefs { get; set; } = [];
        public List<int> AvailableQuests { get; set; } = [];
        public List<int> CompletedQuests { get; set; } = [];
        public Dictionary<string, bool> Flags { get; set; } = [];
    }

    public enum ProfessionType
    {
        // Player professions
        Warrior, // 0
        Magician, // 1
        Paladin, // 2
        Barbarian, // 3
        Conjurer, // 4
        Sorcerer, // 5
        SithSorcerer, // 6

        // NPC professions
        Merchant_NPC, // 7
        Innkeeper_NPC, // 8
        Blacksmith_NPC // 9
    }

    public class DialogueTree
    {
        public int RootNodeId { get; set; }
        public Dictionary<int, DialogueNode> Nodes { get; set; } = [];
        public Dictionary<string, bool> Flags { get; set; } = [];
        public List<string> RequiredFlags { get; set; } = [];
    }

    public class DialogueNode
    {
        public int TextId { get; set; }
        public List<DialogueResponse> Responses { get; set; } = [];
        public List<DialogueAction> Actions { get; set; } = [];
        public List<string> RequiredFlags { get; set; } = [];
        public string? OnSelectScript { get; set; }
    }

    public class DialogueResponse
    {
        public int TextId { get; set; }
        public int NextNodeId { get; set; }
        public List<string> RequiredFlags { get; set; } = [];
        public List<DialogueAction> Actions { get; set; } = [];
        public string? Condition { get; set; }
    }

    public class DialogueAction
    {
        public string Type { get; set; } = ""; // SetFlag, GiveQuest, GiveItem, etc.
        public string Target { get; set; } = ""; // Flag name, Quest ID, Item ID, etc.
        public string Value { get; set; } = ""; // Flag value, quantity, etc.
    }

    public class Quest
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public int GiverId { get; set; }
        public QuestType Type { get; set; }
        public QuestStatus Status { get; set; }
        public List<QuestObjective> Objectives { get; set; } = [];
        public List<QuestStage> Stages { get; set; } = [];
        public List<Reward> Rewards { get; set; } = [];
        public Dictionary<string, bool> Flags { get; set; } = [];
        public List<string> RequiredFlags { get; set; } = [];
        public int MinLevel { get; set; }
        public List<int> PrerequisiteQuests { get; set; } = [];
        public List<int> FollowUpQuests { get; set; } = [];
    }

    public class QuestObjective
    {
        public int DescriptionId { get; set; }
        public string Type { get; set; } = ""; // Collect, Kill, Talk, Explore, etc.
        public string Target { get; set; } = ""; // Item ID, NPC ID, Location ID, etc.
        public int Required { get; set; }
        public int Current { get; set; }
        public bool IsComplete { get; set; }
        public List<Reward> Rewards { get; set; } = []; // Per-objective rewards
    }

    public class QuestStage
    {
        public int DescriptionId { get; set; }
        public List<int> DialogueTreeRefs { get; set; } = [];
        public List<string> RequiredFlags { get; set; } = [];
        public List<DialogueAction> CompletionActions { get; set; } = [];
    }

    public enum QuestType
    {
        Main, // 0
        Side, // 1
        Daily, // 2
        World, // 3
        Guild, // 4
        Reputation // 5
    }

    public enum QuestStatus
    {
        Available, // 0
        Active, // 1
        Complete, // 2
        Failed // 3
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
        public int Speed { get; set; }
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
        Weapon, // 0
        Armor, // 1
        Consumable, // 2
        Quest, // 3
        Misc // 4
    }
}