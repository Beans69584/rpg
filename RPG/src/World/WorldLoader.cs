using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

using RPG.Common;
using RPG.World.Data;

namespace RPG.World
{
    /// <summary>
    /// Loads and provides access to world data.
    /// </summary>
    public class WorldLoader
    {
        private readonly WorldData _worldData;
        private readonly Dictionary<int, string> _stringCache;

        /// <summary>
        /// Initialises a new instance of the <see cref="WorldLoader"/> class.
        /// </summary>
        /// <param name="worldPath">The path to the world file.</param>
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
                PropertyNameCaseInsensitive = true,
                Converters = { new Vector2JsonConverter() }
            };

            _worldData = JsonSerializer.Deserialize<WorldData>(ms.ToArray(), options)
                ?? throw new InvalidDataException("Failed to deserialize world data");

            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            ValidateWorldData();
        }

        /// <inheritdoc cref="WorldLoader(string)"/>
        /// <param name="worldData">The world data to load.</param>
        public WorldLoader(WorldData worldData)
        {
            _worldData = worldData;
            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            ValidateWorldData();
        }

        /// <summary>
        /// Gets the string with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the string to get.</param>
        /// <returns>The string with the specified ID.</returns>
        public string GetString(int id)
        {
            return _stringCache.TryGetValue(id, out string? str) ? str : $"<unknown string {id}>";
        }

        private void ValidateWorldData()
        {
            if (_worldData.Header.Magic != "RPGW")
                throw new InvalidDataException("Invalid world file format");
        }

        /// <summary>
        /// Gets the world data.
        /// </summary>
        /// <returns>The world data.</returns>
        public WorldData GetWorldData()
        {
            return _worldData;
        }

        /// <summary>
        /// Gets the starting region.
        /// </summary>
        /// <returns>The starting region.</returns>
        public WorldRegion? GetStartingRegion()
        {
            return _worldData.Regions
                .FirstOrDefault(r => GetString(r.NameId).Equals("Ravenkeep Town", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the region with the specified name.
        /// </summary>
        /// <param name="name">The name of the region to get.</param>
        /// <returns>The region with the specified name, or null if no region was found.</returns>
        public WorldRegion? GetRegionByName(string name)
        {
            return _worldData.Regions
                .FirstOrDefault(r => GetString(r.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets connected regions to the specified region.
        /// </summary>
        /// <param name="region">The region to get connected regions for.</param>
        /// <returns>The connected regions to the specified region.</returns>
        public IEnumerable<WorldRegion> GetConnectedRegions(WorldRegion region)
        {
            return region.Connections.Select(idx => _worldData.Regions[idx]);
        }

        /// <summary>
        /// Gets the location with the specified name in the specified region.
        /// </summary>
        /// <param name="region">The region to search in.</param>
        /// <param name="name">The name of the location to get.</param>
        /// <returns>The location with the specified name in the specified region, or null if no location was found.</returns>
        public Location? GetLocationByName(WorldRegion region, string name)
        {
            return region.Locations
                .FirstOrDefault(l => GetString(l.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the location description.
        /// </summary>
        /// <param name="location">The location to get the description for.</param>
        /// <returns>The location description.</returns>
        public string GetLocationDescription(Location location)
        {
            string desc = GetString(location.DescriptionId);
            return $"{desc}\nThis is a {location.Type}.";
        }

        /// <summary>
        /// Checks if the location name matches the specified name.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <param name="name">The name to check against.</param>
        /// <returns>True if the location name matches the specified name; otherwise, false.</returns>
        public bool LocationNameMatches(Location location, string name)
        {
            return GetString(location.NameId).Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the locations in the specified region.
        /// </summary>
        /// <param name="region">The region to get locations for.</param>
        /// <returns>The locations in the specified region.</returns>
        public static IEnumerable<Location> GetLocationsInRegion(WorldRegion region)
        {
            return region.Locations;
        }

        /// <summary>
        /// Gets the NPCs in the specified region.
        /// </summary>
        /// <param name="region">The region to get NPCs for.</param>
        /// <returns>The NPCs in the specified region.</returns>
        public IEnumerable<Entity> GetNPCsInRegion(WorldRegion region)
        {
            return region.NPCs.Select(idx => _worldData.NPCs[idx]);
        }

        /// <summary>
        /// Gets the NPCs in the specified location.
        /// </summary>
        /// <param name="location">The location to get NPCs for.</param>
        /// <returns>The NPCs in the specified location.</returns>
        public List<Entity> GetNPCsInLocation(Location location)
        {
            return [.. location.NPCs.Select(idx => _worldData.NPCs[idx])];
        }

        /// <summary>
        /// Gets the NPC dialogue.
        /// </summary>
        /// <param name="npc">The NPC to get the dialogue for.</param>
        /// <returns>The NPC dialogue.</returns>
        public string GetNPCDialogue(Entity npc)
        {
            if (npc.DialogueTreeRefs.Any())
            {
                int dialogueIdx = npc.DialogueTreeRefs[Random.Shared.Next(npc.DialogueTreeRefs.Count)];
                return _worldData.Resources.DialogueTrees.TryGetValue(dialogueIdx, out DialogueTree? tree)
                    ? GetString(tree.Nodes[tree.RootNodeId].TextId)
                    : "...";
            }
            return "...";
        }

        /// <summary>
        /// Gets the items in the specified region.
        /// </summary>
        /// <param name="region">The region to get items for.</param>
        /// <returns>The items in the specified region.</returns>
        public IEnumerable<Item> GetItemsInRegion(WorldRegion region)
        {
            return region.Items.Select(idx => _worldData.Items[idx]);
        }

        /// <summary>
        /// Gets the items in the specified location.
        /// </summary>
        /// <param name="location">The location to get items for.</param>
        /// <returns>The items in the specified location.</returns>
        public List<Item> GetItemsInLocation(Location location)
        {
            return [.. location.Items.Select(idx => _worldData.Items[idx])];
        }

        /// <summary>
        /// Gets a route between two regions.
        /// </summary>
        /// <param name="from">The region to start from.</param>
        /// <param name="to">The region to end at.</param>
        /// <returns>The route between the two regions.</returns>
        public List<RoutePoint> GetRoute(WorldRegion from, WorldRegion to)
        {
            int toIndex = _worldData.Regions.IndexOf(to);
            return from.Routes.ContainsKey(toIndex) ? from.Routes[toIndex] : [];
        }

        /// <summary>
        /// Gets the route description.
        /// </summary>
        /// <param name="point">The route point to get the description for.</param>
        /// <returns>The route description.</returns>
        public string GetRouteDescription(RoutePoint point)
        {
            return GetString(point.DescriptionId);
        }

        /// <summary>
        /// Gets the route directions.
        /// </summary>
        /// <param name="point">The route point to get the directions for.</param>
        /// <returns>The route directions.</returns>
        public string GetRouteDirections(RoutePoint point)
        {
            return GetString(point.DirectionsId);
        }

        /// <summary>
        /// Gets the route landmarks.
        /// </summary>
        /// <param name="point">The route point to get the landmarks for.</param>
        /// <returns>The route landmarks.</returns>
        public IEnumerable<Landmark> GetRouteLandmarks(RoutePoint point)
        {
            return point.Landmarks;
        }

        /// <summary>
        /// Gets the entity name.
        /// </summary>
        /// <param name="entity">The entity to get the name for.</param>
        /// <returns>The entity name.</returns>
        public string GetEntityName(Entity entity)
        {
            return GetString(entity.NameId);
        }

        /// <summary>
        /// Gets the item name.
        /// </summary>
        /// <param name="item">The item to get the name for.</param>
        /// <returns>The item name.</returns>
        public string GetItemName(Item item)
        {
            return GetString(item.NameId);
        }

        /// <summary>
        /// Gets the item description.
        /// </summary>
        /// <param name="item">The item to get the description for.</param>
        /// <returns>The item description.</returns>
        public string GetItemDescription(Item item)
        {
            return GetString(item.DescriptionId);
        }

        /// <summary>
        /// Gets the region name.
        /// </summary>
        /// <param name="region">The region to get the name for.</param>
        /// <returns>The region name.</returns>
        public string GetRegionName(WorldRegion region)
        {
            return GetString(region.NameId);
        }

        /// <summary>
        /// Gets the region description.
        /// </summary>
        /// <param name="region">The region to get the description for.</param>
        /// <returns>The region description.</returns>
        public string GetRegionDescription(WorldRegion region)
        {
            return GetString(region.DescriptionId);
        }

        /// <summary>
        /// Gets the location name.
        /// </summary>
        /// <param name="location">The location to get the name for.</param>
        /// <returns>The location name.</returns>
        public string GetLocationName(Location location)
        {
            return GetString(location.NameId);
        }
    }
}