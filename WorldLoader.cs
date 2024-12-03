using System.IO.Compression;
using System.Text.Json;

namespace RPG
{
    public class WorldLoader
    {
        private readonly WorldData _worldData;
        private readonly Dictionary<int, string> _stringCache;

        public WorldLoader(string worldPath)
        {
            // Read and decompress world file
            using var fs = File.OpenRead(worldPath);
            using var gzip = new GZipStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            gzip.CopyTo(ms);
            ms.Position = 0;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _worldData = JsonSerializer.Deserialize<WorldData>(ms.ToArray(), options)!;
            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            ValidateWorldData();
        }

        private void ValidateWorldData()
        {
            if (_worldData.Header.Magic != "RPGW")
                throw new InvalidDataException("Invalid world file format");
        }

        // Basic accessors
        public string GetString(int id) =>
            _stringCache.TryGetValue(id, out var str) ? str : $"<unknown string {id}>";

        public WorldData GetWorldData() => _worldData;

        // Region methods
        public WorldRegion? GetStartingRegion()
        {
            return _worldData.Regions
                .FirstOrDefault(r => 
                    GetString(r.NameId).Contains("Village") || 
                    GetString(r.NameId).Contains("Town"))
                as WorldRegion
                ?? _worldData.Regions.FirstOrDefault() as WorldRegion;
        }

        public WorldRegion? GetRegionByName(string name)
        {
            return _worldData.Regions
                .FirstOrDefault(r => 
                    GetString(r.NameId).Equals(name, StringComparison.OrdinalIgnoreCase))
                as WorldRegion;
        }

        public IEnumerable<WorldRegion> GetConnectedRegions(WorldRegion region) =>
            region.Connections.Select(idx => _worldData.Regions[idx]);

        // Location methods
        public Location? GetLocationByName(WorldRegion region, string name)
        {
            return region.Locations.FirstOrDefault(l => 
                GetString(l.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public string GetLocationDescription(Location location)
        {
            var desc = GetString(location.DescriptionId);
            var type = GetString(location.TypeId);
            return $"{desc}\nThis is a {type}.";
        }

        public bool LocationNameMatches(Location location, string name) =>
            GetString(location.NameId).Equals(name, StringComparison.OrdinalIgnoreCase);

        public IEnumerable<Location> GetLocationsInRegion(WorldRegion region) =>
            region.Locations;

        // NPC methods
        public IEnumerable<Entity> GetNPCsInRegion(WorldRegion region) =>
            region.NPCs.Select(idx => _worldData.NPCs[idx]);

        public List<Entity> GetNPCsInLocation(Location location) =>
            location.NPCs.Select(idx => _worldData.NPCs[idx]).ToList();

        public string GetNPCDialogue(Entity npc)
        {
            if (npc.DialogueRefs.Any())
            {
                int dialogueIdx = npc.DialogueRefs[Random.Shared.Next(npc.DialogueRefs.Count)];
                return _worldData.Resources.SharedDialogue[dialogueIdx];
            }
            return "...";
        }

        // Item methods
        public IEnumerable<Item> GetItemsInRegion(WorldRegion region) =>
            region.Items.Select(idx => _worldData.Items[idx]);

        public List<Item> GetItemsInLocation(Location location) =>
            location.Items.Select(idx => _worldData.Items[idx]).ToList();

        // Route methods
        public List<RoutePoint> GetRoute(WorldRegion from, WorldRegion to)
        {
            int toIndex = _worldData.Regions.IndexOf(to as WorldRegion);
            return from.Routes.TryGetValue(toIndex, out var route) ? route : new List<RoutePoint>();
        }

        public string GetRouteDescription(RoutePoint point) => 
            GetString(point.DescriptionId);

        public string GetRouteDirections(RoutePoint point) => 
            GetString(point.DirectionsId);

        public IEnumerable<Location> GetRouteLandmarks(RoutePoint point) =>
            point.Landmarks.Select(l => new Location 
            {
                NameId = GetOrAddString(l.Name),
                TypeId = GetOrAddString(l.Type),
                DescriptionId = GetOrAddString(l.Description)
            });

        private int GetOrAddString(string str)
        {
            if (_worldData.Resources.StringPool.TryGetValue(str, out var id))
                return id;

            id = _worldData.Resources.StringPool.Count;
            _worldData.Resources.StringPool[str] = id;
            _stringCache[id] = str;
            return id;
        }

        // Helper methods for getting display text
        public string GetEntityName(Entity entity) => GetString(entity.NameId);
        public string GetItemName(Item item) => GetString(item.NameId);
        public string GetItemDescription(Item item) => GetString(item.DescriptionId);
        public string GetRegionName(WorldRegion region) => GetString(region.NameId);
        public string GetRegionDescription(WorldRegion region) => GetString(region.DescriptionId);
        public string GetLocationName(Location location) => GetString(location.NameId);
    }
}
