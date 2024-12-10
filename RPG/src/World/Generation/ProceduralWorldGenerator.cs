using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Common;
using RPG.World.Data;

namespace RPG.World.Generation
{
    public class ProceduralWorldGenerator
    {
        private readonly Random random;
        private readonly NoiseGenerator terrainNoise;
        private readonly NoiseGenerator moistureNoise;
        private readonly NoiseGenerator riverNoise;
        private readonly NoiseGenerator encounterNoise;
        private readonly int width;
        private readonly int height;
        private Dictionary<Vector2, TerrainType> worldMap = [];
        private readonly Dictionary<string, int> stringPool = [];
        private int nextStringId = 0;
        private WorldData? _worldData;

        private const int MIN_REGION_SIZE = 16;
        private const int MIN_LOCATION_SPACING = 8;
        private const float LOCATION_PLACEMENT_THRESHOLD = 0.6f;
        private const int MIN_SETTLEMENT_DISTANCE = 12;

        public ProceduralWorldGenerator(int seed = 0, int width = 256, int height = 256)
        {
            random = seed == 0 ? Random.Shared : new Random(seed);
            terrainNoise = new NoiseGenerator(random.Next());
            moistureNoise = new NoiseGenerator(random.Next());
            riverNoise = new NoiseGenerator(random.Next());
            encounterNoise = new NoiseGenerator(random.Next());
            this.width = width;
            this.height = height;
        }

        public WorldData GenerateWorld()
        {
            // Reset state
            worldMap.Clear();
            stringPool.Clear();
            nextStringId = 0;

            // Generate base world data
            worldMap = GenerateWorldMap();
            List<Vector2> regionCenters = PlaceRegionCenters();

            // Create world container
            _worldData = new WorldData
            {
                Header = new Header
                {
                    Name = stringPool.FirstOrDefault(x => x.Value == AddToStringPool("Generated World")).Key,
                    Description = stringPool.FirstOrDefault(x => x.Value == AddToStringPool("A world of adventure and mystery.")).Key,
                    Magic = "RPGW",
                    Version = "1.0",
                    CreatedAt = DateTime.UtcNow
                },
                Resources = new ResourceTable
                {
                    StringPool = stringPool
                }
            };

            // Generate regions
            foreach (Vector2 center in regionCenters)
            {
                TerrainType dominantTerrain = GetDominantTerrain(center);
                WorldRegion region = GenerateRegion(center, dominantTerrain);
                _worldData.Regions.Add(region);
            }

            // Connect regions
            ConnectRegions(_worldData.Regions);

            // Generate global NPCs and items
            GenerateGlobalEntities(_worldData);

            // Update counts
            _worldData.Header.RegionCount = _worldData.Regions.Count;
            _worldData.Header.NPCCount = _worldData.NPCs.Count;
            _worldData.Header.ItemCount = _worldData.Items.Count;

            return _worldData;
        }

        private Dictionary<Vector2, TerrainType> GenerateWorldMap()
        {
            Dictionary<Vector2, TerrainType> map = [];

            // Generate base terrain
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float elevation = GenerateElevation(x, y);
                    float moisture = GenerateMoisture(x, y);
                    float riverValue = riverNoise.Generate2D(x / (float)width, y / (float)height, 3.0f);

                    TerrainType terrain = DetermineTerrainType(elevation, moisture);

                    // Add rivers
                    if (riverValue > 0.7f && elevation < 0.7f)
                    {
                        terrain = TerrainType.River;
                    }

                    map[new Vector2 { X = x, Y = y }] = terrain;
                }
            }

