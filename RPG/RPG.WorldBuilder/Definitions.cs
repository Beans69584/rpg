using System.Collections.Generic;
using RPG.Common;
using RPG.World.Data;

namespace RPG.WorldBuilder
{
    public class WorldDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Seed { get; set; } = "";
        public string Version { get; set; } = "1.0";
        public string RecommendedLevel { get; set; } = "";
        public string EstimatedPlaytime { get; set; } = "";
        public List<string> NPCFiles { get; set; } = [];
        public List<string> ItemFiles { get; set; } = [];
        public List<string> RegionFiles { get; set; } = [];
        public List<string> DialogueFiles { get; set; } = [];
        public List<string> QuestFiles { get; set; } = [];
        public StartingLocation StartingLocation { get; set; } = new();
        public List<string> StartingQuests { get; set; } = [];
        public StartingInventory StartingInventory { get; set; } = new();
        public List<QuestChain> QuestChains { get; set; } = [];
        public LevelScaling LevelScaling { get; set; } = new();
        public GameFlags GameFlags { get; set; } = new();
    }

    public class StartingLocation
    {
        public string Region { get; set; } = "";
        public string Position { get; set; } = "";
    }

    public class StartingInventory
    {
        public int Gold { get; set; }
        public List<StartingItem> Items { get; set; } = [];
    }

    public class StartingItem
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
    }

    public class QuestChain
    {
        public string Name { get; set; } = "";
        public List<string> Quests { get; set; } = [];
    }

    public class LevelScaling
    {
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public Dictionary<string, RegionScaling> Regions { get; set; } = [];
    }

    public class RegionScaling
    {
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
    }

    public class GameFlags
    {
        public List<string> Required { get; set; } = [];
    }

    public class RegionDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Vector2 Position { get; set; } = null!;
        public TerrainType Terrain { get; set; }
        public List<LocationDefinition> Locations { get; set; } = [];
    }

    public class LocationDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public LocationType Type { get; set; }
        public Vector2 Position { get; set; } = null!;
        public bool IsDiscovered { get; set; }
        public List<BuildingDefinition> Buildings { get; set; } = [];
    }

    public class BuildingDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public List<int> NPCs { get; set; } = [];
    }

    public class NpcDefinition
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public NPCRole Role { get; set; }
        public List<int> DialogueRefs { get; set; } = [];
        public EntityStats Stats { get; set; } = new();
    }

    public class ItemDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ItemStats Stats { get; set; } = new();
    }

    public class DialogueDefinition
    {
        public int Id { get; set; }
        public int RootNodeId { get; set; }
        public List<DialogueNodeDefinition> Nodes { get; set; } = [];
    }

    public class DialogueNodeDefinition
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
        public List<DialogueResponseDefinition> Responses { get; set; } = [];
        public List<DialogueActionDefinition>? Actions { get; set; }
    }

    public class DialogueResponseDefinition
    {
        public string Text { get; set; } = "";
        public int NextNodeId { get; set; }
        public List<string>? RequiredFlags { get; set; }
        public List<DialogueActionDefinition>? Actions { get; set; }
    }

    public class DialogueActionDefinition
    {
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class QuestDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public QuestType Type { get; set; }
        public int MinLevel { get; set; }
        public List<QuestObjectiveDefinition> Objectives { get; set; } = [];
        public List<QuestRewardDefinition> Rewards { get; set; } = [];
    }

    public class QuestObjectiveDefinition
    {
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public int Required { get; set; }
    }

    public class QuestRewardDefinition
    {
        public string Type { get; set; } = "";
        public int Value { get; set; }
        public string Description { get; set; } = "";
    }
}