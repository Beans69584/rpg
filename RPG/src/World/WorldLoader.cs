using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

using RPG.World;
using RPG.World.Data;
using RPG.World.Generation;

namespace RPG.World
{
    /// <summary>
    /// Loads and provides access to world data from a compressed JSON file.
    /// </summary>
    public class WorldLoader
    {
        private readonly WorldData _worldData;
        private readonly Dictionary<int, string> _stringCache;

        /// <summary>
        /// Initialises a new instance of the <see cref="WorldLoader"/> class.
        /// </summary>
        /// <param name="worldPath">The path to the compressed world data file.</param>
        public WorldLoader(string worldPath)
        {
            // Read and decompress world file
            using FileStream fs = File.OpenRead(worldPath);
            using GZipStream gzip = new(fs, CompressionMode.Decompress);
            using MemoryStream ms = new();
            gzip.CopyTo(ms);
            ms.Position = 0;

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            _worldData = JsonSerializer.Deserialize<WorldData>(ms, options)!;
            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            ValidateWorldData();
        }

        private void ValidateWorldData()
        {
            if (_worldData.Header.Magic != "RPGW")
                throw new InvalidDataException("Invalid world file format");
        }

        /// <summary>
        /// Gets the string with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the string to get.</param>
        /// <returns>The string with the specified ID, or a placeholder if not found.</returns>
        public string GetString(int id)
        {
            return _stringCache.TryGetValue(id, out string? str) ? str : $"<unknown string {id}>";
        }

        /// <summary>
        /// Gets the world data loaded by this instance.
        /// </summary>
        /// <returns>The world data loaded by this instance.</returns>
        public WorldData GetWorldData()
        {
            return _worldData;
        }

        // Region methods
        /// <summary>
        /// Gets the starting region for the game.
        /// </summary>
        /// <returns>The starting region for the game.</returns>
        public WorldRegion? GetStartingRegion()
        {
            return _worldData.Regions
                .Find(r =>
                    GetString(r.NameId).Contains("Village") ||
                    GetString(r.NameId).Contains("Town"))
                ?? _worldData.Regions.FirstOrDefault();
        }

