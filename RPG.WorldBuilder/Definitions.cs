using RPG.Common;
using RPG.World.Data;

namespace RPG.WorldBuilder;

public class WorldDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Seed { get; set; } = "";
    public List<string> NPCFiles { get; set; } = [];
    public List<string> ItemFiles { get; set; } = [];
    public List<string> RegionFiles { get; set; } = [];
    public List<string> DialogueFiles { get; set; } = [];
    public List<string> QuestFiles { get; set; } = [];
}

public class DialogueDefinition
{
    public int Id { get; set; }
    public List<DialogueNodeDefinition> Nodes { get; set; } = [];
    public int RootNodeId { get; set; }
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
    public List<RewardDefinition> Rewards { get; set; } = [];
}

public class QuestObjectiveDefinition
{
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public int Required { get; set; }
}

public class RewardDefinition
{
    public string Type { get; set; } = "";
    public int Value { get; set; }
    public string Description { get; set; } = "";
}

public class NPCDefinition
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public NPCRole Role { get; set; }
    public List<int> DialogueRefs { get; set; } = [];
    public EntityStats Stats { get; set; } = new();
    public ProfessionType Profession { get; set; }
    public Dictionary<string, int> ProfessionStats { get; set; } = [];
    public List<string> Abilities { get; set; } = [];
    public string SpeechPattern { get; set; } = "normal";
}

public class ItemDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ItemStats Stats { get; set; } = new();
}

public class RegionDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Vector2 Position { get; set; } = new();
    public TerrainType Terrain { get; set; }
    public List<LocationDefinition> Locations { get; set; } = [];
}

public class LocationDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public LocationType Type { get; set; }
    public Vector2 Position { get; set; } = new();
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