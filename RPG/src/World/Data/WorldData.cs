using System.Collections.Generic;
using RPG.Common;
using RPG.World.Generation;

namespace RPG.World.Data
{
    public enum TerrainType
    {
        Plains,
        Forest,
        Mountain,
        Desert,
        Swamp,
        Coast,
        Hills,
        Canyon,
        River
    }

    public enum LocationType
    {
        Town,
        Village,
        Dungeon,
        Cave,
        Ruin,
        Landmark,
        Camp,
        Outpost,
        Temple,
        Lake,
        Peak
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
        Combat,
        NPC,
        Event,
        Discovery
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

    public class Quest
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public List<string> Objectives { get; set; } = [];
        public List<Reward> Rewards { get; set; } = [];
        public bool IsComplete { get; set; }
        public Dictionary<string, bool> Flags { get; set; } = [];
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
        public Dictionary<string, int> StringPool { get; set; } = [];
    }
}