            // Post-process to ensure terrain coherence
            SmoothTerrain(map);
            return map;
        }

        private float GenerateElevation(int x, int y)
        {
            float frequency = 1.0f;
            float amplitude = 1.0f;
            float elevation = 0;
            float maxValue = 0;

            for (int i = 0; i < 4; i++)
            {
                elevation += terrainNoise.Generate2D(x * frequency / width, y * frequency / height, 1) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.0f;
            }

            return elevation / maxValue;
        }

        private float GenerateMoisture(int x, int y)
        {
            return moistureNoise.Generate2D(x / (float)width, y / (float)height, 2.0f);
        }

        private TerrainType DetermineTerrainType(float elevation, float moisture)
        {
            if (elevation > 0.8f) return TerrainType.Mountain;
            if (elevation > 0.6f) return TerrainType.Hills;
            if (elevation < 0.3f)
            {
                if (elevation < 0.2f) return TerrainType.Coast;
                return moisture > 0.6f ? TerrainType.Swamp : TerrainType.Plains;
            }
            if (elevation < 0.4f && moisture > 0.6f) return TerrainType.Swamp;
            return moisture > 0.6f ? TerrainType.Forest : TerrainType.Plains;
        }

        private void SmoothTerrain(Dictionary<Vector2, TerrainType> map)
        {
            Dictionary<Vector2, TerrainType> smoothed = new(map);

            foreach (Vector2 pos in map.Keys)
            {
                IEnumerable<TerrainType> surroundingTypes = GetSurroundingTerrain(pos, map);
                TerrainType mostCommon = surroundingTypes
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                // Only smooth if current terrain is very different from surroundings
                if (surroundingTypes.Count(t => t == map[pos]) < 2)
                {
                    smoothed[pos] = mostCommon;
                }
            }

            foreach (KeyValuePair<Vector2, TerrainType> kvp in smoothed)
            {
                map[kvp.Key] = kvp.Value;
            }
        }

        private IEnumerable<TerrainType> GetSurroundingTerrain(Vector2 pos, Dictionary<Vector2, TerrainType> map)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector2 checkPos = new() { X = pos.X + dx, Y = pos.Y + dy };
                    if (map.TryGetValue(checkPos, out TerrainType terrain))
                    {
                        yield return terrain;
                    }
                }
            }
        }

        private List<Vector2> PlaceRegionCenters()
        {
            List<Vector2> centers = [];
            int attempts = 100;

            while (attempts > 0 && centers.Count < (width * height) / (MIN_REGION_SIZE * MIN_REGION_SIZE))
            {
                Vector2 candidate = new()
                {
                    X = random.Next(width),
                    Y = random.Next(height)
                };

                if (IsSuitableRegionCenter(candidate, centers))
                {
                    centers.Add(candidate);
                }

                attempts--;
            }

            return centers;
        }

        private bool IsSuitableRegionCenter(Vector2 candidate, List<Vector2> existingCenters)
        {
            // Check distance from other centers
            if (existingCenters.Any(center =>
                Math.Sqrt(Math.Pow(center.X - candidate.X, 2) + Math.Pow(center.Y - candidate.Y, 2)) < MIN_REGION_SIZE))
            {
                return false;
            }

            // Check terrain suitability
            TerrainType terrain = worldMap[candidate];
            if (terrain == TerrainType.River) return false;

            return true;
        }

        private WorldRegion GenerateRegion(Vector2 center, TerrainType dominantTerrain)
        {
            int regionNameId = AddToStringPool(GenerateRegionName(dominantTerrain));
            int regionDescId = AddToStringPool(GenerateRegionDescription(dominantTerrain));

            WorldRegion region = new()
            {
                NameId = regionNameId,
                DescriptionId = regionDescId,
                Position = center,
                Terrain = dominantTerrain,
                TerrainMap = ExtractLocalTerrainMap(center),
                ExplorationProgress = 0.0f
            };

            // Generate locations
            GenerateLocationsForRegion(region);

            // Generate routes between locations
            GenerateRoutesForRegion(region);

            return region;
        }

        private Dictionary<Vector2, TerrainType> ExtractLocalTerrainMap(Vector2 center)
        {
            Dictionary<Vector2, TerrainType> localMap = [];
            int radius = MIN_REGION_SIZE / 2;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    Vector2 pos = new()
                    {
                        X = center.X + dx,
                        Y = center.Y + dy
                    };

                    if (worldMap.TryGetValue(pos, out TerrainType terrain))
                    {
                        localMap[new Vector2 { X = dx, Y = dy }] = terrain;
                    }
                }
            }

            return localMap;
        }

        private void GenerateLocationsForRegion(WorldRegion region)
        {
            // Always try to place a settlement first if terrain permits
            if (region.Terrain is TerrainType.Plains or TerrainType.Hills)
            {
                Location settlement = GenerateSettlement(LocationType.Town, region.Position);
                region.Locations.Add(settlement);
            }

            // Generate additional locations
            int locationCount = random.Next(3, 7);
            List<Vector2> locationPositions = [region.Position];

            for (int i = 0; i < locationCount; i++)
            {
                Vector2 position = FindLocationPosition(region.Position, locationPositions, region.TerrainMap);
                if (position.X >= 0) // Valid position found
                {
                    LocationType locationType = DetermineLocationType(region.Terrain, region.TerrainMap[position]);
                    Location location = GenerateLocation(locationType, position);
                    region.Locations.Add(location);
                    locationPositions.Add(position);
                }
            }
        }

        private void GenerateRoutesForRegion(WorldRegion region)
        {
            // Generate routes between locations
            for (int i = 0; i < region.Locations.Count; i++)
            {
                for (int j = i + 1; j < region.Locations.Count; j++)
                {
                    if (ShouldConnectLocations(region.Locations[i], region.Locations[j]))
                    {
                        Route route = GenerateRoute(region.Locations[i], region.Locations[j], region.TerrainMap);
                        int destinationIndex = j; // or whatever index system you're using
                        region.Routes.Add(destinationIndex, route.Segments.Select(s =>
                            new RoutePoint
                            {
                                DescriptionId = s.DescriptionId,
                                DirectionsId = s.DirectionsId,
                                Landmarks = s.Landmarks
                            }).ToList());
                    }
                }
            }
        }

        private bool ShouldConnectLocations(Location a, Location b)
        {
            // Always connect towns/villages
            if (a.Type is LocationType.Town or LocationType.Village &&
                b.Type is LocationType.Town or LocationType.Village)
                return true;

            // Connect other locations with 50% chance
            return random.Next(2) == 0;
        }

        private Route GenerateRoute(Location start, Location end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            string routeName = $"Route from {stringPool.FirstOrDefault(x => x.Value == start.NameId).Key} to {stringPool.FirstOrDefault(x => x.Value == end.NameId).Key}";
            Route route = new()
            {
                NameId = AddToStringPool(routeName),
                PathPoints = FindPath(start.Position, end.Position, terrainMap),
                TerrainType = GetPredominantTerrain(terrainMap),
                DifficultyRating = CalculateRouteDifficulty(start.Position, end.Position, terrainMap)
            };

            // Generate segments along the route
            GenerateRouteSegments(route, terrainMap);

            // Generate encounters
            GenerateRouteEncounters(route);

            return route;
        }

        private List<Vector2> FindPath(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            // A* pathfinding implementation
            PriorityQueue<Vector2, float> openSet = new();
            Dictionary<Vector2, Vector2> cameFrom = [];
            Dictionary<Vector2, float> gScore = [];
            Dictionary<Vector2, float> fScore = [];

            openSet.Enqueue(start, 0);
            gScore[start] = 0;
            fScore[start] = HeuristicCost(start, end);

            while (openSet.Count > 0)
            {
                Vector2 current = openSet.Dequeue();

                if (current.Equals(end))
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (Vector2 neighbor in GetNeighbors(current, terrainMap))
                {
                    float tentativeGScore = gScore[current] + MovementCost(current, neighbor, terrainMap);

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        float priority = tentativeGScore + HeuristicCost(neighbor, end);
                        fScore[neighbor] = priority;
                        openSet.Enqueue(neighbor, priority);
                    }
                }
            }

            // If no path found, return direct line
            return [start, end];
        }

        private float MovementCost(Vector2 from, Vector2 to, Dictionary<Vector2, TerrainType> terrainMap)
        {
            // Base cost is distance
            float cost = (float)Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));

            // Modify based on terrain
            if (terrainMap.TryGetValue(to, out TerrainType terrain))
            {
                cost *= terrain switch
                {
                    TerrainType.Mountain => 3.0f,
                    TerrainType.Hills => 1.5f,
                    TerrainType.Swamp => 2.0f,
                    TerrainType.River => 2.5f,
                    TerrainType.Forest => 1.3f,
                    _ => 1.0f
                };
            }

            return cost;
        }

        private float HeuristicCost(Vector2 from, Vector2 to)
        {
            return (float)Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        }

        private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
        {
            List<Vector2> path = [current];
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        private IEnumerable<Vector2> GetNeighbors(Vector2 pos, Dictionary<Vector2, TerrainType> terrainMap)
        {
            int[] dx = [1, 0, -1, 0, 1, 1, -1, -1];
            int[] dy = [0, 1, 0, -1, 1, -1, 1, -1];

            for (int i = 0; i < dx.Length; i++)
            {
                Vector2 neighbor = new()
                {
                    X = pos.X + dx[i],
                    Y = pos.Y + dy[i]
                };

                if (terrainMap.ContainsKey(neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        private void GenerateRouteSegments(Route route, Dictionary<Vector2, TerrainType> terrainMap)
        {
            List<Vector2> path = route.PathPoints;
            int segmentLength = Math.Max(3, path.Count / 4); // Split path into roughly 4 segments

            for (int i = 0; i < path.Count; i += segmentLength)
            {
                Vector2 segmentStart = path[i];
                Vector2 segmentEnd = path[Math.Min(i + segmentLength, path.Count - 1)];

                RouteSegment segment = new()
                {
                    DescriptionId = AddToStringPool(GenerateRouteSegmentDescription(segmentStart, segmentEnd, terrainMap)),
                    DirectionsId = AddToStringPool(GenerateRouteDirections(segmentStart, segmentEnd)),
                    EncounterRate = CalculateSegmentEncounterRate(segmentStart, segmentEnd, terrainMap),
                    Landmarks = GenerateLandmarksForSegment(segmentStart, segmentEnd, terrainMap)
                };

                route.Segments.Add(segment);
            }
        }

        private void GenerateRouteEncounters(Route route)
        {
            // Generate base encounters for route's terrain type
            List<Encounter> baseEncounters = GenerateEncountersForTerrain(route.TerrainType);
            route.PossibleEncounters.AddRange(baseEncounters);

            // Add segment-specific encounters
            foreach (RouteSegment segment in route.Segments)
            {
                segment.SegmentSpecificEncounters = GenerateSegmentSpecificEncounters(segment, baseEncounters);
            }
        }

        private List<Encounter> GenerateEncountersForTerrain(TerrainType terrain)
        {
            List<Encounter> encounters = [];

            // Generate 2-4 encounters appropriate for the terrain
            int count = random.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                Encounter encounter = new()
                {
                    Type = GenerateEncounterType(),
                    DescriptionId = AddToStringPool(GenerateEncounterDescription(terrain)),
                    Probability = 0.2f + (random.NextSingle() * 0.3f), // 20-50% chance
                    Rewards = GenerateEncounterRewards()
                };
                encounters.Add(encounter);
            }

            return encounters;
        }

        private EncounterType GenerateEncounterType()
        {
            return random.Next(100) switch
            {
                < 40 => EncounterType.Combat,
                < 70 => EncounterType.NPC,
                < 90 => EncounterType.Event,
                _ => EncounterType.Discovery
            };
        }

        private List<Encounter> GenerateSegmentSpecificEncounters(RouteSegment segment, List<Encounter> baseEncounters)
        {
            List<Encounter> encounters = new(baseEncounters); // Start with copies of base encounters

            // Add encounters based on landmarks
            foreach (Landmark landmark in segment.Landmarks)
            {
                encounters.Add(new Encounter
                {
                    Type = EncounterType.Discovery,
                    DescriptionId = AddToStringPool($"You discover {stringPool.FirstOrDefault(x => x.Value == landmark.NameId).Key}"),
                    Probability = 0.5f,
                    Rewards = GenerateEncounterRewards()
                });
            }

            return encounters;
        }

        private List<Landmark> GenerateLandmarksForSegment(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            List<Landmark> landmarks = [];

            // 30% chance for each segment to have a landmark
            if (random.Next(100) < 30)
            {
                Vector2 position = new()
                {
                    X = ((start.X + end.X) / 2) + random.Next(-2, 3),
                    Y = ((start.Y + end.Y) / 2) + random.Next(-2, 3)
                };

                if (terrainMap.TryGetValue(position, out TerrainType terrain))
                {
                    landmarks.Add(GenerateLandmark(position, terrain));
                }
            }

            return landmarks;
        }

        private Landmark GenerateLandmark(Vector2 position, TerrainType terrain)
        {
            string name = GenerateLandmarkName(terrain);
            return new Landmark
            {
                NameId = AddToStringPool(name),
                DescriptionId = AddToStringPool(GenerateLandmarkDescription(terrain)),
                Type = terrain.ToString(),
                Position = position
            };
        }

        private List<Reward> GenerateEncounterRewards()
        {
            List<Reward> rewards = [];
            if (random.Next(100) < 70) // 70% chance for reward
            {
                rewards.Add(new Reward
                {
                    Type = random.Next(2) == 0 ? "Gold" : "Item",
                    Value = random.Next(10, 100),
                    DescriptionId = AddToStringPool("A reward for your troubles.")
                });
            }
            return rewards;
        }

        private void ConnectRegions(List<WorldRegion> regions)
        {
            // Create minimum spanning tree to ensure connectivity
            for (int i = 0; i < regions.Count; i++)
            {
                for (int j = i + 1; j < regions.Count; j++)
                {
                    float distance = CalculateRegionDistance(regions[i], regions[j]);
                    if (distance < MIN_REGION_SIZE * 2 || random.NextDouble() < 0.3) // Connect close regions and random extras
                    {
                        regions[i].Connections.Add(j);
                        regions[j].Connections.Add(i);
                    }
                }
            }
        }

        private float CalculateRegionDistance(WorldRegion a, WorldRegion b)
        {
            return (float)Math.Sqrt(
                Math.Pow(b.Position.X - a.Position.X, 2) +
                Math.Pow(b.Position.Y - a.Position.Y, 2));
        }

        private void GenerateGlobalEntities(WorldData world)
        {
            // Since we're now generating NPCs during building creation,
            // we only need to handle NPCs that aren't in buildings
            HashSet<int> npcIndices = [];
            HashSet<int> itemIndices = [];

            foreach (WorldRegion region in world.Regions)
            {
                // Add region-specific NPCs
                for (int i = 0; i < random.Next(2, 5); i++)
                {
                    Entity npc = GenerateNPC();
                    int index = world.NPCs.Count;
                    world.NPCs.Add(npc);
                    npcIndices.Add(index);
                }

                npcIndices.UnionWith(region.NPCs);
                itemIndices.UnionWith(region.Items);
            }

            // Generate global items
            foreach (int index in itemIndices)
            {
                if (index >= world.Items.Count)
                {
                    Item item = GenerateItem();
                    world.Items.Add(item);
                }
            }
        }

        private Entity GenerateNPC()
        {
            return new Entity
            {
                NameId = AddToStringPool(GenerateNPCName()),
                Level = random.Next(1, 10),
                HP = random.Next(50, 100),
                Stats = GenerateEntityStats(),
                DialogueRefs = GenerateNPCDialogue()
            };
        }

        private Item GenerateItem()
        {
            string name = GenerateItemName();
            return new Item
            {
                NameId = AddToStringPool(name),
                DescriptionId = AddToStringPool(GenerateItemDescription(name)),
                Stats = GenerateItemStats()
            };
        }

        private int AddToStringPool(string str)
        {
            if (!stringPool.TryGetValue(str, out int id))
            {
                id = nextStringId++;
                stringPool[str] = id;
            }
            return id;
        }

        private string GenerateNPCName()
        {
            string[] firstNames = [
                "Erik", "Anna", "Johan", "Maria", "Lars", "Sofia", "Karl", "Emma",
        "Anders", "Linnea", "Gustav", "Helena", "Magnus", "Sara", "Olaf", "Ingrid"
            ];

            string[] lastNames = [
                "Berg", "Holm", "Strand", "Lund", "Bjork", "Storm", "Nord", "Ek",
        "Dahl", "Falk", "Roth", "Kvist", "Skog", "Bäck", "Åker", "Sten"
            ];

            return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
        }

        private string GenerateItemName()
        {
            string[] prefixes = ["Rusty", "Ancient", "Fine", "Magic", "Strong", "Sharp", "Heavy", "Light"];
            string[] items = ["Sword", "Shield", "Bow", "Staff", "Armor", "Ring", "Amulet", "Potion"];
            string[] suffixes = ["of Power", "of Light", "of Protection", "of Speed", "of Wisdom", "of the Bear", "of the Wolf", "of the Eagle"];

            if (random.Next(100) < 30) // 30% chance for magical item
            {
                return $"{prefixes[random.Next(prefixes.Length)]} {items[random.Next(items.Length)]} {suffixes[random.Next(suffixes.Length)]}";
            }

            return $"{prefixes[random.Next(prefixes.Length)]} {items[random.Next(items.Length)]}";
        }

        private string GenerateLandmarkName(TerrainType terrain)
        {
            Dictionary<TerrainType, (string[] Prefixes, string[] Objects)> components = new()
            {
                [TerrainType.Mountain] = (
                    ["Ancient", "Towering", "Jagged", "Frost", "Thunder"],
                    ["Peak", "Spire", "Crag", "Summit", "Ridge"]
                ),
                [TerrainType.Forest] = (
                    ["Whispering", "Ancient", "Dark", "Sacred", "Elder"],
                    ["Grove", "Clearing", "Circle", "Shrine", "Tree"]
                ),
                [TerrainType.Plains] = (
                    ["Standing", "Ancient", "Weathered", "Sacred", "Lost"],
                    ["Stone", "Monolith", "Circle", "Ruins", "Shrine"]
                ),
                [TerrainType.Hills] = (
                    ["Lonely", "Silent", "Ancient", "Watching", "Hidden"],
                    ["Tower", "Stones", "Fort", "Cairn", "Tomb"]
                ),
                [TerrainType.Swamp] = (
                    ["Sunken", "Lost", "Cursed", "Forgotten", "Dark"],
                    ["Temple", "Ruins", "Stones", "Statue", "Shrine"]
                ),
                [TerrainType.River] = (
                    ["Ancient", "Stone", "Lost", "Broken", "Old"],
                    ["Bridge", "Ford", "Crossing", "Dock", "Port"]
                )
            };

            (string[] prefixes, string[] objects) = components.GetValueOrDefault(terrain, (
                ["Mysterious", "Strange", "Ancient", "Forgotten", "Hidden"],
                ["Ruins", "Monument", "Structure", "Stones", "Site"]
            ));

            return $"{prefixes[random.Next(prefixes.Length)]} {objects[random.Next(objects.Length)]}";
        }

        private string GenerateSettlementName()
        {
            string[] prefixes = ["River", "Lake", "Hill", "Green", "North", "South", "East", "West", "New", "Old"];
            string[] roots = ["wood", "vale", "ton", "ford", "bridge", "cross", "haven", "stead"];
            string[] suffixes = ["town", "ville", "burg", "port", "gate", "keep", "fall", "rise"];

            return random.Next(2) == 0
                ? $"{prefixes[random.Next(prefixes.Length)]}{roots[random.Next(roots.Length)]}"
                : $"{roots[random.Next(roots.Length)]}{suffixes[random.Next(suffixes.Length)]}";
        }

        private string GenerateRouteSegmentDescription(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            TerrainType terrain = GetPredominantTerrainBetweenPoints(start, end, terrainMap);

            Dictionary<TerrainType, string[]> descriptions = new()
            {
                [TerrainType.Mountain] = [
                    "A treacherous mountain path winds upward.",
            "The trail navigates between towering peaks.",
            "A narrow ledge hugs the mountainside."
                ],
                [TerrainType.Forest] = [
                    "The path weaves through ancient trees.",
            "Dense forest surrounds the trail.",
            "Shadows dance between the trees along the path."
                ],
                [TerrainType.Plains] = [
                    "A well-worn trail crosses the open plains.",
            "The path cuts through waving grasslands.",
            "A clear trail stretches across the meadow."
                ],
                [TerrainType.Hills] = [
                    "The path winds over rolling hills.",
            "A scenic trail crosses the highlands.",
            "The route meanders through gentle slopes."
                ],
                [TerrainType.Swamp] = [
                    "A raised walkway crosses the murky waters.",
            "The path carefully picks through the wetlands.",
            "Wooden planks bridge the boggy ground."
                ],
                [TerrainType.River] = [
                    "The trail follows the river's course.",
            "A path runs alongside the flowing water.",
            "The route traces the riverbank."
                ]
            };

            string[] options = descriptions.GetValueOrDefault(terrain, [
                "The path continues onward.",
        "The route stretches ahead.",
        "The trail leads forward."
            ]);

            return options[random.Next(options.Length)];
        }

        private string GenerateRouteDirections(Vector2 start, Vector2 end)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            string direction = angle switch
            {
                <= -2.748893f or > 2.748893f => "west",
                <= -1.9634954f => "northwest",
                <= -1.1780972f => "north",
                <= -0.39269908f => "northeast",
                <= 0.39269908f => "east",
                <= 1.1780972f => "southeast",
                <= 1.9634954f => "south",
                <= 2.748893f => "southwest",
                _ => "west"
            };

            string[] templates = [
                $"Follow the path {direction}.",
        $"Continue {direction} along the trail.",
        $"Head {direction} at the marker.",
        $"Take the {direction} fork in the path.",
        $"The route leads {direction} from here."
            ];

            return templates[random.Next(templates.Length)];
        }

        private TerrainType GetPredominantTerrainBetweenPoints(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            Dictionary<TerrainType, int> terrainCounts = [];
            int steps = (int)Math.Ceiling(Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2)));

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = new()
                {
                    X = start.X + ((end.X - start.X) * t),
                    Y = start.Y + ((end.Y - start.Y) * t)
                };

                if (terrainMap.TryGetValue(point, out TerrainType terrain))
                {
                    terrainCounts.TryGetValue(terrain, out int count);
                    terrainCounts[terrain] = count + 1;
                }
            }

            return terrainCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private string GenerateRegionName(TerrainType terrain)
        {
            (string[], string[]) nameComponents = terrain switch
            {
                TerrainType.Mountain => (
                    ["Frost", "Stone", "Cloud", "Storm", "Thunder"],
                    ["peaks", "heights", "summit", "range", "spires"]
                ),
                TerrainType.Forest => (
                    ["Green", "Dark", "Wild", "Shadow", "Ancient"],
                    ["woods", "forest", "grove", "thicket", "wilds"]
                ),
                TerrainType.Plains => (
                    ["Golden", "Wind", "Sun", "Green", "Rolling"],
                    ["plains", "fields", "meadows", "grasslands", "steppes"]
                ),
                TerrainType.Hills => (
                    ["Rolling", "Green", "Misty", "Emerald", "Pleasant"],
                    ["hills", "downs", "slopes", "rises", "highlands"]
                ),
                TerrainType.Swamp => (
                    ["Murk", "Mist", "Shadow", "Dark", "Dead"],
                    ["marsh", "swamp", "fen", "mire", "bogs"]
                ),
                _ => (
                    ["Wild", "Lost", "Far", "Forgotten", "Hidden"],
                    ["lands", "reach", "expanse", "territory", "realm"]
                )
            };

            return $"{nameComponents.Item1[random.Next(nameComponents.Item1.Length)]} " +
                   $"{nameComponents.Item2[random.Next(nameComponents.Item2.Length)]}";
        }

        private string GenerateRegionDescription(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Mountain =>
                    "Towering peaks pierce the clouds in this majestic mountain range.",
                TerrainType.Forest =>
                    "Ancient trees tower overhead in this dense forest.",
                TerrainType.Plains =>
                    "Rolling grasslands stretch toward the horizon.",
                TerrainType.Hills =>
                    "Gentle hills roll across the landscape.",
                TerrainType.Swamp =>
                    "Murky waters and twisted trees fill this forbidding swamp.",
                _ =>
                    "A wild and untamed region waiting to be explored."
            };
        }

        private Location GenerateSettlement(LocationType type, Vector2 position)
        {
            string name = GenerateSettlementName();
            Location settlement = new()
            {
                NameId = AddToStringPool(name),
                Type = type,
                DescriptionId = AddToStringPool($"A {type.ToString().ToLower()} nestled in the landscape."),
                Position = position,
                IsDiscovered = true, // Settlements start discovered
                ImportanceRating = type == LocationType.Town ? 1.0f : 0.7f
            };

            GenerateSettlementBuildings(settlement);
            return settlement;
        }

        private Vector2 FindLocationPosition(Vector2 center, List<Vector2> existingPositions, Dictionary<Vector2, TerrainType> terrainMap)
        {
            for (int attempts = 0; attempts < 20; attempts++)
            {
                float angle = random.NextSingle() * MathF.PI * 2;
                float distance = random.Next(MIN_LOCATION_SPACING, MIN_REGION_SIZE);

                Vector2 position = new()
                {
                    X = center.X + distance * MathF.Cos(angle),
                    Y = center.Y + distance * MathF.Sin(angle)
                };

                if (IsValidLocationPosition(position, existingPositions, terrainMap))
                {
                    return position;
                }
            }

            return new Vector2 { X = -1, Y = -1 }; // Invalid position
        }

        private LocationType DetermineLocationType(TerrainType regionTerrain, TerrainType localTerrain)
        {
            return (regionTerrain, localTerrain) switch
            {
                (TerrainType.Mountain, _) => random.Next(2) == 0 ? LocationType.Cave : LocationType.Peak,
                (TerrainType.Forest, _) => random.Next(2) == 0 ? LocationType.Camp : LocationType.Temple,
                (TerrainType.Hills, _) => random.Next(2) == 0 ? LocationType.Outpost : LocationType.Ruin,
                (TerrainType.Swamp, _) => random.Next(2) == 0 ? LocationType.Ruin : LocationType.Temple,
                (TerrainType.Plains, _) => random.Next(2) == 0 ? LocationType.Village : LocationType.Outpost,
                _ => LocationType.Landmark
            };
        }

        private Location GenerateLocation(LocationType type, Vector2 position)
        {
            string name = GenerateLocationName(type);
            return new Location
            {
                NameId = AddToStringPool(name),
                Type = type,
                DescriptionId = AddToStringPool(GenerateLocationDescription(type)),
                Position = position,
                IsDiscovered = false,
                ImportanceRating = random.NextSingle() * 0.5f
            };
        }

        private TerrainType GetPredominantTerrain(Dictionary<Vector2, TerrainType> terrainMap)
        {
            return terrainMap.GroupBy(kvp => kvp.Value)
                            .OrderByDescending(g => g.Count())
                            .First()
                            .Key;
        }

        private float CalculateRouteDifficulty(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            float distance = (float)Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            float terrainDifficulty = GetAverageTerrainDifficulty(start, end, terrainMap);
            return (distance * terrainDifficulty) / 100.0f; // Normalize to 0-1 range
        }

        private float CalculateSegmentEncounterRate(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            float baseRate = 0.3f;
            float terrainModifier = GetAverageTerrainDifficulty(start, end, terrainMap) / 10.0f;
            return Math.Clamp(baseRate + terrainModifier, 0.1f, 0.8f);
        }

        private string GenerateEncounterDescription(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Mountain => "A mountain creature blocks the path!",
                TerrainType.Forest => "Something rustles in the undergrowth...",
                TerrainType.Plains => "You spot movement in the distance.",
                TerrainType.Hills => "A figure watches from the hillside.",
                TerrainType.Swamp => "Strange sounds echo from the marsh.",
                _ => "You sense danger nearby."
            };
        }

        private string GenerateLandmarkDescription(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Mountain => "An impressive rock formation marks this spot.",
                TerrainType.Forest => "An ancient tree stands as a natural marker.",
                TerrainType.Plains => "A distinctive boulder serves as a waypoint.",
                TerrainType.Hills => "A scenic overlook provides a good vantage point.",
                TerrainType.Swamp => "A twisted old tree marks this location.",
                _ => "A notable landmark catches your attention."
            };
        }

        private float GetAverageTerrainDifficulty(Vector2 start, Vector2 end, Dictionary<Vector2, TerrainType> terrainMap)
        {
            float totalDifficulty = 0;
            int count = 0;

            // Sample points along the line
            for (float t = 0; t <= 1; t += 0.1f)
            {
                Vector2 point = new()
                {
                    X = start.X + (end.X - start.X) * t,
                    Y = start.Y + (end.Y - start.Y) * t
                };

                if (terrainMap.TryGetValue(point, out TerrainType terrain))
                {
                    totalDifficulty += GetTerrainDifficulty(terrain);
                    count++;
                }
            }

            return count > 0 ? totalDifficulty / count : 1.0f;
        }

        private float GetTerrainDifficulty(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Mountain => 2.0f,
                TerrainType.Hills => 1.5f,
                TerrainType.Swamp => 1.8f,
                TerrainType.Forest => 1.3f,
                TerrainType.River => 1.6f,
                _ => 1.0f
            };
        }

        private void GenerateSettlementBuildings(Location settlement)
        {
            bool isTown = settlement.Type == LocationType.Town;

            // Essential buildings
            List<string> buildingTypes =
            [
                "Inn",
        isTown ? "Town Hall" : "Meeting House",
        "Market",
        "Blacksmith"
            ];

            // Additional buildings based on size
            if (isTown)
            {
                buildingTypes.AddRange(
                [
                    "Temple",
            "Guard Post",
            "Warehouse",
            "Trading Post"
                ]);
            }

            // Reset NPC tracking for this settlement
            foreach (string buildingType in buildingTypes)
            {
                Building building = new()
                {
                    NameId = AddToStringPool(GenerateBuildingName(buildingType)),
                    DescriptionId = AddToStringPool(GenerateBuildingDescription(buildingType)),
                    Type = buildingType,
                    NPCs = GenerateNPCsForBuilding(buildingType),
                    Items = GenerateItemsForBuilding(buildingType)
                };

                settlement.Buildings.Add(building);
            }
        }

        private bool IsValidLocationPosition(Vector2 position, List<Vector2> existingPositions, Dictionary<Vector2, TerrainType> terrainMap)
        {
            // Check if position is within terrain map
            if (!terrainMap.ContainsKey(position))
                return false;

            // Check minimum distance from other locations
            if (existingPositions.Any(pos =>
                Math.Sqrt(Math.Pow(pos.X - position.X, 2) + Math.Pow(pos.Y - position.Y, 2)) < MIN_LOCATION_SPACING))
            {
                return false;
            }

            // Check terrain suitability
            TerrainType terrain = terrainMap[position];
            return terrain != TerrainType.River; // Can't build on rivers
        }

        private string GenerateLocationName(LocationType type)
        {
            return type switch
            {
                LocationType.Town => GenerateSettlementName(),
                LocationType.Village => GenerateSettlementName(),
                LocationType.Cave => GenerateName("Dark,Deep,Hidden,Ancient,Crystal", "Cave,Cavern,Grotto,Hollow"),
                LocationType.Ruin => GenerateName("Lost,Fallen,Ancient,Forgotten,Cursed", "Ruins,Temple,Tower,Fortress,City"),
                LocationType.Temple => GenerateName("Sacred,Divine,Holy,Ancient,Mystic", "Temple,Shrine,Sanctuary,Chapel"),
                LocationType.Camp => GenerateName("Hidden,Sheltered,Secure,Quiet,Secret", "Camp,Outpost,Haven,Rest"),
                LocationType.Peak => GenerateName("High,Lonely,Storm,Cloud,Thunder", "Peak,Summit,Spire,Top,Crown"),
                LocationType.Landmark => GenerateName("Notable,Strange,Mysterious,Ancient,Curious", "Stone,Monument,Marker,Sign"),
                _ => GenerateName("Hidden,Lost,Strange,Mysterious", "Place,Location,Site,Spot")
            };
        }

        private string GenerateLocationDescription(LocationType type)
        {
            return type switch
            {
                LocationType.Town => "A bustling settlement with many buildings and people.",
                LocationType.Village => "A small, peaceful settlement nestled in the landscape.",
                LocationType.Cave => "A dark opening leads into the earth, promising secrets within.",
                LocationType.Ruin => "Ancient stonework hints at forgotten glory.",
                LocationType.Temple => "An air of reverence surrounds this sacred place.",
                LocationType.Camp => "A temporary settlement that offers refuge to travelers.",
                LocationType.Peak => "The high point offers a commanding view of the surroundings.",
                LocationType.Landmark => "A notable feature that catches the eye.",
                _ => "An interesting location worth investigating."
            };
        }

        private string GenerateName(string prefixes, string suffixes)
        {
            string[] prefixArray = prefixes.Split(',');
            string[] suffixArray = suffixes.Split(',');

            return $"{prefixArray[random.Next(prefixArray.Length)]} " +
                   $"{suffixArray[random.Next(suffixArray.Length)]}";
        }

        private string GenerateBuildingName(string type)
        {
            return type switch
            {
                "Inn" => GenerateName("Golden,Silver,Sleeping,Weary,Restful", "Dragon,Lion,Bear,Swan,Knight"),
                "Market" => $"The {GenerateName("Old,Grand,Royal,Trade,Market", "Square,Plaza,Exchange,Hall,Court")}",
                "Blacksmith" => $"The {GenerateName("Iron,Steel,Forge,Anvil,Thunder", "Works,Smith,Forge,Arms,Metals")}",
                _ => $"The {type}"
            };
        }

        private string GenerateBuildingDescription(string type)
        {
            return type switch
            {
                "Inn" => "A welcoming establishment where travelers can rest and refresh themselves.",
                "Town Hall" => "The center of local governance and community gatherings.",
                "Meeting House" => "A simple building where the community gathers.",
                "Market" => "A busy place where goods and services are traded.",
                "Blacksmith" => "The ring of hammer on anvil sounds from this essential workshop.",
                "Temple" => "A place of worship and contemplation.",
                "Guard Post" => "Alert guards keep watch over the settlement from here.",
                "Warehouse" => "A sturdy building for storing goods and supplies.",
                "Trading Post" => "Merchants gather here to conduct business.",
                _ => $"A typical {type.ToLower()}."
            };
        }

        private EntityStats GenerateEntityStats()
        {
            return new EntityStats
            {
                Strength = random.Next(5, 15),
                Dexterity = random.Next(5, 15),
                Intelligence = random.Next(5, 15),
                Defense = random.Next(5, 15)
            };
        }

        private List<int> GenerateNPCDialogue()
        {
            List<int> dialogueRefs = [];
            int count = random.Next(2, 5);

            for (int i = 0; i < count; i++)
            {
                dialogueRefs.Add(random.Next(0, 10)); // Reference to shared dialogue pool
            }

            return dialogueRefs;
        }

        private string GenerateItemDescription(string name)
        {
            string[] templates = [
                $"A well-crafted {name.ToLower()}.",
        $"An interesting {name.ToLower()} of unknown origin.",
        $"A useful {name.ToLower()} in good condition.",
        $"This {name.ToLower()} appears to be quite valuable.",
        $"A rather ordinary looking {name.ToLower()}."
            ];

            return templates[random.Next(templates.Length)];
        }

        private ItemStats GenerateItemStats()
        {
            return new ItemStats
            {
                Value = random.Next(1, 100),
                Weight = random.Next(1, 10),
                Durability = random.Next(50, 100),
                Type = (ItemType)random.Next(0, 5)
            };
        }

        private List<int> GenerateNPCsForBuilding(string buildingType)
        {
            // Determine NPC count based on building type
            int count = buildingType switch
            {
                "Inn" => random.Next(2, 4),
                "Market" => random.Next(3, 6),
                "Guard Post" => random.Next(2, 4),
                "Temple" => random.Next(2, 3),
                _ => random.Next(1, 3)
            };

            List<int> npcs = [];
            // Track generated NPCs globally
            var worldData = _worldData ?? throw new InvalidOperationException("World data not initialized");

            for (int i = 0; i < count; i++)
            {
                // Create new NPC
                Entity npc = GenerateNPC();

                // Add NPC to global list and get its index
                int npcIndex = worldData.NPCs.Count;
                worldData.NPCs.Add(npc);

                // Store the valid index
                npcs.Add(npcIndex);
            }
            return npcs;
        }

        private List<int> GenerateItemsForBuilding(string buildingType)
        {
            int count = buildingType switch
            {
                "Market" => random.Next(5, 10),
                "Blacksmith" => random.Next(3, 6),
                "Trading Post" => random.Next(4, 8),
                "Warehouse" => random.Next(3, 7),
                _ => random.Next(1, 4)
            };

            List<int> items = [];
            for (int i = 0; i < count; i++)
            {
                items.Add(random.Next(100)); // Reference to global item pool
            }
            return items;
        }

        private TerrainType GetDominantTerrain(Vector2 center)
        {
            Dictionary<TerrainType, int> terrainCounts = [];
            int radius = 5;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    Vector2 pos = new()
                    {
                        X = center.X + dx,
                        Y = center.Y + dy
                    };

                    if (worldMap.TryGetValue(pos, out TerrainType terrain))
                    {
                        terrainCounts.TryGetValue(terrain, out int count);
                        terrainCounts[terrain] = count + 1;
                    }
                }
            }

            return terrainCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }
    }
}