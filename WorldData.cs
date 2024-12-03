using System.Text.Json.Serialization;

namespace RPG
{
    public class WorldRegionBase
    {
        public int NameId { get; set; }
        public int DescriptionId { get; set; }
        public List<int> Connections { get; set; } = new();
        public List<int> NPCs { get; set; } = new();
        public List<int> Items { get; set; } = new();
        public Vector2 Position { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = new();
    }

    public class WorldRegion : WorldRegionBase
    {
        // No need to redeclare properties as they're inherited from WorldRegionBase
    }

    public class WorldData
    {
        public Header Header { get; set; } = new();
        public ResourceTable Resources { get; set; } = new();
        public List<WorldRegion> Regions { get; set; } = new();  // Changed from Region to WorldRegionBase
        public List<Entity> NPCs { get; set; } = new();
        public List<Item> Items { get; set; } = new();
    }
}
