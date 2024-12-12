using System.Text.Json;
using System.IO.Compression;
using RPG.World.Data;
using RPG.WorldBuilder;
using Serilog;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = LoggerConfig.CreateLogger();

        try
        {
            Log.Information("RPG World Builder v1.0 starting...");
            
            WorldBuilder builder = new("Data", Log.Logger);
            
            Log.Information("Building world...");
            Log.Information("Loading configuration from Data/worlds/ravenkeep.json");
            
            WorldData world = await builder.BuildWorldAsync("ravenkeep");

            Log.Information("World Build Summary:");
            Log.Information("Name: {Name}", world.Resources.StringPool.FirstOrDefault(x => x.Value == world.Header.NameId).Key);
            Log.Information("Regions: {Count}", world.Regions.Count);
            Log.Information("NPCs: {Count}", world.NPCs.Count);
            Log.Information("Items: {Count}", world.Items.Count);
            
            string outputPath = "output/ravenkeep.rpgw";
            Log.Information("Saving world to {Path}...", outputPath);
            await SaveWorldAsync(world, outputPath);
            
            Log.Information("World build completed successfully!");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Log.Error(ex, "Required file not found");
            return 1;
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "JSON parsing error");
            return 1;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unexpected error occurred");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static async Task SaveWorldAsync(WorldData world, string path)
    {
        try
        {
            JsonSerializerOptions options = new() { WriteIndented = true };

            using MemoryStream ms = new();
            await JsonSerializer.SerializeAsync(ms, world, options);
            ms.Position = 0;

            using FileStream fs = File.Create(path);
            using GZipStream gzip = new(fs, CompressionMode.Compress);
            await ms.CopyToAsync(gzip);
            
            Log.Information("Successfully saved world to {Path}", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save world to {Path}", path);
            throw;
        }
    }
}