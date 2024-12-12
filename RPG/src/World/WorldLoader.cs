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
    public class WorldLoader
    {
        private readonly WorldData _worldData;
        private readonly Dictionary<int, string> _stringCache;

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

            // Update string cache creation to use Resources.StringPool
            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            ValidateWorldData();
        }

        // Add new constructor for loading from WorldData
        public WorldLoader(WorldData worldData)
        {
            _worldData = worldData;
            _stringCache = _worldData.Resources.StringPool
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            ValidateWorldData();
        }

        public string GetString(int id)
        {
            return _stringCache.TryGetValue(id, out string? str) ? str : $"<unknown string {id}>";
        }

        private void ValidateWorldData()
        {
            if (_worldData.Header.Magic != "RPGW")
                throw new InvalidDataException("Invalid world file format");

            // Additional validation can be added here
        }

        public WorldData GetWorldData()
        {
            return _worldData;
        }

        public WorldRegion? GetStartingRegion()
        {
            return _worldData.Regions
                .FirstOrDefault(r => GetString(r.NameId).Equals("Ravenkeep Town", StringComparison.OrdinalIgnoreCase));
        }

        public WorldRegion? GetRegionByName(string name)
        {
            return _worldData.Regions
                .FirstOrDefault(r => GetString(r.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<WorldRegion> GetConnectedRegions(WorldRegion region)
        {
            return region.Connections.Select(idx => _worldData.Regions[idx]);
        }

        public Location? GetLocationByName(WorldRegion region, string name)
        {
            return region.Locations
                .FirstOrDefault(l => GetString(l.NameId).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public string GetLocationDescription(Location location)
        {
            string desc = GetString(location.DescriptionId);
            return $"{desc}\nThis is a {location.Type}.";
        }

        public bool LocationNameMatches(Location location, string name)
        {
            return GetString(location.NameId).Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<Location> GetLocationsInRegion(WorldRegion region)
        {
            return region.Locations;
        }

        public IEnumerable<Entity> GetNPCsInRegion(WorldRegion region)
        {
            return region.NPCs.Select(idx => _worldData.NPCs[idx]);
        }

        public List<Entity> GetNPCsInLocation(Location location)
        {
            return [.. location.NPCs.Select(idx => _worldData.NPCs[idx])];
        }

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

        public IEnumerable<Item> GetItemsInRegion(WorldRegion region)
        {
            return region.Items.Select(idx => _worldData.Items[idx]);
        }

        public List<Item> GetItemsInLocation(Location location)
        {
            return [.. location.Items.Select(idx => _worldData.Items[idx])];
        }

        public List<RoutePoint> GetRoute(WorldRegion from, WorldRegion to)
        {
            int toIndex = _worldData.Regions.IndexOf(to);
            return from.Routes.ContainsKey(toIndex) ? from.Routes[toIndex] : [];
        }

        public string GetRouteDescription(RoutePoint point)
        {
            return GetString(point.DescriptionId);
        }

        public string GetRouteDirections(RoutePoint point)
        {
            return GetString(point.DirectionsId);
        }

        public IEnumerable<Landmark> GetRouteLandmarks(RoutePoint point)
        {
            return point.Landmarks;
        }

        public string GetEntityName(Entity entity)
        {
            return GetString(entity.NameId);
        }

        public string GetItemName(Item item)
        {
            return GetString(item.NameId);
        }

        public string GetItemDescription(Item item)
        {
            return GetString(item.DescriptionId);
        }

        public string GetRegionName(WorldRegion region)
        {
            return GetString(region.NameId);
        }

        public string GetRegionDescription(WorldRegion region)
        {
            return GetString(region.DescriptionId);
        }

        public string GetLocationName(Location location)
        {
            return GetString(location.NameId);
        }
    }
}