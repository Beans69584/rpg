using System.Collections.Generic;
using System.Linq;
using RPG.Common;

namespace RPG.World.Data
{
    public enum TerrainType
    {
        Plains, // 0
        Forest, // 1
        Mountain, // 2
        Desert, // 3
        Swamp, // 4
        Coast, // 5
        Hills, // 6
        Canyon, // 7
        River // 8
    }

    public enum LocationType
    {
        Town, // 0
        Village, // 1
        Dungeon, // 2
        Cave, // 3
        Ruin, // 4
        Landmark, // 5
        Camp, // 6
        Outpost, // 7
        Temple, // 8
        Lake, // 9
        Peak // 10
    }

    public static class LocationTypeExtensions
    {
        public static string ToFriendlyString(this LocationType type)
        {
            return type switch
            {
                LocationType.Town => "Town",
                LocationType.Village => "Village",
                LocationType.Dungeon => "Dungeon",
                LocationType.Cave => "Cave",
                LocationType.Ruin => "Ruin",
                LocationType.Landmark => "Landmark",
                LocationType.Camp => "Camp",
                LocationType.Outpost => "Outpost",
                LocationType.Temple => "Temple",
                LocationType.Lake => "Lake",
                LocationType.Peak => "Peak",
                _ => "Unknown"
            };
        }
    }

    public enum EncounterType
    {
        Combat, // 0
        NPC, // 1
        Event, // 2
        Discovery // 3
    }

    public class WorldRegion
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public List<int> Connections { get; set; } = [];
        public TerrainType Terrain { get; set; }
        public Vector2 Position { get; set; } = new();
        public Dictionary<Vector2, TerrainType> TerrainMap { get; set; } = [];
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = [];
        public List<Location> Locations { get; set; } = [];
        public List<int> NPCs { get; set; } = [];
        public List<int> Items { get; set; } = [];
        public float ExplorationProgress { get; set; }
        public List<string> Flags { get; set; } = [];
    }

    public class Route
    {
        public int NameId { get; set; }
        public List<RouteSegment> Segments { get; set; } = [];
        public float Length { get; set; }
        public List<Encounter> PossibleEncounters { get; set; } = [];
        public float DifficultyRating { get; set; }
        public List<Vector2> PathPoints { get; set; } = [];
        public TerrainType TerrainType { get; set; }
    }

    public class RouteSegment
    {
        public int DescriptionId { get; set; }
        public int DirectionsId { get; set; }
        public List<Landmark> Landmarks { get; set; } = [];
        public float EncounterRate { get; set; }
        public List<Encounter> SegmentSpecificEncounters { get; set; } = [];
    }

    public class Location
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public LocationType Type { get; set; }
        public bool IsDiscovered { get; set; }
        public List<int> NPCs { get; set; } = [];
        public List<int> Items { get; set; } = [];
        public List<Quest> Quests { get; set; } = [];
        public List<Reward> ExplorationRewards { get; set; } = [];
        public Dictionary<string, bool> Flags { get; set; } = [];
        public List<Building> Buildings { get; set; } = [];
        public float ImportanceRating { get; set; }
        public TerrainType LocalTerrain { get; set; }
        public Vector2 Position { get; set; } = new();
        public Building? CurrentBuilding { get; set; }
    }

    public class Building
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public string Type { get; set; } = "";
        public List<int> NPCs { get; set; } = [];
        public List<int> Items { get; set; } = [];
    }

    public class Encounter
    {
        public EncounterType Type { get; set; }
        public int DescriptionId { get; set; }
        public float Probability { get; set; }
        public List<string> Conditions { get; set; } = [];
        public List<Reward> Rewards { get; set; } = [];
    }

    public class Reward
    {
        public string Type { get; set; } = "";
        public int Value { get; set; }
        public int DescriptionId { get; set; }
    }

    public class Landmark
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public string Type { get; set; } = "";
        public Vector2 Position { get; set; } = new();
    }

    public class WorldData
    {
        public Header Header { get; set; } = new();
        public ResourceTable Resources { get; set; } = new();
        public List<WorldRegion> Regions { get; set; } = [];
        public List<Entity> NPCs { get; set; } = [];
        public List<Item> Items { get; set; } = [];

        public string GetString(int id)
        {
            return Resources.StringPool.FirstOrDefault(x => x.Value == id).Key ?? $"<unknown string {id}>";
        }
    }

    public class ResourceTable
    {
        public Dictionary<string, int> StringPool { get; set; } = [];
        public Dictionary<string, int> TextureRefs { get; set; } = [];
        public Dictionary<string, int> SoundRefs { get; set; } = [];
        public Dictionary<int, DialogueTree> DialogueTrees { get; set; } = [];
        public Dictionary<int, Quest> Quests { get; set; } = [];
        public Dictionary<string, string> Scripts { get; set; } = [];
    }
}