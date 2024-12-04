using System.Text;
using System.Text.Json;

namespace RPG
{
    public class OptimizedWorldBuilder
    {
        private readonly string _outputPath;
        private readonly WorldConfig _sourceConfig;
        private readonly WorldData _data;

        public OptimizedWorldBuilder(string outputPath, WorldConfig sourceConfig)
        {
            _outputPath = outputPath;
            _sourceConfig = sourceConfig;
            _data = new WorldData();
        }

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
                WorldRegion region = new WorldRegion
                {
                    NameId = GetOrAddString(regionConfig.Name),
                    DescriptionId = GetOrAddString(regionConfig.Description),
                    Position = new Vector2 { X = Random.Shared.Next(-100, 100), Y = Random.Shared.Next(-100, 100) }
                };

                // Convert locations
                foreach (LocationConfig locationConfig in regionConfig.Locations)
                {
                    Location location = new Location
                    {
                        NameId = GetOrAddString(locationConfig.Name),
                        TypeId = GetOrAddString(locationConfig.Type),
                        DescriptionId = GetOrAddString(locationConfig.Description),
                        NPCs = locationConfig.NPCs
                            .Select(npc => _sourceConfig.NPCs.IndexOf(npc))
                            .Where(idx => idx != -1)
                            .ToList(),
                        Items = locationConfig.Items
                            .Select(item => _sourceConfig.Items.IndexOf(item))
                            .Where(idx => idx != -1)
                            .ToList()
                    };
                    region.Locations.Add(location);
                }

                // Convert references to indices
                region.Connections = regionConfig.Connections
                    .Select(c => _sourceConfig.Regions.FindIndex(r => r.Name == c))
                    .Where(idx => idx != -1)
                    .ToList();

                // Convert routes
                foreach (int connection in region.Connections)
                {
                    string targetName = _sourceConfig.Regions[connection].Name;
                    if (regionConfig.Routes.TryGetValue(targetName, out List<RoutePoint>? routePoints))
                    {
                        region.Routes[connection] = routePoints;
                    }
                }

                region.NPCs = regionConfig.NPCs
                    .Select(npc => _sourceConfig.NPCs.IndexOf(npc))
                    .Where(idx => idx != -1)
                    .ToList();

                region.Items = regionConfig.Items
                    .Select(item => _sourceConfig.Items.IndexOf(item))
                    .Where(idx => idx != -1)
                    .ToList();

                _data.Regions.Add(region);
            }

            // Convert NPCs
            foreach (string npcName in _sourceConfig.NPCs)
            {
                Entity npc = new Entity
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
                Item item = new Item
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
            _data.Resources.SharedDialogue.AddRange(new[]
            {
            "Hello traveler!",
            "Nice weather we're having.",
            "Safe travels!",
            "I have wares if you have coin.",
            "These are dangerous times.",
            "Watch yourself out there."
        });
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
            return Enumerable.Range(0, Random.Shared.Next(2, 4))
                .Select(_ => Random.Shared.Next(0, _data.Resources.SharedDialogue.Count))
                .Distinct()
                .ToList();
        }

        private void SaveWorld()
        {
            string worldPath = Path.Combine(_outputPath, "world.dat");
            Directory.CreateDirectory(_outputPath);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = false, // Compact format
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Serialize to JSON bytes
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data, options);

