using System.Collections.Generic;
using System.Linq;
using RPG.Common;

namespace RPG.World.Data
{
    /// <summary>
    /// Represents different types of terrain in the world.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>
        /// Flat, open grasslands with minimal vegetation.
        /// </summary>
        Plains,

        /// <summary>
        /// Dense woodland areas with heavy tree coverage.
        /// </summary>
        Forest,

        /// <summary>
        /// High elevation rocky terrain with steep slopes.
        /// </summary>
        Mountain,

        /// <summary>
        /// Arid regions with sand dunes and minimal vegetation.
        /// </summary>
        Desert,

        /// <summary>
        /// Wetlands with standing water and dense vegetation.
        /// </summary>
        Swamp,

        /// <summary>
        /// Shoreline areas where land meets water bodies.
        /// </summary>
        Coast,

        /// <summary>
        /// Rolling elevated terrain with gradual slopes.
        /// </summary>
        Hills,

        /// <summary>
        /// Deep natural ravines with steep walls.
        /// </summary>
        Canyon,

        /// <summary>
        /// Flowing water bodies that traverse the landscape.
        /// </summary>
        River
    }

    /// <summary>
    /// Represents different types of locations that can exist in the world.
    /// </summary>
    public enum LocationType
    {
        /// <summary>
        /// Large settled area with significant population and infrastructure.
        /// </summary>
        Town,

        /// <summary>
        /// Small settled area with modest population and basic amenities.
        /// </summary>
        Village,

        /// <summary>
        /// Underground complex often containing hostile creatures and treasures.
        /// </summary>
        Dungeon,

        /// <summary>
        /// Natural or artificial underground chamber system.
        /// </summary>
        Cave,

        /// <summary>
        /// Remnants of abandoned or destroyed structures.
        /// </summary>
        Ruin,

        /// <summary>
        /// Notable natural or artificial feature of the landscape.
        /// </summary>
        Landmark,

        /// <summary>
        /// Temporary or semi-permanent settlement area.
        /// </summary>
        Camp,

        /// <summary>
        /// Military or guard post for monitoring territory.
        /// </summary>
        Outpost,

        /// <summary>
        /// Religious or sacred structure dedicated to worship.
        /// </summary>
        Temple,

        /// <summary>
        /// Large body of still water surrounded by land.
        /// </summary>
        Lake,

        /// <summary>
        /// Highest point of a mountain or hill.
        /// </summary>
        Peak
    }

    /// <summary>
    /// Provides extension methods for the LocationType enum.
    /// </summary>
    public static class LocationTypeExtensions
    {
        /// <summary>
        /// Converts a LocationType to its friendly string representation.
        /// </summary>
        /// <param name="type">The LocationType to convert.</param>
        /// <returns>A human-readable string representation of the location type.</returns>
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

    /// <summary>
    /// Represents different types of encounters that can occur.
    /// </summary>
    public enum EncounterType
    {
        /// <summary>
        /// Hostile engagement with enemies requiring battle.
        /// </summary>
        Combat,

        /// <summary>
        /// Interaction with non-player characters.
        /// </summary>
        NPC,

        /// <summary>
        /// Special occurrence that affects gameplay.
        /// </summary>
        Event,

        /// <summary>
        /// Finding of new locations, items, or information.
        /// </summary>
        Discovery
    }

    /// <summary>
    /// Represents a distinct region in the world with its own characteristics and contents.
    /// </summary>
    public class WorldRegion
    {
        /// <summary>
        /// Gets or sets the unique identifier for the region's name in the string pool.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the region's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the list of connected region identifiers.
        /// </summary>
        public List<int> Connections { get; set; } = [];

        /// <summary>
        /// Gets or sets the primary terrain type of the region.
        /// </summary>
        public TerrainType Terrain { get; set; }

        /// <summary>
        /// Gets or sets the position of the region in the world.
        /// </summary>
        public Vector2 Position { get; set; } = new();

        /// <summary>
        /// Gets or sets the detailed terrain mapping of the region.
        /// </summary>
        public Dictionary<Vector2, TerrainType> TerrainMap { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of routes within the region.
        /// </summary>
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of locations within the region.
        /// </summary>
        public List<Location> Locations { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of NPC identifiers present in the region.
        /// </summary>
        public List<int> NPCs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of item identifiers present in the region.
        /// </summary>
        public List<int> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the exploration progress as a percentage.
        /// </summary>
        public float ExplorationProgress { get; set; }

        /// <summary>
        /// Gets or sets the list of flags associated with the region.
        /// </summary>
        public List<string> Flags { get; set; } = [];
    }

    /// <summary>
    /// Represents a traversable path between locations.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Gets or sets the unique identifier for the route's name in the string pool.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the segments that make up this route.
        /// </summary>
        public List<RouteSegment> Segments { get; set; } = [];

        /// <summary>
        /// Gets or sets the total length of the route.
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// Gets or sets the list of possible encounters along this route.
        /// </summary>
        public List<Encounter> PossibleEncounters { get; set; } = [];

        /// <summary>
        /// Gets or sets the difficulty rating for traversing this route.
        /// </summary>
        public float DifficultyRating { get; set; }

        /// <summary>
        /// Gets or sets the list of points that define the route's path.
        /// </summary>
        public List<Vector2> PathPoints { get; set; } = [];

        /// <summary>
        /// Gets or sets the primary terrain type of the route.
        /// </summary>
        public TerrainType TerrainType { get; set; }
    }

    /// <summary>
    /// Represents a segment of a route with its own characteristics.
    /// </summary>
    public class RouteSegment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the segment's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the segment's directions in the string pool.
        /// </summary>
        public int DirectionsId { get; set; }

        /// <summary>
        /// Gets or sets the list of landmarks present in this segment.
        /// </summary>
        public List<Landmark> Landmarks { get; set; } = [];

        /// <summary>
        /// Gets or sets the encounter rate for this segment.
        /// </summary>
        public float EncounterRate { get; set; }

        /// <summary>
        /// Gets or sets the list of encounters specific to this segment.
        /// </summary>
        public List<Encounter> SegmentSpecificEncounters { get; set; } = [];
    }

    /// <summary>
    /// Represents a location within a region.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Gets or sets the unique identifier for the location's name in the string pool.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the location's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        public LocationType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the location is discovered.
        /// </summary>
        public bool IsDiscovered { get; set; }

        /// <summary>
        /// Gets or sets the list of NPC identifiers present in the location.
        /// </summary>
        public List<int> NPCs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of item identifiers present in the location.
        /// </summary>
        public List<int> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of quests available at the location.
        /// </summary>
        public List<Quest> Quests { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rewards for exploring the location.
        /// </summary>
        public List<Reward> ExplorationRewards { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of flags associated with the location.
        /// </summary>
        public Dictionary<string, bool> Flags { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of buildings present in the location.
        /// </summary>
        public List<Building> Buildings { get; set; } = [];

        /// <summary>
        /// Gets or sets the importance rating of the location.
        /// </summary>
        public float ImportanceRating { get; set; }

        /// <summary>
        /// Gets or sets the local terrain type of the location.
        /// </summary>
        public TerrainType LocalTerrain { get; set; }

        /// <summary>
        /// Gets or sets the position of the location within the region.
        /// </summary>
        public Vector2 Position { get; set; } = new();

        /// <summary>
        /// Gets or sets the current building the player is in, if any.
        /// </summary>
        public Building? CurrentBuilding { get; set; }
    }

    /// <summary>
    /// Represents a building within a location.
    /// </summary>
    public class Building
    {
        /// <summary>
        /// Gets or sets the unique identifier for the building's name in the string pool.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the building's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the type of the building.
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of NPC identifiers present in the building.
        /// </summary>
        public List<int> NPCs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of item identifiers present in the building.
        /// </summary>
        public List<int> Items { get; set; } = [];
    }

    /// <summary>
    /// Represents an encounter that can occur in the world.
    /// </summary>
    public class Encounter
    {
        /// <summary>
        /// Gets or sets the type of the encounter.
        /// </summary>
        public EncounterType Type { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the encounter's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the probability of the encounter occurring.
        /// </summary>
        public float Probability { get; set; }

        /// <summary>
        /// Gets or sets the list of conditions required for the encounter to occur.
        /// </summary>
        public List<string> Conditions { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rewards for completing the encounter.
        /// </summary>
        public List<Reward> Rewards { get; set; } = [];
    }

    /// <summary>
    /// Represents a reward that can be obtained in the world.
    /// </summary>
    public class Reward
    {
        /// <summary>
        /// Gets or sets the type of the reward.
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the reward.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the reward's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }
    }

    /// <summary>
    /// Represents a landmark within a region.
    /// </summary>
    public class Landmark
    {
        /// <summary>
        /// Gets or sets the unique identifier for the landmark's name in the string pool.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the landmark's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the type of the landmark.
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the position of the landmark within the region.
        /// </summary>
        public Vector2 Position { get; set; } = new();
    }

    /// <summary>
    /// Represents the data for the entire world.
    /// </summary>
    public class WorldData
    {
        /// <summary>
        /// Gets or sets the header information for the world data.
        /// </summary>
        public Header Header { get; set; } = new();

        /// <summary>
        /// Gets or sets the resource table containing various resources used in the world.
        /// </summary>
        public ResourceTable Resources { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of regions in the world.
        /// </summary>
        public List<WorldRegion> Regions { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of NPCs in the world.
        /// </summary>
        public List<Entity> NPCs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of items in the world.
        /// </summary>
        public List<Item> Items { get; set; } = [];

        /// <summary>
        /// Gets the string associated with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the string to retrieve.</param>
        /// <returns>The string associated with the specified identifier.</returns>
        public string GetString(int id)
        {
            return Resources.StringPool.FirstOrDefault(x => x.Value == id).Key ?? $"<unknown string {id}>";
        }

        /// <summary>
        /// Adds a string to the string pool and returns its identifier.
        /// </summary>
        /// <param name="value">The string to add.</param>
        /// <returns>The identifier of the added string.</returns>
        public int AddString(string value)
        {
            if (Resources.StringPool.ContainsKey(value))
            {
                return Resources.StringPool[value];
            }

            int id = Resources.StringPool.Count;
            Resources.StringPool[value] = id;
            return id;
        }
    }

    /// <summary>
    /// Represents a table of resources used in the world.
    /// </summary>
    public class ResourceTable
    {
        /// <summary>
        /// Gets or sets the dictionary of strings and their identifiers.
        /// </summary>
        public Dictionary<string, int> StringPool { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of texture references and their identifiers.
        /// </summary>
        public Dictionary<string, int> TextureRefs { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of sound references and their identifiers.
        /// </summary>
        public Dictionary<string, int> SoundRefs { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of dialogue trees and their identifiers.
        /// </summary>
        public Dictionary<int, DialogueTree> DialogueTrees { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of quests and their identifiers.
        /// </summary>
        public Dictionary<int, Quest> Quests { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary of scripts and their identifiers.
        /// </summary>
        public Dictionary<string, string> Scripts { get; set; } = [];
    }
}