        /// <summary>
        /// Gets the region with the specified name.
        /// </summary>
        /// <param name="name">The name of the region to get.</param>
        /// <returns>The region with the specified name, or <see langword="null"/> if not found.</returns>
        public WorldRegion? GetRegionByName(string name)
        {
            return _worldData.Regions
                .Find(r =>
                    GetString(r.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the region with the specified ID.
        /// </summary>
        /// <param name="region">The ID of the region to get.</param>
        /// <returns>The region with the specified ID, or <see langword="null"/> if not found.</returns>
        public IEnumerable<WorldRegion> GetConnectedRegions(WorldRegion region)
        {
            return region.Connections.Select(idx => _worldData.Regions[idx]);
        }

        /// <summary>
        /// Gets a location in the specified region by its name.
        /// </summary>
        /// <param name="region">The region to search for the location.</param>
        /// <param name="name">The name of the location to find.</param>
        /// <returns>The location with the specified name, or <see langword="null"/> if not found.</returns>
        public Location? GetLocationByName(WorldRegion region, string name)
        {
            return region.Locations.Find(l =>
                GetString(l.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the description of the specified location.
        /// </summary>
        /// <param name="location">The location to get the description for.</param>
        /// <returns>A string containing the location's description and type.</returns>
        public string GetLocationDescription(Location location)
        {
            string desc = GetString(location.DescriptionId);
            string type = GetString(location.TypeId);
            return $"{desc}\nThis is a {type}.";
        }

        /// <summary>
        /// Checks if the location's name matches the specified name.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <param name="name">The name to compare against.</param>
        /// <returns><see langword="true"/> if the names match (case-insensitive), otherwise <see langword="false"/>.</returns>
        public bool LocationNameMatches(Location location, string name)
        {
            return GetString(location.NameId).Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all locations within the specified region.
        /// </summary>
        /// <param name="region">The region to get locations from.</param>
        /// <returns>A collection of locations within the specified region.</returns>
        public static IEnumerable<Location> GetLocationsInRegion(WorldRegion region)
        {
            return region.Locations;
        }

        /// <summary>
        /// Gets all NPCs within the specified region.
        /// </summary>
        /// <param name="region">The region to get NPCs from.</param>
        /// <returns>A collection of NPCs within the specified region.</returns>
        public IEnumerable<Entity> GetNPCsInRegion(WorldRegion region)
        {
            return region.NPCs.Select(idx => _worldData.NPCs[idx]);
        }

        /// <summary>
        /// Gets all NPCs within the specified location.
        /// </summary>
        /// <param name="location">The location to get NPCs from.</param>
        /// <returns>A list of NPCs within the specified location.</returns>
        public List<Entity> GetNPCsInLocation(Location location)
        {
            return [.. location.NPCs.Select(idx => _worldData.NPCs[idx])];
        }

        /// <summary>
        /// Gets a random dialogue line for the specified NPC.
        /// </summary>
        /// <param name="npc">The NPC to get dialogue for.</param>
        /// <returns>A dialogue string, or "..." if the NPC has no dialogue.</returns>
        public string GetNPCDialogue(Entity npc)
        {
            if (npc.DialogueRefs.Any())
            {
                int dialogueIdx = npc.DialogueRefs[Random.Shared.Next(npc.DialogueRefs.Count)];
                return _worldData.Resources.SharedDialogue[dialogueIdx];
            }
            return "...";
        }

        /// <summary>
        /// Gets all items within the specified region.
        /// </summary>
        /// <param name="region">The region to get items from.</param>
        /// <returns>A collection of items within the specified region.</returns>
        public IEnumerable<Item> GetItemsInRegion(WorldRegion region)
        {
            return region.Items.Select(idx => _worldData.Items[idx]);
        }

        /// <summary>
        /// Gets all items within the specified location.
        /// </summary>
        /// <param name="location">The location to get items from.</param>
        /// <returns>A list of items within the specified location.</returns>
        public List<Item> GetItemsInLocation(Location location)
        {
            return [.. location.Items.Select(idx => _worldData.Items[idx])];
        }

        /// <summary>
        /// Gets the route points for traveling between two regions.
        /// </summary>
        /// <param name="from">The starting region.</param>
        /// <param name="to">The destination region.</param>
        /// <returns>A list of route points describing the path between regions, or an empty list if no route exists.</returns>
        public List<RoutePoint> GetRoute(WorldRegion from, WorldRegion to)
        {
            int toIndex = _worldData.Regions.IndexOf(to);
            return from.Routes.TryGetValue(toIndex, out List<RoutePoint>? route) ? route : [];
        }

        /// <summary>
        /// Gets the description of the specified route point.
        /// </summary>
        /// <param name="point">The route point to get the description for.</param>
        /// <returns>The description of the route point.</returns>
        public string GetRouteDescription(RoutePoint point)
        {
            return GetString(point.DescriptionId);
        }

        /// <summary>
        /// Gets the directions for the specified route point.
        /// </summary>
        /// <param name="point">The route point to get the directions for.</param>
        /// <returns>The directions for the route point.</returns>
        public string GetRouteDirections(RoutePoint point)
        {
            return GetString(point.DirectionsId);
        }

        /// <summary>
        /// Gets the landmarks associated with the specified route point.
        /// </summary>
        /// <param name="point">The route point to get landmarks for.</param>
        /// <returns>A collection of locations representing landmarks along the route.</returns>
        public IEnumerable<Location> GetRouteLandmarks(RoutePoint point)
        {
            return point.Landmarks.Select(l => new Location
            {
                NameId = GetOrAddString(l.Name),
                TypeId = GetOrAddString(l.Type),
                DescriptionId = GetOrAddString(l.Description)
            });
        }

        private int GetOrAddString(string str)
        {
            if (_worldData.Resources.StringPool.TryGetValue(str, out int id))
                return id;

            id = _worldData.Resources.StringPool.Count;
            _worldData.Resources.StringPool[str] = id;
            _stringCache[id] = str;
            return id;
        }

        /// <summary>
        /// Gets the name of the specified entity.
        /// </summary>
        /// <param name="entity">The entity to get the name for.</param>
        /// <returns>The name of the entity.</returns>
        public string GetEntityName(Entity entity)
        {
            return GetString(entity.NameId);
        }

        /// <summary>
        /// Gets the name of the specified item.
        /// </summary>
        /// <param name="item">The item to get the name for.</param>
        /// <returns>The name of the item.</returns>
        public string GetItemName(Item item)
        {
            return GetString(item.NameId);
        }

        /// <summary>
        /// Gets the description of the specified item.
        /// </summary>
        /// <param name="item">The item to get the description for.</param>
        /// <returns>The description of the item.</returns>
        public string GetItemDescription(Item item)
        {
            return GetString(item.DescriptionId);
        }

        /// <summary>
        /// Gets the name of the specified region.
        /// </summary>
        /// <param name="region">The region to get the name for.</param>
        /// <returns>The name of the region.</returns>
        public string GetRegionName(WorldRegion region)
        {
            return GetString(region.NameId);
        }

        /// <summary>
        /// Gets the description of the specified region.
        /// </summary>
        /// <param name="region">The region to get the description for.</param>
        /// <returns>The description of the region.</returns>
        public string GetRegionDescription(WorldRegion region)
        {
            return GetString(region.DescriptionId);
        }

        /// <summary>
        /// Gets the name of the specified location.
        /// </summary>
        /// <param name="location">The location to get the name for.</param>
        /// <returns>The name of the location.</returns>
        public string GetLocationName(Location location)
        {
            return GetString(location.NameId);
        }
    }
}