            // Compress using GZip
            using FileStream fs = File.Create(worldPath);
            using System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(
                fs,
                System.IO.Compression.CompressionLevel.Optimal);
            gzip.Write(jsonBytes, 0, jsonBytes.Length);
        }
    }
    public class ProceduralWorldGenerator
    {
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;
        private readonly float[,] _heightMap;
        private readonly float[,] _moistureMap;

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
            int xi = (int)x & 255;
            int yi = (int)y & 255;
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

        private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        private float Lerp(float t, float a, float b) => a + t * (b - a);

        private float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
        }

        public WorldConfig GenerateWorld()
        {
            WorldConfig config = new WorldConfig
            {
                Name = GenerateWorldName(),
                Description = "A procedurally generated realm with diverse landscapes and hidden mysteries.",
                Regions = new List<RegionConfig>(),
                NPCs = new List<string>(),
                Items = new List<string>()
            };

            // Find interesting points for regions
            List<(int x, int y)> regions = FindRegionLocations();

            // Generate regions and their connections
            foreach ((int x, int y) point in regions)
            {
                string regionType = DetermineRegionType(point.x, point.y);
                RegionConfig region = GenerateRegion(regionType, point.x, point.y);
                config.Regions.Add(region);
            }

            // Connect nearby regions
            GenerateConnections(config.Regions, regions);

            // Collect all unique NPCs and items
            config.NPCs = config.Regions.SelectMany(r => r.NPCs).Distinct().ToList();
            config.Items = config.Regions.SelectMany(r => r.Items).Distinct().ToList();

            return config;
        }

        private List<(int x, int y)> FindRegionLocations()
        {
            List<(int x, int y)> locations = new List<(int x, int y)>();
            int minDistance = 10; // Minimum distance between regions

            // Find local maxima and interesting points in the heightmap
            for (int x = 5; x < _width - 5; x += 5)
            {
                for (int y = 5; y < _height - 5; y += 5)
                {
                    if (IsInterestingLocation(x, y) &&
                        !locations.Any(l => Distance(l, (x, y)) < minDistance))
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
            if (moisture < 0.3f) return "Plains";
            return "Valley";
        }

        private RegionConfig GenerateRegion(string type, int x, int y)
        {
            RegionConfig region = new RegionConfig
            {
                Name = GenerateRegionName(type),
                Description = GenerateRegionDescription(type),
                Connections = new List<string>(),
                NPCs = GenerateNPCsForType(type),
                Items = GenerateItemsForType(type),
                Locations = new List<LocationConfig>(),
                Routes = new Dictionary<string, List<RoutePoint>>()
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
                "large" => new[] { "Town Hall", "Market", "Inn", "Temple", "Blacksmith" },
                "medium" => new[] { "Inn", "Market", "Chapel", "Blacksmith" },
                _ => new[] { "Inn", "Trading Post" }
            };

            // Add essential locations
            foreach (string? locType in essentialLocations)
            {
                region.Locations.Add(GenerateLocation(locType));
            }

            // Add random additional locations
            string[] additionalTypes = new[]
            {
            "House", "Farm", "Workshop", "Store", "Stable",
            "Garden", "Well", "Warehouse", "Watch Post", "Mill"
        };

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
            Dictionary<string, string[]> prefixes = new Dictionary<string, string[]>
            {
                ["Inn"] = new[] { "Traveler's", "Weary", "Golden", "Silver", "Old" },
                ["Market"] = new[] { "Town", "Trade", "Market", "Merchant's", "Commons" },
                ["Temple"] = new[] { "Sacred", "Holy", "Divine", "Blessed", "Ancient" },
                ["Blacksmith"] = new[] { "Iron", "Forge", "Anvil", "Steel", "Smith's" },
                ["Trading Post"] = new[] { "Frontier", "Trader's", "Merchant's", "Caravan", "Waypoint" }
            };

            Dictionary<string, string[]> suffixes = new Dictionary<string, string[]>
            {
                ["Inn"] = new[] { "Rest", "Lodge", "Inn", "Haven", "House" },
                ["Market"] = new[] { "Square", "Plaza", "Market", "Exchange", "Grounds" },
                ["Temple"] = new[] { "Temple", "Sanctuary", "Chapel", "Shrine", "Cathedral" },
                ["Blacksmith"] = new[] { "Forge", "Workshop", "Smith", "Works", "Anvil" },
                ["Trading Post"] = new[] { "Post", "House", "Store", "Exchange", "Shop" }
            };

            if (prefixes.TryGetValue(type, out string[]? typePrefix) && suffixes.TryGetValue(type, out string[]? typeSuffix))
            {
                return $"{typePrefix[_random.Next(typePrefix.Length)]} {typeSuffix[_random.Next(typeSuffix.Length)]}";
            }

            return $"{type}";
        }

        private List<RoutePoint> GenerateRoutePath()
        {
            List<RoutePoint> points = new List<RoutePoint>();
            int pathLength = _random.Next(2, 5);

            for (int i = 0; i < pathLength; i++)
            {
                RoutePoint point = new RoutePoint
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
            string[] descriptions = new[]
            {
            "The path winds through dense vegetation.",
            "A well-worn trail stretches ahead.",
            "The route follows an ancient stone road.",
            "A narrow path hugs the hillside.",
            "The track crosses a shallow stream.",
            "A bridge spans a deep ravine here."
        };
            return descriptions[_random.Next(descriptions.Length)];
        }

        private string GenerateRouteDirections()
        {
            string[] directions = new[]
            {
            "Follow the path north past the large boulder.",
            "Continue east along the stream.",
            "Head uphill toward the cliff face.",
            "Take the fork in the road heading west.",
            "Cross the wooden bridge and continue straight.",
            "Follow the markers through the valley."
        };
            return directions[_random.Next(directions.Length)];
        }

        private List<LocationConfig> GenerateRouteLandmarks()
        {
            List<LocationConfig> landmarks = new List<LocationConfig>();
            if (_random.Next(100) < 30) // 30% chance for a landmark
            {
                string[] types = new[]
                {
                "Ancient Ruins", "Watch Tower", "Abandoned Fort",
                "Old Shrine", "Cave Entrance", "Stone Circle",
                "Abandoned Mill", "Bridge", "Wayshrine",
                "Campsite", "Trading Post", "Mystery"
            };

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
                List<(int index, double dist)> distances = new List<(int index, double dist)>();
                for (int j = 0; j < regions.Count; j++)
                {
                    if (i == j) continue;
                    distances.Add((j, Distance(positions[i], positions[j])));
                }

                // Sort by distance and take 2-4 nearest
                int connectionCount = _random.Next(2, 5);
                List<string> connections = distances
                    .OrderBy(d => d.dist)
                    .Take(connectionCount)
                    .Select(d => regions[d.index].Name)
                    .ToList();

                regions[i].Connections = connections;
            }
        }

        private string GenerateWorldName()
        {
            string[] prefixes = new[] { "Northern", "Eastern", "Western", "Southern", "Lost", "Ancient", "Wild" };
            string[] suffixes = new[] { "Frontier", "Reaches", "Lands", "Territory", "Province", "Region", "Domain" };
            return $"The {prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
        }

        private string GenerateRegionName(string type)
        {
            Dictionary<string, string[]> prefixes = new Dictionary<string, string[]>
            {
                ["Mountain"] = new[] { "High", "Steep", "Rocky", "Stone", "Frost" },
                ["Hills"] = new[] { "Rolling", "Green", "Windy", "Low", "Grassy" },
                ["Lake"] = new[] { "Deep", "Clear", "Still", "Mirror", "Dark" },
                ["Swamp"] = new[] { "Murky", "Misty", "Fog", "Reed", "Marsh" },
                ["Forest"] = new[] { "Dense", "Old", "Wild", "Deep", "Shadow" },
                ["Plains"] = new[] { "Open", "Vast", "Wide", "Windy", "Golden" },
                ["Valley"] = new[] { "Hidden", "Quiet", "Green", "Peaceful", "Sheltered" }
            };

            Dictionary<string, string[]> suffixes = new Dictionary<string, string[]>
            {
                ["Mountain"] = new[] { "Peak", "Ridge", "Summit", "Heights", "Pass" },
                ["Hills"] = new[] { "Hills", "Slopes", "Rise", "Downs", "Highlands" },
                ["Lake"] = new[] { "Lake", "Waters", "Pool", "Basin", "Mere" },
                ["Swamp"] = new[] { "Marsh", "Bog", "Fen", "Swamp", "Mire" },
                ["Forest"] = new[] { "Woods", "Forest", "Grove", "Thicket", "Woodland" },
                ["Plains"] = new[] { "Plains", "Fields", "Grassland", "Meadow", "Prairie" },
                ["Valley"] = new[] { "Valley", "Vale", "Dale", "Glen", "Bottom" }
            };

            string prefix = prefixes[type][_random.Next(prefixes[type].Length)];
            string suffix = suffixes[type][_random.Next(suffixes[type].Length)];
            return $"{prefix} {suffix}";
        }

        private string GenerateRegionDescription(string type)
        {
            Dictionary<string, string[]> descriptions = new Dictionary<string, string[]>
            {
                ["Mountain"] = new[] {
                "Steep cliffs rise sharply against the sky, their peaks shrouded in clouds.",
                "A rugged landscape of stone and snow stretches upward into thin air.",
                "Rocky paths wind between towering peaks of weathered stone."
            },
                ["Hills"] = new[] {
                "Gentle slopes roll endlessly toward the horizon, covered in wild grass.",
                "Wind-swept hills dotted with hardy shrubs and exposed rocks.",
                "A series of low rises and dips create a peaceful, pastoral landscape."
            },
                ["Lake"] = new[] {
                "Clear waters reflect the sky like a mirror, surrounded by reeds.",
                "A calm body of water stretches into the distance, its surface occasionally broken by fish.",
                "The lake's deep waters lap gently at its rocky shores."
            },
                ["Swamp"] = new[] {
                "Murky water pools between twisted trees draped with moss.",
                "A maze of waterways winds through dense vegetation and muddy ground.",
                "Mist clings to the surface of dark water, obscuring what lies beneath."
            },
                ["Forest"] = new[] {
                "Ancient trees stand like silent sentinels, their canopy blocking most light.",
                "A dense woodland of old growth trees and tangled underbrush.",
                "Filtered sunlight creates patterns on the forest floor."
            },
                ["Plains"] = new[] {
                "Tall grass waves in the wind like a golden sea under open skies.",
                "A vast expanse of open ground stretches to the horizon.",
                "Wild grasses and scattered wildflowers cover the level ground."
            },
                ["Valley"] = new[] {
                "Sheltered by high ground on either side, the valley offers protection from harsh winds.",
                "A peaceful lowland nestled between higher terrain.",
                "Rich soil and protected position make this an ideal settlement location."
            }
            };

            return descriptions[type][_random.Next(descriptions[type].Length)];
        }

        private List<string> GenerateNPCsForType(string type)
        {
            List<string> npcs = new List<string>();
            int count = _random.Next(2, 5);

            Dictionary<string, string[]> npcTypes = new Dictionary<string, string[]>
            {
                ["Mountain"] = new[] { "Miner", "Guide", "Climber", "Scout", "Hunter" },
                ["Hills"] = new[] { "Shepherd", "Farmer", "Hunter", "Guard", "Traveler" },
                ["Lake"] = new[] { "Fisher", "Boatman", "Merchant", "Guard", "Dock Worker" },
                ["Swamp"] = new[] { "Hunter", "Guide", "Herbalist", "Fisher", "Recluse" },
                ["Forest"] = new[] { "Woodcutter", "Hunter", "Ranger", "Trapper", "Scout" },
                ["Plains"] = new[] { "Farmer", "Herder", "Merchant", "Guard", "Scout" },
                ["Valley"] = new[] { "Farmer", "Miller", "Trader", "Guard", "Worker" }
            };

            for (int i = 0; i < count; i++)
            {
                npcs.Add(npcTypes[type][_random.Next(npcTypes[type].Length)]);
            }

            return npcs.Distinct().ToList();
        }

        private List<string> GenerateItemsForType(string type)
        {
            List<string> items = new List<string>();
            int count = _random.Next(3, 6);

            Dictionary<string, string[]> itemTypes = new Dictionary<string, string[]>
            {
                ["Mountain"] = new[] { "Pickaxe", "Rope", "Lantern", "Iron Ore", "Climbing Gear" },
                ["Hills"] = new[] { "Walking Staff", "Water Skin", "Dried Food", "Wool", "Tools" },
                ["Lake"] = new[] { "Fishing Rod", "Net", "Fresh Fish", "Boat Hook", "Rope" },
                ["Swamp"] = new[] { "Boots", "Herbs", "Walking Staff", "Medicine", "Torch" },
                ["Forest"] = new[] { "Axe", "Bow", "Herbs", "Leather", "Wood" },
                ["Plains"] = new[] { "Farming Tools", "Seeds", "Water Skin", "Cart", "Food" },
                ["Valley"] = new[] { "Tools", "Grain", "Cart", "Trade Goods", "Food" }
            };

            for (int i = 0; i < count; i++)
            {
                items.Add(itemTypes[type][_random.Next(itemTypes[type].Length)]);
            }

            return items.Distinct().ToList();
        }

        private double Distance((int x, int y) a, (int x, int y) b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private string GenerateLocationDescription(string type)
        {
            Dictionary<string, string[]> descriptions = new Dictionary<string, string[]>
            {
                ["Inn"] = new[] {
                "A cozy establishment with a warm hearth and the smell of fresh bread.",
                "The sound of lively chatter fills this well-maintained inn.",
                "A popular rest stop for weary travelers, known for its comfortable beds."
            },
                ["Market"] = new[] {
                "Colorful stalls line the busy marketplace, filled with goods from all over.",
                "Merchants call out their wares as customers haggle over prices.",
                "A bustling center of trade where locals gather to buy and sell."
            },
                ["Temple"] = new[] {
                "Peaceful silence fills this sacred space, broken only by quiet prayer.",
                "Sunlight streams through colored windows onto stone floors.",
                "An ancient place of worship, maintained with careful dedication."
            },
                ["Blacksmith"] = new[] {
                "The ring of hammer on anvil echoes from the busy forge.",
                "Heat radiates from the glowing forge as tools take shape.",
                "A well-equipped workshop where quality weapons and tools are made."
            },
                ["Trading Post"] = new[] {
                "A sturdy building where traders exchange goods and news.",
                "Shelves lined with goods from distant lands fill this trading post.",
                "A busy stop along the trade route, always full of travelers."
            },
                ["House"] = new[] {
                "A modest dwelling with a small garden.",
                "Smoke rises from the chimney of this comfortable home.",
                "A well-maintained house with flowers in the windows."
            },
                ["Farm"] = new[] {
                "Fields of crops stretch out behind the farmhouse.",
                "A working farm with various animals and growing fields.",
                "The smell of fresh hay wafts from the barn."
            },
                ["Workshop"] = new[] {
                "Tools and materials fill this busy craftsman's space.",
                "The sound of work echoes from within.",
                "A place where skilled artisans practice their trade."
            },
                ["Ancient Ruins"] = new[] {
                "Crumbling stone walls hint at past grandeur.",
                "Mystery surrounds these weathered ruins.",
                "Time-worn stones covered in creeping vines."
            },
                ["Watch Tower"] = new[] {
                "A tall structure providing views of the surrounding area.",
                "Guards keep vigilant watch from this strategic point.",
                "A defensive position overlooking the landscape."
            }
            };

            if (descriptions.TryGetValue(type, out string[]? typeDescriptions))
            {
                return typeDescriptions[_random.Next(typeDescriptions.Length)];
            }

            return $"A typical {type.ToLower()}.";
        }

        private List<string> GenerateNPCsForLocation(string type)
        {
            List<string> npcs = new List<string>();
            int count = _random.Next(1, 4);

            Dictionary<string, string[]> npcTypes = new Dictionary<string, string[]>
            {
                ["Inn"] = new[] { "Innkeeper", "Barmaid", "Cook", "Patron", "Traveler" },
                ["Market"] = new[] { "Merchant", "Shopper", "Guard", "Vendor", "Pickpocket" },
                ["Temple"] = new[] { "Priest", "Acolyte", "Worshipper", "Pilgrim", "Healer" },
                ["Blacksmith"] = new[] { "Smith", "Apprentice", "Customer", "Supplier" },
                ["Trading Post"] = new[] { "Trader", "Merchant", "Guard", "Porter", "Customer" },
                ["House"] = new[] { "Resident", "Family Member", "Visitor" },
                ["Farm"] = new[] { "Farmer", "Farmhand", "Worker", "Animal Handler" },
                ["Workshop"] = new[] { "Craftsman", "Apprentice", "Customer", "Supplier" },
                ["Watch Tower"] = new[] { "Guard", "Watchman", "Soldier", "Scout" },
                ["Ancient Ruins"] = new[] { "Explorer", "Archaeologist", "Treasure Hunter", "Guard" }
            };

            if (npcTypes.TryGetValue(type, out string[]? typeNPCs))
            {
                for (int i = 0; i < count; i++)
                {
                    npcs.Add(typeNPCs[_random.Next(typeNPCs.Length)]);
                }
            }

            return npcs.Distinct().ToList();
        }

        private List<string> GenerateItemsForLocation(string type)
        {
            List<string> items = new List<string>();
            int count = _random.Next(2, 5);

            Dictionary<string, string[]> itemTypes = new Dictionary<string, string[]>
            {
                ["Inn"] = new[] { "Ale", "Bread", "Stew", "Bedroll", "Candle", "Key" },
                ["Market"] = new[] { "Food", "Cloth", "Pottery", "Tools", "Jewelry", "Spices" },
                ["Temple"] = new[] { "Candle", "Holy Symbol", "Incense", "Offering", "Scripture" },
                ["Blacksmith"] = new[] { "Sword", "Armor", "Tools", "Metal", "Coal", "Hammer" },
                ["Trading Post"] = new[] { "Map", "Supplies", "Trade Goods", "Food", "Equipment" },
                ["House"] = new[] { "Furniture", "Dishes", "Tools", "Personal Items" },
                ["Farm"] = new[] { "Tools", "Seeds", "Grain", "Feed", "Produce" },
                ["Workshop"] = new[] { "Tools", "Materials", "Products", "Work Table" },
                ["Watch Tower"] = new[] { "Weapons", "Armor", "Supplies", "Signal Horn" },
                ["Ancient Ruins"] = new[] { "Artifact", "Relic", "Old Coin", "Broken Pottery" }
            };

            if (itemTypes.TryGetValue(type, out string[]? typeItems))
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(typeItems[_random.Next(typeItems.Length)]);
                }
            }

            return items.Distinct().ToList();
        }
    }

    public class WorldConfig
    {
        public string Name { get; set; } = "Demo World";
        public string Description { get; set; } = "A sample RPG world";
        public List<RegionConfig> Regions { get; set; } = new();
        public List<string> NPCs { get; set; } = new();
        public List<string> Items { get; set; } = new();
    }

    public class RegionConfig
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Connections { get; set; } = new();
        public List<string> NPCs { get; set; } = new();
        public List<string> Items { get; set; } = new();
        public List<LocationConfig> Locations { get; set; } = new();
        public Dictionary<string, List<RoutePoint>> Routes { get; set; } = new();
    }

    public class LocationConfig
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> NPCs { get; set; } = new();
        public List<string> Items { get; set; } = new();
    }

    public class RoutePoint
    {
        public int DescriptionId { get; set; }
        public int DirectionsId { get; set; }
        public string Description { get; set; } = "";
        public string Directions { get; set; } = "";
        public List<LocationConfig> Landmarks { get; set; } = new();
    }
    public class Header
    {
        public string Magic { get; set; } = "RPGW"; // Magic number
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "1.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RegionCount { get; set; }
        public int NPCCount { get; set; }
        public int ItemCount { get; set; }
    }

    public class ResourceTable
    {
        public Dictionary<string, int> StringPool { get; set; } = new();
        public Dictionary<string, int> TextureRefs { get; set; } = new();
        public Dictionary<string, int> SoundRefs { get; set; } = new();
        public List<string> SharedDialogue { get; set; } = new();
    }

    public class GenWorldRegion
    {
        public int NameId { get; set; }          // Reference to string pool
        public int DescriptionId { get; set; }   // Reference to string pool
        public List<int> Connections { get; set; } = new();  // GenWorldRegion indices
        public List<int> NPCs { get; set; } = new();        // NPC indices
        public List<int> Items { get; set; } = new();       // Item indices
        public Vector2 Position { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public Dictionary<int, List<RoutePoint>> Routes { get; set; } = new();
    }

    public class Location
    {
        public int NameId { get; set; }
        public int TypeId { get; set; }
        public int DescriptionId { get; set; }
        public List<int> NPCs { get; set; } = new();
        public List<int> Items { get; set; } = new();
    }

    public class Entity
    {
        public int NameId { get; set; }          // Reference to string pool
        public int Level { get; set; }
        public int HP { get; set; }
        public List<int> DialogueRefs { get; set; } = new(); // References to shared dialogue
        public EntityStats Stats { get; set; } = new();
    }

    public class Item
    {
        public int NameId { get; set; }          // Reference to string pool
        public int DescriptionId { get; set; }   // Reference to string pool
        public ItemStats Stats { get; set; } = new();
    }

    public class Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class EntityStats
    {
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int Defense { get; set; }
    }

    public class ItemStats
    {
        public int Value { get; set; }
        public int Weight { get; set; }
        public int Durability { get; set; }
        public ItemType Type { get; set; }
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Quest,
        Misc
    }
}