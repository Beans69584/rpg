using System.Collections.Generic;
using System.Text.Json.Serialization;

using RPG.World.Generation;

namespace RPG.World.Data
{
    /// <summary>
    /// Represents a region in the game world with its properties and connections
    /// </summary>
    public class WorldRegion
    {
        /// <summary>
        /// Gets or sets the identifier for the region name
        /// </summary>
        public int NameId { get; set; }
        /// <summary>
        /// Gets or sets the identifier for the region description
        /// </summary>
        public int DescriptionId { get; set; }
        /// <summary>
        /// Gets or sets the list of connected region identifiers
        /// </summary>
        public List<int> Connections { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of NPC identifiers in this region
        /// </summary>
        public List<int> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of item identifiers in this region
        /// </summary>
        public List<int> Items { get; set; } = [];
        /// <summary>
        /// Gets or sets the position of this region in the world
        /// </summary>
        public Vector2 Position { get; set; } = new();
        /// <summary>
        /// Gets or sets the list of locations within this region
        /// </summary>
        public List<Location> Locations { get; set; } = [];
        /// <summary>
        /// Gets or sets the dictionary of routes in this region
        /// </summary>
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = [];
    }

    /// <summary>
    /// Contains all data related to the game world
    /// </summary>
    public class WorldData
    {
        /// <summary>
        /// Gets or sets the header information
        /// </summary>
        public Header Header { get; set; } = new();
        /// <summary>
        /// Gets or sets the resource table
        /// </summary>
        public ResourceTable Resources { get; set; } = new();
        /// <summary>
        /// Gets or sets the list of world regions
        /// </summary>
        public List<WorldRegion> Regions { get; set; } = [];  // Changed from Region to WorldRegionBase
        /// <summary>
        /// Gets or sets the list of NPCs in the world
        /// </summary>
        public List<Entity> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of items in the world
        /// </summary>
        public List<Item> Items { get; set; } = [];
    }
}
