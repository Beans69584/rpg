using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RPG
{
    /// <summary>
    /// Configuration for the world generation process.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="OptimizedWorldBuilder"/> class.
    /// </remarks>
    /// <param name="outputPath">The output directory for the generated world data.</param>
    /// <param name="sourceConfig">The source configuration for the world generation.</param>
    public class OptimizedWorldBuilder(string outputPath, WorldConfig sourceConfig)
    {
        private readonly string _outputPath = outputPath;
        private readonly WorldConfig _sourceConfig = sourceConfig;
        private readonly WorldData _data = new();

        /// <summary>
        /// Builds the world data from the source configuration.
        /// </summary>
        public void Build()
        {
            // Initialize header
            _data.Header = new Header
            {
                Name = _sourceConfig.Name,
                Description = _sourceConfig.Description,
                CreatedAt = DateTime.UtcNow
            };

            // Build string pool and resource tables
            BuildResourceTables();

            // Convert regions
            foreach (RegionConfig regionConfig in _sourceConfig.Regions)
            {
                WorldRegion region = new()
                {
                    NameId = GetOrAddString(regionConfig.Name),
                    DescriptionId = GetOrAddString(regionConfig.Description),
                    Position = new Vector2 { X = Random.Shared.Next(-100, 100), Y = Random.Shared.Next(-100, 100) }
                };

                // Convert locations
                foreach (LocationConfig locationConfig in regionConfig.Locations)
                {
                    Location location = new()
                    {
                        NameId = GetOrAddString(locationConfig.Name),
                        TypeId = GetOrAddString(locationConfig.Type),
                        DescriptionId = GetOrAddString(locationConfig.Description),
                        NPCs = [.. locationConfig.NPCs
                            .Select(npc => _sourceConfig.NPCs.IndexOf(npc))
                            .Where(idx => idx != -1)],
                        Items = [.. locationConfig.Items
                            .Select(item => _sourceConfig.Items.IndexOf(item))
                            .Where(idx => idx != -1)]
                    };
                    region.Locations.Add(location);
                }

                // Convert references to indices
                region.Connections = [.. regionConfig.Connections
                    .Select(c => _sourceConfig.Regions.FindIndex(r => r.Name == c))
                    .Where(idx => idx != -1)];

                // Convert routes
                foreach (int connection in region.Connections)
                {
                    string targetName = _sourceConfig.Regions[connection].Name;
                    if (regionConfig.Routes.TryGetValue(targetName, out List<RoutePoint>? routePoints))
                    {
                        region.Routes[connection] = routePoints;
                    }
                }

                region.NPCs = [.. regionConfig.NPCs
                    .Select(npc => _sourceConfig.NPCs.IndexOf(npc))
                    .Where(idx => idx != -1)];

                region.Items = [.. regionConfig.Items
                    .Select(item => _sourceConfig.Items.IndexOf(item))
                    .Where(idx => idx != -1)];

                _data.Regions.Add(region);
            }

            // Convert NPCs
            foreach (string npcName in _sourceConfig.NPCs)
            {
                Entity npc = new()
                {
                    NameId = GetOrAddString(npcName),
                    Level = Random.Shared.Next(1, 10),
                    HP = Random.Shared.Next(50, 100),
                    Stats = GenerateRandomStats(),
                    DialogueRefs = AssignRandomDialogue()
                };
                _data.NPCs.Add(npc);
            }

            // Convert items
            foreach (string itemName in _sourceConfig.Items)
            {
                Item item = new()
                {
                    NameId = GetOrAddString(itemName),
                    DescriptionId = GetOrAddString($"A {itemName.ToLower()} of unknown origin."),
                    Stats = new ItemStats
                    {
                        Value = Random.Shared.Next(1, 100),
                        Weight = Random.Shared.Next(1, 10),
                        Durability = Random.Shared.Next(50, 100),
                        Type = (ItemType)Random.Shared.Next(0, 5)
                    }
                };
                _data.Items.Add(item);
            }

            // Update counts in header
            _data.Header.RegionCount = _data.Regions.Count;
            _data.Header.NPCCount = _data.NPCs.Count;
            _data.Header.ItemCount = _data.Items.Count;

            // Save to binary-like format
            SaveWorld();
        }

        private void BuildResourceTables()
        {
            // Add common dialogue to shared pool
            _data.Resources.SharedDialogue.AddRange(
            [
            "Hello traveler!",
            "Nice weather we're having.",
            "Safe travels!",
            "I have wares if you have coin.",
            "These are dangerous times.",
            "Watch yourself out there."
        ]);
        }

        private int GetOrAddString(string str)
        {
            if (!_data.Resources.StringPool.TryGetValue(str, out int id))
            {
                id = _data.Resources.StringPool.Count;
                _data.Resources.StringPool[str] = id;
            }
            return id;
        }

        private EntityStats GenerateRandomStats()
        {
            return new EntityStats
            {
                Strength = Random.Shared.Next(1, 20),
                Dexterity = Random.Shared.Next(1, 20),
                Intelligence = Random.Shared.Next(1, 20),
                Defense = Random.Shared.Next(1, 20)
            };
        }

        private List<int> AssignRandomDialogue()
        {
            return [.. Enumerable.Range(0, Random.Shared.Next(2, 4))
                .Select(_ => Random.Shared.Next(0, _data.Resources.SharedDialogue.Count))
                .Distinct()];
        }

        private void SaveWorld()
        {
            string worldPath = Path.Combine(_outputPath, "world.dat");
            Directory.CreateDirectory(_outputPath);

            JsonSerializerOptions options = new()
            {
                WriteIndented = false, // Compact format
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Serialize to JSON bytes
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data, options);

            // Compress using GZip
            using FileStream fs = File.Create(worldPath);
            using System.IO.Compression.GZipStream gzip = new(
                fs,
                System.IO.Compression.CompressionLevel.Optimal);
            gzip.Write(jsonBytes, 0, jsonBytes.Length);
        }
    }
    /// <summary>
    /// Represents the configuration for a generated world.
    /// </summary>
    public class ProceduralWorldGenerator
    {
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;
        private readonly float[,] _heightMap;
        private readonly float[,] _moistureMap;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProceduralWorldGenerator"/> class.
        /// </summary>
        /// <param name="seed">The seed value for the random number generator.</param>
        /// <param name="width">The width of the generated world.</param>
        /// <param name="height">The height of the generated world.</param>
        public ProceduralWorldGenerator(int seed = 0, int width = 100, int height = 100)
        {
            _random = seed == 0 ? Random.Shared : new Random(seed);
            _width = width;
            _height = height;
            _heightMap = new float[width, height];
            _moistureMap = new float[width, height];

            GenerateTerrain();
        }

        private void GenerateTerrain()
        {
            // Generate height map using multiple octaves of Perlin noise
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float nx = x / (float)_width;
                    float ny = y / (float)_height;

                    _heightMap[x, y] = GenerateOctaveNoise(nx, ny, 6, 0.5f);
                    _moistureMap[x, y] = GenerateOctaveNoise(nx + 1000, ny + 1000, 4, 0.6f);
                }
            }
        }

        private float GenerateOctaveNoise(float x, float y, int octaves, float persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        private float Noise(float x, float y)
        {
            // Simple implementation of 2D noise
            float xf = x - (int)x;
            float yf = y - (int)y;

            float u = Fade(xf);
            float v = Fade(yf);

            return Lerp(v,
                Lerp(u,
                    Grad(_random.Next(256), xf, yf),
                    Grad(_random.Next(256), xf - 1, yf)),
                Lerp(u,
                    Grad(_random.Next(256), xf, yf - 1),
                    Grad(_random.Next(256), xf - 1, yf - 1)));
        }

        private float Fade(float t)
        {
            return t * t * t * ((t * ((t * 6) - 15)) + 10);
        }

        private float Lerp(float t, float a, float b)
        {
            return a + (t * (b - a));
        }

        private float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
        }

        /// <summary>
        /// Generates a new world configuration based on the current settings.
        /// </summary>
        /// <returns>A new <see cref="WorldConfig"/> instance representing the generated world.</returns>
        public WorldConfig GenerateWorld()
        {
            WorldConfig config = new()
            {
                Name = GenerateWorldName(),
                Description = "A procedurally generated realm with diverse landscapes and hidden mysteries.",
                Regions = [],
                NPCs = [],
                Items = []
            };

            // Find interesting points for regions
            List<(int x, int y)> regions = FindRegionLocations();

            // Generate regions and their connections
            foreach ((int x, int y) in regions)
            {
                string regionType = DetermineRegionType(x, y);
                RegionConfig region = GenerateRegion(regionType);
                config.Regions.Add(region);
            }

            // Connect nearby regions
            GenerateConnections(config.Regions, regions);

            // Collect all unique NPCs and items
            config.NPCs = [.. config.Regions.SelectMany(r => r.NPCs).Distinct()];
            config.Items = [.. config.Regions.SelectMany(r => r.Items).Distinct()];

            return config;
        }

        private List<(int x, int y)> FindRegionLocations()
        {
            List<(int x, int y)> locations = [];
            int minDistance = 10; // Minimum distance between regions

            // Find local maxima and interesting points in the heightmap
            for (int x = 5; x < _width - 5; x += 5)
            {
                for (int y = 5; y < _height - 5; y += 5)
                {
                    if (IsInterestingLocation(x, y) &&
                        !locations.Exists(l => Distance(l, (x, y)) < minDistance))
                    {
                        locations.Add((x, y));
                    }
                }
            }

            return locations;
        }

        private bool IsInterestingLocation(int x, int y)
        {
            float height = _heightMap[x, y];
            float moisture = _moistureMap[x, y];

            // Check if this point is a local maximum or has interesting features
            return IsLocalMaximum(x, y) ||
                   (height > 0.7f) ||  // Mountains
                   (height < 0.3f && moisture > 0.6f) || // Lakes/Swamps
                   (height > 0.4f && height < 0.6f && moisture > 0.5f); // Forests
        }

        private bool IsLocalMaximum(int x, int y)
        {
            float value = _heightMap[x, y];
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (x + dx < 0 || x + dx >= _width || y + dy < 0 || y + dy >= _height) continue;
                    if (_heightMap[x + dx, y + dy] > value) return false;
                }
            }
            return true;
        }

        private string DetermineRegionType(int x, int y)
        {
            float height = _heightMap[x, y];
            float moisture = _moistureMap[x, y];

            if (height > 0.7f) return "Mountain";
            if (height > 0.6f) return "Hills";
            if (height < 0.3f && moisture > 0.6f) return "Lake";
            if (height < 0.4f && moisture > 0.5f) return "Swamp";
            if (moisture > 0.6f) return "Forest";
            return moisture < 0.3f ? "Plains" : "Valley";
        }

        private RegionConfig GenerateRegion(string type)
        {
            RegionConfig region = new()
            {
                Name = GenerateRegionName(type),
                Description = GenerateRegionDescription(type),
                Connections = [],
                NPCs = GenerateNPCsForType(type),
                Items = GenerateItemsForType(type),
                Locations = [],
                Routes = []
            };

            // Generate locations based on region type
            if (type == "Valley" || type == "Plains" || _random.Next(100) < 40)
            {
                GenerateSettlement(region);
            }

            // Generate route points between regions
            foreach (string connection in region.Connections)
            {
                region.Routes[connection] = GenerateRoutePath();
            }

            return region;
        }

        private void GenerateSettlement(RegionConfig region)
        {
            // Determine settlement size and type
            string size = _random.Next(100) switch
            {
                > 90 => "large",
                > 70 => "medium",
                _ => "small"
            };

            int locationCount = size switch
            {
                "large" => _random.Next(8, 12),
                "medium" => _random.Next(5, 8),
                _ => _random.Next(3, 5)
            };

            // Essential locations based on size
            string[] essentialLocations = size switch
            {
                "large" => ["Town Hall", "Market", "Inn", "Temple", "Blacksmith"],
                "medium" => ["Inn", "Market", "Chapel", "Blacksmith"],
                _ => ["Inn", "Trading Post"]
            };

            // Add essential locations
            foreach (string? locType in essentialLocations)
            {
                region.Locations.Add(GenerateLocation(locType));
            }

            // Add random additional locations
            string[] additionalTypes =
            [
            "House", "Farm", "Workshop", "Store", "Stable",
            "Garden", "Well", "Warehouse", "Watch Post", "Mill"
        ];

            for (int i = essentialLocations.Length; i < locationCount; i++)
            {
                string locType = additionalTypes[_random.Next(additionalTypes.Length)];
                region.Locations.Add(GenerateLocation(locType));
            }
        }

        private LocationConfig GenerateLocation(string type)
        {
            return new LocationConfig
            {
                Name = GenerateLocationName(type),
                Type = type,
                Description = GenerateLocationDescription(type),
                NPCs = GenerateNPCsForLocation(type),
                Items = GenerateItemsForLocation(type)
            };
        }

        private string GenerateLocationName(string type)
        {
            Dictionary<string, string[]> prefixes = new()
            {
                ["Inn"] = ["Traveler's", "Weary", "Golden", "Silver", "Old"],
                ["Market"] = ["Town", "Trade", "Market", "Merchant's", "Commons"],
                ["Temple"] = ["Sacred", "Holy", "Divine", "Blessed", "Ancient"],
                ["Blacksmith"] = ["Iron", "Forge", "Anvil", "Steel", "Smith's"],
                ["Trading Post"] = ["Frontier", "Trader's", "Merchant's", "Caravan", "Waypoint"]
            };

            Dictionary<string, string[]> suffixes = new()
            {
                ["Inn"] = ["Rest", "Lodge", "Inn", "Haven", "House"],
                ["Market"] = ["Square", "Plaza", "Market", "Exchange", "Grounds"],
                ["Temple"] = ["Temple", "Sanctuary", "Chapel", "Shrine", "Cathedral"],
                ["Blacksmith"] = ["Forge", "Workshop", "Smith", "Works", "Anvil"],
                ["Trading Post"] = ["Post", "House", "Store", "Exchange", "Shop"]
            };

            if (prefixes.TryGetValue(type, out string[]? typePrefix) && suffixes.TryGetValue(type, out string[]? typeSuffix))
            {
                return $"{typePrefix[_random.Next(typePrefix.Length)]} {typeSuffix[_random.Next(typeSuffix.Length)]}";
            }

            return $"{type}";
        }

        private List<RoutePoint> GenerateRoutePath()
        {
            List<RoutePoint> points = [];
            int pathLength = _random.Next(2, 5);

            for (int i = 0; i < pathLength; i++)
            {
                RoutePoint point = new()
                {
                    Description = GenerateRouteDescription(),
                    Directions = GenerateRouteDirections(),
                    Landmarks = GenerateRouteLandmarks()
                };
                points.Add(point);
            }

            return points;
        }

        private string GenerateRouteDescription()
        {
            string[] descriptions =
            [
            "The path winds through dense vegetation.",
            "A well-worn trail stretches ahead.",
            "The route follows an ancient stone road.",
            "A narrow path hugs the hillside.",
            "The track crosses a shallow stream.",
            "A bridge spans a deep ravine here."
        ];
            return descriptions[_random.Next(descriptions.Length)];
        }

        private string GenerateRouteDirections()
        {
            string[] directions =
            [
            "Follow the path north past the large boulder.",
            "Continue east along the stream.",
            "Head uphill toward the cliff face.",
            "Take the fork in the road heading west.",
            "Cross the wooden bridge and continue straight.",
            "Follow the markers through the valley."
        ];
            return directions[_random.Next(directions.Length)];
        }

        private List<LocationConfig> GenerateRouteLandmarks()
        {
            List<LocationConfig> landmarks = [];
            if (_random.Next(100) < 30) // 30% chance for a landmark
            {
                string[] types =
                [
                "Ancient Ruins", "Watch Tower", "Abandoned Fort",
                "Old Shrine", "Cave Entrance", "Stone Circle",
                "Abandoned Mill", "Bridge", "Wayshrine",
                "Campsite", "Trading Post", "Mystery"
            ];

                string type = types[_random.Next(types.Length)];
                landmarks.Add(new LocationConfig
                {
                    Name = GenerateLocationName(type),
                    Type = type,
                    Description = GenerateLocationDescription(type),
                    NPCs = GenerateNPCsForLocation(type),
                    Items = GenerateItemsForLocation(type)
                });
            }
            return landmarks;
        }

        private void GenerateConnections(List<RegionConfig> regions, List<(int x, int y)> positions)
        {
            // Connect each region to its 2-4 nearest neighbors
            for (int i = 0; i < regions.Count; i++)
            {
                List<(int index, double dist)> distances = [];
                for (int j = 0; j < regions.Count; j++)
                {
                    if (i == j) continue;
                    distances.Add((j, Distance(positions[i], positions[j])));
                }

                // Sort by distance and take 2-4 nearest
                int connectionCount = _random.Next(2, 5);
                List<string> connections = [.. distances
                    .OrderBy(d => d.dist)
                    .Take(connectionCount)
                    .Select(d => regions[d.index].Name)];

                regions[i].Connections = connections;
            }
        }

        private string GenerateWorldName()
        {
            string[] prefixes = ["Northern", "Eastern", "Western", "Southern", "Lost", "Ancient", "Wild"];
            string[] suffixes = ["Frontier", "Reaches", "Lands", "Territory", "Province", "Region", "Domain"];
            return $"The {prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
        }

        private string GenerateRegionName(string type)
        {
            Dictionary<string, string[]> prefixes = new()
            {
                ["Mountain"] = ["High", "Steep", "Rocky", "Stone", "Frost"],
                ["Hills"] = ["Rolling", "Green", "Windy", "Low", "Grassy"],
                ["Lake"] = ["Deep", "Clear", "Still", "Mirror", "Dark"],
                ["Swamp"] = ["Murky", "Misty", "Fog", "Reed", "Marsh"],
                ["Forest"] = ["Dense", "Old", "Wild", "Deep", "Shadow"],
                ["Plains"] = ["Open", "Vast", "Wide", "Windy", "Golden"],
                ["Valley"] = ["Hidden", "Quiet", "Green", "Peaceful", "Sheltered"]
            };

            Dictionary<string, string[]> suffixes = new()
            {
                ["Mountain"] = ["Peak", "Ridge", "Summit", "Heights", "Pass"],
                ["Hills"] = ["Hills", "Slopes", "Rise", "Downs", "Highlands"],
                ["Lake"] = ["Lake", "Waters", "Pool", "Basin", "Mere"],
                ["Swamp"] = ["Marsh", "Bog", "Fen", "Swamp", "Mire"],
                ["Forest"] = ["Woods", "Forest", "Grove", "Thicket", "Woodland"],
                ["Plains"] = ["Plains", "Fields", "Grassland", "Meadow", "Prairie"],
                ["Valley"] = ["Valley", "Vale", "Dale", "Glen", "Bottom"]
            };

            string prefix = prefixes[type][_random.Next(prefixes[type].Length)];
            string suffix = suffixes[type][_random.Next(suffixes[type].Length)];
            return $"{prefix} {suffix}";
        }

        private string GenerateRegionDescription(string type)
        {
            Dictionary<string, string[]> descriptions = new()
            {
                ["Mountain"] = [
                "Steep cliffs rise sharply against the sky, their peaks shrouded in clouds.",
                "A rugged landscape of stone and snow stretches upward into thin air.",
                "Rocky paths wind between towering peaks of weathered stone."
            ],
                ["Hills"] = [
                "Gentle slopes roll endlessly toward the horizon, covered in wild grass.",
                "Wind-swept hills dotted with hardy shrubs and exposed rocks.",
                "A series of low rises and dips create a peaceful, pastoral landscape."
            ],
                ["Lake"] = [
                "Clear waters reflect the sky like a mirror, surrounded by reeds.",
                "A calm body of water stretches into the distance, its surface occasionally broken by fish.",
                "The lake's deep waters lap gently at its rocky shores."
            ],
                ["Swamp"] = [
                "Murky water pools between twisted trees draped with moss.",
                "A maze of waterways winds through dense vegetation and muddy ground.",
                "Mist clings to the surface of dark water, obscuring what lies beneath."
            ],
                ["Forest"] = [
                "Ancient trees stand like silent sentinels, their canopy blocking most light.",
                "A dense woodland of old growth trees and tangled underbrush.",
                "Filtered sunlight creates patterns on the forest floor."
            ],
                ["Plains"] = [
                "Tall grass waves in the wind like a golden sea under open skies.",
                "A vast expanse of open ground stretches to the horizon.",
                "Wild grasses and scattered wildflowers cover the level ground."
            ],
                ["Valley"] = [
                "Sheltered by high ground on either side, the valley offers protection from harsh winds.",
                "A peaceful lowland nestled between higher terrain.",
                "Rich soil and protected position make this an ideal settlement location."
            ]
            };

            return descriptions[type][_random.Next(descriptions[type].Length)];
        }

        private List<string> GenerateNPCsForType(string type)
        {
            List<string> npcs = [];
            int count = _random.Next(2, 5);

            Dictionary<string, string[]> npcTypes = new()
            {
                ["Mountain"] = ["Miner", "Guide", "Climber", "Scout", "Hunter"],
                ["Hills"] = ["Shepherd", "Farmer", "Hunter", "Guard", "Traveler"],
                ["Lake"] = ["Fisher", "Boatman", "Merchant", "Guard", "Dock Worker"],
                ["Swamp"] = ["Hunter", "Guide", "Herbalist", "Fisher", "Recluse"],
                ["Forest"] = ["Woodcutter", "Hunter", "Ranger", "Trapper", "Scout"],
                ["Plains"] = ["Farmer", "Herder", "Merchant", "Guard", "Scout"],
                ["Valley"] = ["Farmer", "Miller", "Trader", "Guard", "Worker"]
            };

            for (int i = 0; i < count; i++)
            {
                npcs.Add(npcTypes[type][_random.Next(npcTypes[type].Length)]);
            }

            return [.. npcs.Distinct()];
        }

        private List<string> GenerateItemsForType(string type)
        {
            List<string> items = [];
            int count = _random.Next(3, 6);

            Dictionary<string, string[]> itemTypes = new()
            {
                ["Mountain"] = ["Pickaxe", "Rope", "Lantern", "Iron Ore", "Climbing Gear"],
                ["Hills"] = ["Walking Staff", "Water Skin", "Dried Food", "Wool", "Tools"],
                ["Lake"] = ["Fishing Rod", "Net", "Fresh Fish", "Boat Hook", "Rope"],
                ["Swamp"] = ["Boots", "Herbs", "Walking Staff", "Medicine", "Torch"],
                ["Forest"] = ["Axe", "Bow", "Herbs", "Leather", "Wood"],
                ["Plains"] = ["Farming Tools", "Seeds", "Water Skin", "Cart", "Food"],
                ["Valley"] = ["Tools", "Grain", "Cart", "Trade Goods", "Food"]
            };

            for (int i = 0; i < count; i++)
            {
                items.Add(itemTypes[type][_random.Next(itemTypes[type].Length)]);
            }

            return [.. items.Distinct()];
        }

        private double Distance((int x, int y) a, (int x, int y) b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private string GenerateLocationDescription(string type)
        {
            Dictionary<string, string[]> descriptions = new()
            {
                ["Inn"] = [
                "A cozy establishment with a warm hearth and the smell of fresh bread.",
                "The sound of lively chatter fills this well-maintained inn.",
                "A popular rest stop for weary travelers, known for its comfortable beds."
            ],
                ["Market"] = [
                "Colorful stalls line the busy marketplace, filled with goods from all over.",
                "Merchants call out their wares as customers haggle over prices.",
                "A bustling center of trade where locals gather to buy and sell."
            ],
                ["Temple"] = [
                "Peaceful silence fills this sacred space, broken only by quiet prayer.",
                "Sunlight streams through colored windows onto stone floors.",
                "An ancient place of worship, maintained with careful dedication."
            ],
                ["Blacksmith"] = [
                "The ring of hammer on anvil echoes from the busy forge.",
                "Heat radiates from the glowing forge as tools take shape.",
                "A well-equipped workshop where quality weapons and tools are made."
            ],
                ["Trading Post"] = [
                "A sturdy building where traders exchange goods and news.",
                "Shelves lined with goods from distant lands fill this trading post.",
                "A busy stop along the trade route, always full of travelers."
            ],
                ["House"] = [
                "A modest dwelling with a small garden.",
                "Smoke rises from the chimney of this comfortable home.",
                "A well-maintained house with flowers in the windows."
            ],
                ["Farm"] = [
                "Fields of crops stretch out behind the farmhouse.",
                "A working farm with various animals and growing fields.",
                "The smell of fresh hay wafts from the barn."
            ],
                ["Workshop"] = [
                "Tools and materials fill this busy craftsman's space.",
                "The sound of work echoes from within.",
                "A place where skilled artisans practice their trade."
            ],
                ["Ancient Ruins"] = [
                "Crumbling stone walls hint at past grandeur.",
                "Mystery surrounds these weathered ruins.",
                "Time-worn stones covered in creeping vines."
            ],
                ["Watch Tower"] = [
                "A tall structure providing views of the surrounding area.",
                "Guards keep vigilant watch from this strategic point.",
                "A defensive position overlooking the landscape."
            ]
            };

            if (descriptions.TryGetValue(type, out string[]? typeDescriptions))
            {
                return typeDescriptions[_random.Next(typeDescriptions.Length)];
            }

            return $"A typical {type.ToLower()}.";
        }

        private List<string> GenerateNPCsForLocation(string type)
        {
            List<string> npcs = [];
            int count = _random.Next(1, 4);

            Dictionary<string, string[]> npcTypes = new()
            {
                ["Inn"] = ["Innkeeper", "Barmaid", "Cook", "Patron", "Traveler"],
                ["Market"] = ["Merchant", "Shopper", "Guard", "Vendor", "Pickpocket"],
                ["Temple"] = ["Priest", "Acolyte", "Worshipper", "Pilgrim", "Healer"],
                ["Blacksmith"] = ["Smith", "Apprentice", "Customer", "Supplier"],
                ["Trading Post"] = ["Trader", "Merchant", "Guard", "Porter", "Customer"],
                ["House"] = ["Resident", "Family Member", "Visitor"],
                ["Farm"] = ["Farmer", "Farmhand", "Worker", "Animal Handler"],
                ["Workshop"] = ["Craftsman", "Apprentice", "Customer", "Supplier"],
                ["Watch Tower"] = ["Guard", "Watchman", "Soldier", "Scout"],
                ["Ancient Ruins"] = ["Explorer", "Archaeologist", "Treasure Hunter", "Guard"]
            };

            if (npcTypes.TryGetValue(type, out string[]? typeNPCs))
            {
                for (int i = 0; i < count; i++)
                {
                    npcs.Add(typeNPCs[_random.Next(typeNPCs.Length)]);
                }
            }

            return [.. npcs.Distinct()];
        }

        private List<string> GenerateItemsForLocation(string type)
        {
            List<string> items = [];
            int count = _random.Next(2, 5);

            Dictionary<string, string[]> itemTypes = new()
            {
                ["Inn"] = ["Ale", "Bread", "Stew", "Bedroll", "Candle", "Key"],
                ["Market"] = ["Food", "Cloth", "Pottery", "Tools", "Jewelry", "Spices"],
                ["Temple"] = ["Candle", "Holy Symbol", "Incense", "Offering", "Scripture"],
                ["Blacksmith"] = ["Sword", "Armor", "Tools", "Metal", "Coal", "Hammer"],
                ["Trading Post"] = ["Map", "Supplies", "Trade Goods", "Food", "Equipment"],
                ["House"] = ["Furniture", "Dishes", "Tools", "Personal Items"],
                ["Farm"] = ["Tools", "Seeds", "Grain", "Feed", "Produce"],
                ["Workshop"] = ["Tools", "Materials", "Products", "Work Table"],
                ["Watch Tower"] = ["Weapons", "Armor", "Supplies", "Signal Horn"],
                ["Ancient Ruins"] = ["Artifact", "Relic", "Old Coin", "Broken Pottery"]
            };

            if (itemTypes.TryGetValue(type, out string[]? typeItems))
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(typeItems[_random.Next(typeItems.Length)]);
                }
            }

            return [.. items.Distinct()];
        }
    }

    /// <summary>
    /// Represents the configuration for a generated world.
    /// </summary>
    public class WorldConfig
    {
        /// <summary>
        /// Gets or sets the name of the world.
        /// </summary>
        public string Name { get; set; } = "Demo World";
        /// <summary>
        /// Gets or sets the description of the world.
        /// </summary>
        public string Description { get; set; } = "A sample RPG world";
        /// <summary>
        /// Gets or sets the list of regions in the world.
        /// </summary>
        public List<RegionConfig> Regions { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of NPCs in the world.
        /// </summary>
        public List<string> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of items in the world.
        /// </summary>
        public List<string> Items { get; set; } = [];
    }

    /// <summary>
    /// Represents the configuration for a region in the world.
    /// </summary>
    public class RegionConfig
    {
        /// <summary>
        /// Gets or sets the name of the region.
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the description of the region.
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// Gets or sets the list of connections to other regions.
        /// </summary>
        public List<string> Connections { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of NPCs in the region.
        /// </summary>
        public List<string> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of items in the region.
        /// </summary>
        public List<string> Items { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of locations in the region.
        /// </summary>
        public List<LocationConfig> Locations { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of routes to other regions.
        /// </summary>
        public Dictionary<string, List<RoutePoint>> Routes { get; set; } = [];
    }

    /// <summary>
    /// Represents the configuration for a location in the world.
    /// </summary>
    public class LocationConfig
    {
        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        public string Type { get; set; } = "";
        /// <summary>
        /// Gets or sets the description of the location.
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// Gets or sets the list of NPCs at the location.
        /// </summary>
        public List<string> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of items at the location.
        /// </summary>
        public List<string> Items { get; set; } = [];
    }

    /// <summary>
    /// Represents a point along a route between regions.
    /// </summary>
    public class RoutePoint
    {
        /// <summary>
        /// Gets or sets the description of the route point.
        /// </summary>
        public int DescriptionId { get; set; }
        /// <summary>
        /// Gets or sets the directions at the route point.
        /// </summary>
        public int DirectionsId { get; set; }
        /// <summary>
        /// Gets or sets the list of landmarks at the route point.
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// Gets or sets the directions at the route point.
        /// </summary>
        public string Directions { get; set; } = "";
        /// <summary>
        /// Gets or sets the list of landmarks at the route point.
        /// </summary>
        public List<LocationConfig> Landmarks { get; set; } = [];
    }
    /// <summary>
    /// Represents the header information for a generated world file.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Gets or sets the magic number identifying the file format.
        /// </summary>
        public string Magic { get; set; } = "RPGW";

        /// <summary>
        /// Gets or sets the name of the world.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the description of the world.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets the version of the world format.
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the creation timestamp of the world.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total number of regions in the world.
        /// </summary>
        public int RegionCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of NPCs in the world.
        /// </summary>
        public int NPCCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of items in the world.
        /// </summary>
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Contains resource mappings and shared data for the world.
    /// </summary>
    public class ResourceTable
    {
        /// <summary>
        /// Maps strings to their unique integer identifiers.
        /// </summary>
        public Dictionary<string, int> StringPool { get; set; } = [];
        /// <summary>
        /// Maps texture names to their resource identifiers.
        /// </summary>
        public Dictionary<string, int> TextureRefs { get; set; } = [];
        /// <summary>
        /// Maps sound effect names to their resource identifiers.
        /// </summary>
        public Dictionary<string, int> SoundRefs { get; set; } = [];
        /// <summary>
        /// Collection of common dialogue lines shared across NPCs.
        /// </summary>
        public List<string> SharedDialogue { get; set; } = [];
    }

    /// <summary>
    /// Represents a region in the generated world with its properties and connections.
    /// </summary>
    public class GenWorldRegion
    {
        /// <summary>
        /// Gets or sets the ID reference to the region name in the string pool.
        /// </summary>
        public int NameId { get; set; }          // Reference to string pool
        /// <summary>
        /// Gets or sets the ID reference to the region description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }   // Reference to string pool
        /// <summary>
        /// Gets or sets the list of indices referencing connected regions.
        /// </summary>
        public List<int> Connections { get; set; } = [];  // GenWorldRegion indices
        /// <summary>
        /// Gets or sets the list of indices referencing NPCs present in this region.
        /// </summary>
        public List<int> NPCs { get; set; } = [];        // NPC indices
        /// <summary>
        /// Gets or sets the list of indices referencing items found in this region.
        /// </summary>
        public List<int> Items { get; set; } = [];       // Item indices
        /// <summary>
        /// Gets or sets the 2D position of this region in the world.
        /// </summary>
        public Vector2 Position { get; set; } = new();
        /// <summary>
        /// Gets or sets the list of locations within this region.
        /// </summary>
        public List<Location> Locations { get; set; } = [];
        /// <summary>
        /// Gets or sets the dictionary mapping region indices to their route points.
        /// </summary>
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = [];
    }

    /// <summary>
    /// Represents a location within a region in the world.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Gets or sets the ID reference to the location name in the string pool.
        /// </summary>
        public int NameId { get; set; }
        /// <summary>
        /// Gets or sets the ID reference to the location type in the string pool.
        /// </summary>
        public int TypeId { get; set; }
        /// <summary>
        /// Gets or sets the ID reference to the location description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }
        /// <summary>
        /// Gets or sets the list of indices referencing NPCs present at this location.
        /// </summary>
        public List<int> NPCs { get; set; } = [];
        /// <summary>
        /// Gets or sets the list of indices referencing items found at this location.
        /// </summary>
        public List<int> Items { get; set; } = [];
    }

    /// <summary>
    /// Represents an entity in the game world, such as an NPC or creature.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Gets or sets the ID reference to the entity's name in the string pool.
        /// </summary>
        public int NameId { get; set; }          // Reference to string pool
        /// <summary>
        /// Gets or sets the experience level of the entity.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// Gets or sets the current hit points of the entity.
        /// </summary>
        public int HP { get; set; }
        /// <summary>
        /// Gets or sets the list of dialogue reference IDs available to this entity.
        /// </summary>
        public List<int> DialogueRefs { get; set; } = []; // References to shared dialogue
        /// <summary>
        /// Gets or sets the base statistics for this entity.
        /// </summary>
        public EntityStats Stats { get; set; } = new();
    }

    /// <summary>
    /// Represents an item in the game world with its properties and statistics.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Gets or sets the ID reference to the item's name in the string pool.
        /// </summary>
        public int NameId { get; set; }          // Reference to string pool
        /// <summary>
        /// Gets or sets the ID reference to the item's description in the string pool.
        /// </summary>
        public int DescriptionId { get; set; }   // Reference to string pool
        /// <summary>
        /// Gets or sets the statistics and attributes of the item.
        /// </summary>
        public ItemStats Stats { get; set; } = new();
    }

    /// <summary>
    /// Represents a two-dimensional vector with X and Y coordinates.
    /// </summary>
    public class Vector2
    {
        /// <summary>
        /// Gets or sets the X coordinate of the vector.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Gets or sets the Y coordinate of the vector.
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// Represents the base statistics for an entity in the game world.
    /// </summary>
    public class EntityStats
    {
        /// <summary>
        /// Gets or sets the physical power and melee damage capability of the entity.
        /// </summary>
        public int Strength { get; set; }
        /// <summary>
        /// Gets or sets the agility and precision of the entity.
        /// </summary>
        public int Dexterity { get; set; }
        /// <summary>
        /// Gets or sets the mental capacity and magical ability of the entity.
        /// </summary>
        public int Intelligence { get; set; }
        /// <summary>
        /// Gets or sets the damage resistance capability of the entity.
        /// </summary>
        public int Defense { get; set; }
    }

    /// <summary>
    /// Represents the statistical properties of an item in the game world.
    /// </summary>
    public class ItemStats
    {
        /// <summary>
        /// Gets or sets the monetary value of the item.
        /// </summary>
        public int Value { get; set; }
        /// <summary>
        /// Gets or sets the weight of the item in arbitrary units.
        /// </summary>
        public int Weight { get; set; }
        /// <summary>
        /// Gets or sets the durability rating of the item.
        /// </summary>
        public int Durability { get; set; }
        /// <summary>
        /// Gets or sets the categorical type of the item.
        /// </summary>
        public ItemType Type { get; set; }
    }

    /// <summary>
    /// Defines the different types of items that can exist in the game world.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// Represents items that can be used to deal damage.
        /// </summary>
        Weapon,
        /// <summary>
        /// Represents items that can be equipped for protection.
        /// </summary>
        Armor,
        /// <summary>
        /// Represents items that can be used once for their effects.
        /// </summary>
        Consumable,
        /// <summary>
        /// Represents items that are related to quests or missions.
        /// </summary>
        Quest,
        /// <summary>
        /// Represents miscellaneous items that don't fit other categories.
        /// </summary>
        Misc
    }
}