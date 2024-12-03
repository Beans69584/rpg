#nullable enable
#r "System.Text.Json"
#load "build_world.csx"
using System.Text.Json;
using System.IO.Compression;

public class WorldInspector
{
    private readonly WorldData _data;
    private readonly Dictionary<int, string> _reverseStringPool;

    public WorldInspector(string worldPath)
    {
        // Read and decompress the world file
        using var fs = File.OpenRead(worldPath);
        using var gzip = new GZipStream(fs, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        gzip.CopyTo(ms);
        ms.Position = 0;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _data = JsonSerializer.Deserialize<WorldData>(ms.ToArray(), options)!;
        
        // Create reverse lookup for string pool
        _reverseStringPool = _data.Resources.StringPool
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    public void PrintWorldInfo()
    {
        Console.Clear();
        while (true)
        {
            Console.WriteLine("\n=== World Inspector ===");
            Console.WriteLine("1. World Overview");
            Console.WriteLine("2. View Map");
            Console.WriteLine("3. List All Regions");
            Console.WriteLine("4. Region Details");
            Console.WriteLine("5. NPC List");
            Console.WriteLine("6. Item List");
            Console.WriteLine("7. Location Types");
            Console.WriteLine("8. Route Information");
            Console.WriteLine("9. Statistics");
            Console.WriteLine("0. Exit");
            
            Console.Write("\nSelect an option: ");
            var input = Console.ReadLine();
            Console.Clear();

            switch (input)
            {
                case "1": PrintWorldOverview(); break;
                case "2": PrintWorldMap(); break;
                case "3": PrintRegionList(); break;
                case "4": PrintRegionDetails(); break;
                case "5": PrintNPCList(); break;
                case "6": PrintItemList(); break;
                case "7": PrintLocationTypes(); break;
                case "8": PrintRouteInformation(); break;
                case "9": PrintStatistics(); break;
                case "0": return;
                default: Console.WriteLine("Invalid option"); break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
            Console.Clear();
        }
    }

    private void PrintWorldOverview()
    {
        Console.WriteLine($"\n=== {_data.Header.Name} ===");
        Console.WriteLine(_data.Header.Description);
        Console.WriteLine($"Created: {_data.Header.CreatedAt}");
        Console.WriteLine($"Version: {_data.Header.Version}");
        
        PrintRegionDistribution();
    }

    private void PrintRegionList()
    {
        Console.WriteLine("\n=== Regions ===");
        foreach (var region in _data.Regions)
        {
            Console.WriteLine($"\n{GetString(region.NameId)}");
            Console.WriteLine($"Type: {GetRegionType(GetString(region.NameId))}");
            Console.WriteLine($"Locations: {region.Locations.Count}");
            Console.WriteLine($"NPCs: {region.NPCs.Count}");
            Console.WriteLine($"Connected to: {string.Join(", ", region.Connections.Select(c => GetString(_data.Regions[c].NameId)))}");
        }
    }

    private void PrintRegionDetails()
    {
        Console.WriteLine("\n=== Region Details ===");
        Console.WriteLine("Enter region name or number:");
        
        // List available regions
        for (int i = 0; i < _data.Regions.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {GetString(_data.Regions[i].NameId)}");
        }

        var input = Console.ReadLine();
        Region? region = null;

        if (int.TryParse(input, out int num) && num > 0 && num <= _data.Regions.Count)
        {
            region = _data.Regions[num - 1];
        }
        else
        {
            region = _data.Regions.FirstOrDefault(r => 
                GetString(r.NameId).Equals(input, StringComparison.OrdinalIgnoreCase));
        }

        if (region == null)
        {
            Console.WriteLine("Region not found");
            return;
        }

        PrintDetailedRegionInfo(region);
    }

    private void PrintDetailedRegionInfo(Region region)
    {
        Console.WriteLine($"\n=== {GetString(region.NameId)} ===");
        Console.WriteLine(GetString(region.DescriptionId));
        Console.WriteLine($"Position: ({region.Position.X}, {region.Position.Y})");

        if (region.Locations.Any())
        {
            Console.WriteLine("\nLocations:");
            foreach (var loc in region.Locations)
            {
                Console.WriteLine($"\n  {GetString(loc.NameId)} ({GetString(loc.TypeId)})");
                Console.WriteLine($"  {GetString(loc.DescriptionId)}");
                
                if (loc.NPCs.Any())
                    Console.WriteLine($"  NPCs: {string.Join(", ", loc.NPCs.Select(n => GetString(_data.NPCs[n].NameId)))}");
                
                if (loc.Items.Any())
                    Console.WriteLine($"  Items: {string.Join(", ", loc.Items.Select(i => GetString(_data.Items[i].NameId)))}");
            }
        }

        PrintRouteInfoForRegion(region);
    }

    private void PrintRouteInfoForRegion(Region region)
    {
        if (!region.Routes.Any()) return;

        Console.WriteLine("\nRoutes:");
        foreach (var route in region.Routes)
        {
            var targetRegion = _data.Regions[route.Key];
            Console.WriteLine($"\nRoute to {GetString(targetRegion.NameId)}:");
            
            foreach (var point in route.Value)
            {
                Console.WriteLine($"  * {point.Description}");
                Console.WriteLine($"    Directions: {point.Directions}");
                
                if (point.Landmarks?.Any() == true)
                {
                    Console.WriteLine("    Landmarks:");
                    foreach (var landmark in point.Landmarks)
                    {
                        Console.WriteLine($"    - {landmark.Name} ({landmark.Type})");
                        Console.WriteLine($"      {landmark.Description}");
                    }
                }
            }
        }
    }

    private void PrintNPCList()
    {
        Console.WriteLine("\n=== NPCs ===");
        var npcGroups = _data.NPCs
            .GroupBy(npc => _data.Regions.FirstOrDefault(r => r.NPCs.Contains(_data.NPCs.IndexOf(npc))))
            .OrderBy(g => g.Key != null ? GetString(g.Key.NameId) : "");

        foreach (var group in npcGroups)
        {
            Console.WriteLine($"\nIn {(group.Key != null ? GetString(group.Key.NameId) : "Unknown Location")}:");
            foreach (var npc in group)
            {
                Console.WriteLine($"  {GetString(npc.NameId)} (Level {npc.Level})");
                Console.WriteLine($"    HP: {npc.HP}  STR: {npc.Stats.Strength}  DEX: {npc.Stats.Dexterity}  INT: {npc.Stats.Intelligence}  DEF: {npc.Stats.Defense}");
            }
        }
    }

    private void PrintItemList()
    {
        Console.WriteLine("\n=== Items ===");
        var itemGroups = _data.Items
            .GroupBy(item => item.Stats.Type)
            .OrderBy(g => g.Key);

        foreach (var group in itemGroups)
        {
            Console.WriteLine($"\n{group.Key}:");
            foreach (var item in group)
            {
                Console.WriteLine($"  {GetString(item.NameId)}");
                Console.WriteLine($"    {GetString(item.DescriptionId)}");
                Console.WriteLine($"    Value: {item.Stats.Value}  Weight: {item.Stats.Weight}  Durability: {item.Stats.Durability}");
            }
        }
    }

    private void PrintLocationTypes()
    {
        var locationTypes = _data.Regions
            .SelectMany(r => r.Locations)
            .GroupBy(l => GetString(l.TypeId))
            .OrderByDescending(g => g.Count());

        Console.WriteLine("\n=== Location Types ===");
        foreach (var type in locationTypes)
        {
            Console.WriteLine($"\n{type.Key} ({type.Count()} total):");
            foreach (var loc in type.Take(3))
            {
                Console.WriteLine($"  - {GetString(loc.NameId)}");
            }
            if (type.Count() > 3)
                Console.WriteLine("  ...");
        }
    }

    private void PrintRouteInformation()
    {
        Console.WriteLine("\n=== Route Information ===");
        var landmarkTypes = _data.Regions
            .SelectMany(r => r.Routes.Values)
            .SelectMany(r => r.SelectMany(p => p.Landmarks ?? new List<LocationConfig>()))
            .GroupBy(l => l.Type)
            .OrderByDescending(g => g.Count());

        Console.WriteLine("\nLandmark Types Found:");
        foreach (var type in landmarkTypes)
        {
            Console.WriteLine($"\n{type.Key} ({type.Count()} total):");
            foreach (var landmark in type.Take(3))
            {
                Console.WriteLine($"  - {landmark.Name}");
            }
            if (type.Count() > 3)
                Console.WriteLine("  ...");
        }
    }

    private void PrintStatistics()
    {
        Console.WriteLine("\n=== World Statistics ===");
        Console.WriteLine($"Regions: {_data.Regions.Count}");
        Console.WriteLine($"Total NPCs: {_data.NPCs.Count}");
        Console.WriteLine($"Total Items: {_data.Items.Count}");
        Console.WriteLine($"Total Locations: {_data.Regions.Sum(r => r.Locations.Count)}");
        
        var avgConnections = _data.Regions.Average(r => r.Connections.Count);
        Console.WriteLine($"Average Connections per Region: {avgConnections:F1}");
        
        var routeCount = _data.Regions.Sum(r => r.Routes.Count);
        Console.WriteLine($"Total Routes: {routeCount}");
        
        var landmarkCount = _data.Regions
            .SelectMany(r => r.Routes.Values)
            .SelectMany(r => r.SelectMany(p => p.Landmarks ?? new List<LocationConfig>()))
            .Count();
        Console.WriteLine($"Total Landmarks: {landmarkCount}");
    }

    private void PrintRegionMap()
    {
        // Find map boundaries
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (var region in _data.Regions)
        {
            minX = Math.Min(minX, region.Position.X);
            maxX = Math.Max(maxX, region.Position.X);
            minY = Math.Min(minY, region.Position.Y);
            maxY = Math.Max(maxY, region.Position.Y);
        }

        // Map size and scaling
        const int mapWidth = 60;
        const int mapHeight = 30;
        var grid = new char[mapWidth, mapHeight];
        var regionAtPos = new Dictionary<(int x, int y), Region>();

        // Initialize grid with spaces
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                grid[x, y] = ' ';

        // Plot regions
        foreach (var region in _data.Regions)
        {
            int x = (int)((region.Position.X - minX) / (maxX - minX) * (mapWidth - 1));
            int y = (int)((region.Position.Y - minY) / (maxY - minY) * (mapHeight - 1));
            
            x = Math.Clamp(x, 0, mapWidth - 1);
            y = Math.Clamp(y, 0, mapHeight - 1);

            grid[x, y] = GetRegionSymbol(region);
            regionAtPos[(x, y)] = region;

            // Draw connections
            foreach (var connIdx in region.Connections)
            {
                var connRegion = _data.Regions[connIdx];
                int x2 = (int)((connRegion.Position.X - minX) / (maxX - minX) * (mapWidth - 1));
                int y2 = (int)((connRegion.Position.Y - minY) / (maxY - minY) * (mapHeight - 1));
                
                x2 = Math.Clamp(x2, 0, mapWidth - 1);
                y2 = Math.Clamp(y2, 0, mapHeight - 1);

                // Draw simple line
                DrawLine(grid, x, y, x2, y2);
            }
        }

        // Print the map
        Console.WriteLine("Map Legend: # Mountain, ^ Hills, ~ Lake, , Swamp, * Forest, . Plains, v Valley");
        Console.WriteLine(new string('-', mapWidth + 2));
        for (int y = 0; y < mapHeight; y++)
        {
            Console.Write("|");
            for (int x = 0; x < mapWidth; x++)
            {
                Console.Write(grid[x, y]);
                if (regionAtPos.TryGetValue((x, y), out var region))
                {
                    // Add region name hint when hovering over a point
                    if (x == 0 && y % 3 == 0)
                    {
                        Console.Write($" - {GetString(region.NameId)}");
                    }
                }
            }
            Console.WriteLine("|");
        }
        Console.WriteLine(new string('-', mapWidth + 2));
    }

    private void PrintWorldMap()
    {
        // Find map boundaries
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (var region in _data.Regions)
        {
            minX = Math.Min(minX, region.Position.X);
            maxX = Math.Max(maxX, region.Position.X);
            minY = Math.Min(minY, region.Position.Y);
            maxY = Math.Max(maxY, region.Position.Y);
        }

        // Map size and scaling
        const int mapWidth = 80;
        const int mapHeight = 40;
        var grid = new char[mapWidth, mapHeight];
        var regionAtPos = new Dictionary<(int x, int y), Region>();

        // Initialize grid with spaces
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                grid[x, y] = ' ';

        // Plot regions and connections
        foreach (var region in _data.Regions)
        {
            int x = (int)((region.Position.X - minX) / (maxX - minX) * (mapWidth - 1));
            int y = (int)((region.Position.Y - minY) / (maxY - minY) * (mapHeight - 1));
            
            x = Math.Clamp(x, 0, mapWidth - 1);
            y = Math.Clamp(y, 0, mapHeight - 1);

            grid[x, y] = GetRegionSymbol(region);
            regionAtPos[(x, y)] = region;

            // Draw connections
            foreach (var connIdx in region.Connections)
            {
                var connRegion = _data.Regions[connIdx];
                int x2 = (int)((connRegion.Position.X - minX) / (maxX - minX) * (mapWidth - 1));
                int y2 = (int)((connRegion.Position.Y - minY) / (maxY - minY) * (mapHeight - 1));
                
                x2 = Math.Clamp(x2, 0, mapWidth - 1);
                y2 = Math.Clamp(y2, 0, mapHeight - 1);

                DrawLine(grid, x, y, x2, y2);
            }
        }

        // Print map with legend
        Console.WriteLine("\nWorld Map");
        Console.WriteLine("Legend: # Mountain  ^ Hills  ~ Lake  , Swamp  * Forest  . Plains  v Valley  o Other");
        Console.WriteLine(new string('-', mapWidth + 2));

        // Print the map with region names
        for (int y = 0; y < mapHeight; y++)
        {
            Console.Write("|");
            for (int x = 0; x < mapWidth; x++)
            {
                Console.Write(grid[x, y]);
            }
            Console.Write("| ");

            // Show region names on the right side
            if (y < _data.Regions.Count && y % 2 == 0)
            {
                var region = _data.Regions[y / 2];
                Console.Write($"{GetRegionSymbol(region)} {GetString(region.NameId)}");
            }

            Console.WriteLine();
        }
        Console.WriteLine(new string('-', mapWidth + 2));
    }

    private char GetRegionSymbol(Region region)
    {
        var name = GetString(region.NameId).ToLower();
        if (name.Contains("mountain")) return '#';
        if (name.Contains("hill")) return '^';
        if (name.Contains("lake")) return '~';
        if (name.Contains("swamp")) return ',';
        if (name.Contains("forest")) return '*';
        if (name.Contains("plain")) return '.';
        if (name.Contains("valley")) return 'v';
        return 'o';
    }

    private void DrawLine(char[,] grid, int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = (dx > dy ? dx : -dy) / 2;

        while (true)
        {
            if (grid[x1, y1] == ' ') grid[x1, y1] = '·';
            if (x1 == x2 && y1 == y2) break;
            int e2 = err;
            if (e2 > -dx) { err -= dy; x1 += sx; }
            if (e2 < dy) { err += dx; y1 += sy; }
        }
    }

    private void PrintRegionDistribution()
    {
        var regionTypes = _data.Regions
            .GroupBy(r => GetRegionType(GetString(r.NameId)))
            .OrderByDescending(g => g.Count());

        Console.WriteLine("Region type distribution:");
        foreach (var type in regionTypes)
        {
            var percentage = (type.Count() * 100.0) / _data.Regions.Count;
            var locationCount = _data.Regions
                .Where(r => GetRegionType(GetString(r.NameId)) == type.Key)
                .Sum(r => r.Locations.Count);
                
            Console.WriteLine($"  {type.Key,-12}: {type.Count(),2} ({percentage:F1}%) with {locationCount} locations");
        }
    }

    private string GetRegionType(string name)
    {
        if (name.Contains("mountain", StringComparison.OrdinalIgnoreCase)) return "Mountain";
        if (name.Contains("hill", StringComparison.OrdinalIgnoreCase)) return "Hills";
        if (name.Contains("lake", StringComparison.OrdinalIgnoreCase)) return "Lake";
        if (name.Contains("swamp", StringComparison.OrdinalIgnoreCase)) return "Swamp";
        if (name.Contains("forest", StringComparison.OrdinalIgnoreCase)) return "Forest";
        if (name.Contains("plain", StringComparison.OrdinalIgnoreCase)) return "Plains";
        if (name.Contains("valley", StringComparison.OrdinalIgnoreCase)) return "Valley";
        return "Other";
    }

    private string GetString(int id) => 
        _reverseStringPool.TryGetValue(id, out var str) ? str : $"<unknown string {id}>";
}

// Script execution

// Replace the args-based execution code with Args from dotnet-script
var worldPath = Args.Count > 0 ? Args[0] : "./World/world.dat";

if (!File.Exists(worldPath))
{
    Console.WriteLine("Please provide a world file path.");
    Console.WriteLine("Usage: dotnet script inspect_world.csx [path_to_world.dat]");
    Console.WriteLine($"Error: File not found: {worldPath}");
    return;
}

try
{
    var inspector = new WorldInspector(worldPath);
    inspector.PrintWorldInfo();
}
catch (Exception ex)
{
    Console.WriteLine($"Error inspecting world file: {ex.Message}");
}
