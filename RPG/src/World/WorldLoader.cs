using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

using RPG.Common;
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
        /// Initializes a new instance of the WorldLoader class.
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
        /// Gets the string with the specified ID from the string pool.
        /// </summary>
        public string GetString(int id)
        {
            return _stringCache.TryGetValue(id, out string? str) ? str : $"<unknown string {id}>";
        }

        /// <summary>
        /// Gets the world data loaded by this instance.
        /// </summary>
        public WorldData GetWorldData()
        {
            return _worldData;
        }

        /// <summary>
        /// Gets the starting region for new players.
        /// </summary>
        public WorldRegion? GetStartingRegion()
        {
            return _worldData.Regions
                .FirstOrDefault(r => r.Locations.Any(l =>
                    l.Type is LocationType.Town or
                    LocationType.Village));
        }

        /// <summary>
        /// Gets a region by its name.
        /// </summary>
        public WorldRegion? GetRegionByName(string name)
        {
            return _worldData.Regions
                .FirstOrDefault(r =>
                    GetString(r.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all regions connected to the specified region.
        /// </summary>
        public IEnumerable<WorldRegion> GetConnectedRegions(WorldRegion region)
        {
            return region.Connections.Select(idx => _worldData.Regions[idx]);
        }

        /// <summary>
        /// Gets a location within a region by its name.
        /// </summary>
        public Location? GetLocationByName(WorldRegion region, string name)
        {
            return region.Locations.FirstOrDefault(l =>
                GetString(l.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the description of a location.
        /// </summary>
        public string GetLocationDescription(Location location)
        {
            string desc = GetString(location.DescriptionId);
            return $"{desc}\nThis is a {location.Type}.";
        }

        /// <summary>
        /// Checks if a location's name matches the specified name.
        /// </summary>
        public bool LocationNameMatches(Location location, string name)
        {
            return GetString(location.NameId).Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all locations within a region.
        /// </summary>
        public static IEnumerable<Location> GetLocationsInRegion(WorldRegion region)
        {
            return region.Locations;
        }

        /// <summary>
        /// Gets all NPCs in a region.
        /// </summary>
        public IEnumerable<Entity> GetNPCsInRegion(WorldRegion region)
        {
            return region.NPCs.Select(idx => _worldData.NPCs[idx]);
        }

        /// <summary>
        /// Gets all NPCs at a location.
        /// </summary>
        public List<Entity> GetNPCsInLocation(Location location)
        {
            return [.. location.NPCs.Select(idx => _worldData.NPCs[idx])];
        }

        /// <summary>
        /// Gets random dialogue for an NPC.
        /// </summary>
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
        /// Gets all items in a region.
        /// </summary>
        public IEnumerable<Item> GetItemsInRegion(WorldRegion region)
        {
            return region.Items.Select(idx => _worldData.Items[idx]);
        }

        /// <summary>
        /// Gets all items at a location.
        /// </summary>
        public List<Item> GetItemsInLocation(Location location)
        {
            return [.. location.Items.Select(idx => _worldData.Items[idx])];
        }

        /// <summary>
        /// Gets the route points between two regions.
        /// </summary>
        public List<RoutePoint> GetRoute(WorldRegion from, WorldRegion to)
        {
            int toIndex = _worldData.Regions.IndexOf(to);
            return from.Routes.ContainsKey(toIndex) ? from.Routes[toIndex] : [];
        }

        /// <summary>
        /// Gets the description of a route point.
        /// </summary>
        public string GetRouteDescription(RoutePoint point)
        {
            return GetString(point.DescriptionId);
        }

        /// <summary>
        /// Gets the directions for a route point.
        /// </summary>
        public string GetRouteDirections(RoutePoint point)
        {
            return GetString(point.DirectionsId);
        }

        /// <summary>
        /// Gets all landmarks along a route point.
        /// </summary>
        public IEnumerable<Landmark> GetRouteLandmarks(RoutePoint point)
        {
            return point.Landmarks;
        }

        /// <summary>
        /// Gets the name of an entity.
        /// </summary>
        public string GetEntityName(Entity entity)
        {
            return GetString(entity.NameId);
        }

        /// <summary>
        /// Gets the name of an item.
        /// </summary>
        public string GetItemName(Item item)
        {
            return GetString(item.NameId);
        }

        /// <summary>
        /// Gets the description of an item.
        /// </summary>
        public string GetItemDescription(Item item)
        {
            return GetString(item.DescriptionId);
        }

        /// <summary>
        /// Gets the name of a region.
        /// </summary>
        public string GetRegionName(WorldRegion region)
        {
            return GetString(region.NameId);
        }

        /// <summary>
        /// Gets the description of a region.
        /// </summary>
        public string GetRegionDescription(WorldRegion region)
        {
            return GetString(region.DescriptionId);
        }

        /// <summary>
        /// Gets the name of a location.
        /// </summary>
        public string GetLocationName(Location location)
        {
            return GetString(location.NameId);
        }
    }
}