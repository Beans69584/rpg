using System.Text.Json;
using RPG.Common;
using RPG.World.Data;
using Serilog;

namespace RPG.WorldBuilder;

public class WorldBuilder
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, int> stringPool = new();
    private int nextStringId = 0;
    private readonly string worldDataPath;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public WorldBuilder(string worldDataPath = "Data", ILogger? logger = null)
    {
        this.worldDataPath = worldDataPath;
        _logger = logger ?? Log.Logger;
        _logger.Information("WorldBuilder initialized with data path: {Path}", worldDataPath);
    }

    private int AddString(string str)
    {
        if (!stringPool.TryGetValue(str, out int id))
        {
            id = nextStringId++;
            stringPool[str] = id;
        }
        return id;
    }

    public async Task<WorldData> BuildWorldAsync(string worldName)
    {
        try
        {
            _logger.Information("Starting world build for {WorldName}", worldName);
            
            string worldPath = Path.Combine(worldDataPath, "worlds", $"{worldName}.json");
            var worldDef = await LoadJsonAsync<WorldDefinition>(worldPath)
                ?? throw new FileNotFoundException($"World definition not found: {worldPath}");

            _logger.Information("Building resource table...");
            var resources = await BuildResourceTableAsync(worldDef);

            _logger.Information("Loading NPCs...");
            var npcs = await BuildNPCsAsync(worldDef.NPCFiles);

            _logger.Information("Loading items...");
            var items = await BuildItemsAsync(worldDef.ItemFiles);

            _logger.Information("Generating regions...");
            var regions = await BuildRegionsAsync(worldDef.RegionFiles);

            WorldData world = new()
            {
                Header = new Header
                {
                    Magic = "RPGW",
                    NameId = AddString(worldDef.Name),
                    DescriptionId = AddString(worldDef.Description),
                    Version = "1.0",
                    CreatedAt = DateTime.UtcNow,
                    Seed = worldDef.Seed,
                    RegionCount = regions.Count,
                    NPCCount = npcs.Count,
                    ItemCount = items.Count,
                },
                Resources = resources,
                NPCs = npcs,
                Items = items,
                Regions = regions
            };

            _logger.Information("World build completed successfully for {WorldName}", worldName);
            return world;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to build world {WorldName}", worldName);
            throw;
        }
    }

    private async Task<List<Entity>> BuildNPCsAsync(List<string> npcFiles)
    {
        List<Entity> npcs = [];

        if (npcFiles != null)
        {
            foreach (string file in npcFiles)
            {
                if (string.IsNullOrEmpty(file)) continue;

                string path = Path.Combine(worldDataPath, "npcs", file);
                var npcDefs = await LoadJsonAsync<List<NPCDefinition>>(path);
                if (npcDefs != null)
                {
                    foreach (var npcDef in npcDefs)
                    {
                        npcs.Add(ConvertNPCDefinition(npcDef));
                    }
                }
            }
        }

        return npcs;
    }

    private async Task<List<Item>> BuildItemsAsync(List<string> itemFiles)
    {
        List<Item> items = [];

        if (itemFiles != null)
        {
            foreach (string file in itemFiles)
            {
                if (string.IsNullOrEmpty(file)) continue;

                string path = Path.Combine(worldDataPath, "items", file);
                var itemDefs = await LoadJsonAsync<List<ItemDefinition>>(path);
                if (itemDefs != null)
                {
                    foreach (var itemDef in itemDefs)
                    {
                        items.Add(ConvertItemDefinition(itemDef));
                    }
                }
            }
        }

        return items;
    }

    private async Task<List<WorldRegion>> BuildRegionsAsync(List<string> regionFiles)
    {
        List<WorldRegion> regions = [];

        if (regionFiles != null)
        {
            foreach (string file in regionFiles)
            {
                if (string.IsNullOrEmpty(file)) continue;

                string path = Path.Combine(worldDataPath, "regions", file);
                var regionDef = await LoadJsonAsync<RegionDefinition>(path);
                if (regionDef != null)
                {
                    regions.Add(ConvertRegionDefinition(regionDef));
                }
            }
        }

        return regions;
    }

    private DialogueTree ConvertDialogueDefinition(DialogueDefinition def)
    {
        Dictionary<int, DialogueNode> nodes = new();
        foreach (var nodeDef in def.Nodes)
        {
            nodes[nodeDef.Id] = new DialogueNode
            {
                TextId = AddString(nodeDef.Text),
                Responses = nodeDef.Responses.Select(r => new DialogueResponse
                {
                    TextId = AddString(r.Text),
                    NextNodeId = r.NextNodeId,
                    RequiredFlags = r.RequiredFlags ?? [],
                    Actions = r.Actions?.Select(a => new DialogueAction
                    {
                        Type = a.Type,
                        Target = a.Target,
                        Value = a.Value
                    }).ToList() ?? []
                }).ToList(),
                Actions = nodeDef.Actions?.Select(a => new DialogueAction
                {
                    Type = a.Type,
                    Target = a.Target,
                    Value = a.Value
                }).ToList() ?? []
            };
        }

        return new DialogueTree
        {
            RootNodeId = def.RootNodeId,
            Nodes = nodes
        };
    }

    private Quest ConvertQuestDefinition(QuestDefinition def)
    {
        return new Quest
        {
            NameId = AddString(def.Name),
            DescriptionId = AddString(def.Description),
            Type = def.Type,
            Status = QuestStatus.Available,
            MinLevel = def.MinLevel,
            Objectives = def.Objectives.Select(o => new QuestObjective
            {
                DescriptionId = AddString(o.Description),
                Type = o.Type,
                Target = o.Target,
                Required = o.Required
            }).ToList(),
            Rewards = def.Rewards.Select(r => new Reward
            {
                Type = r.Type,
                Value = r.Value,
                DescriptionId = AddString(r.Description)
            }).ToList()
        };
    }

    private Entity ConvertNPCDefinition(NPCDefinition def)
    {
        return new Entity
        {
            NameId = AddString(def.Name),
            Level = def.Level,
            Role = def.Role,
            DialogueTreeRefs = def.DialogueRefs,
            Stats = def.Stats
        };
    }

    private Item ConvertItemDefinition(ItemDefinition def)
    {
        return new Item
        {
            NameId = AddString(def.Name),
            DescriptionId = AddString(def.Description),
            Stats = def.Stats
        };
    }

    private WorldRegion ConvertRegionDefinition(RegionDefinition def)
    {
        return new WorldRegion
        {
            NameId = AddString(def.Name),
            DescriptionId = AddString(def.Description),
            Position = def.Position,
            Terrain = def.Terrain,
            Locations = def.Locations.Select(l => new Location
            {
                NameId = AddString(l.Name),
                DescriptionId = AddString(l.Description),
                Type = l.Type,
                Position = l.Position,
                IsDiscovered = l.IsDiscovered,
                Buildings = l.Buildings.Select(b => new Building
                {
                    NameId = AddString(b.Name),
                    DescriptionId = AddString(b.Description),
                    Type = b.Type,
                    NPCs = b.NPCs
                }).ToList()
            }).ToList()
        };
    }

    private async Task<T?> LoadJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            _logger.Warning("File not found: {Path}", path);
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions);

            if (result == null)
            {
                _logger.Warning("Failed to deserialize: {Path}", path);
                return null;
            }

            _logger.Debug("Successfully loaded {Path}", path);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load {Path}", path);
            return null;
        }
    }

    private async Task<ResourceTable> BuildResourceTableAsync(WorldDefinition worldDef)
    {
        var dialogueTrees = new Dictionary<int, DialogueTree>();
        var quests = new Dictionary<int, Quest>();
        var scripts = new Dictionary<string, string>();

        // Load dialogues
        if (worldDef.DialogueFiles != null)
        {
            foreach (string dialogueFile in worldDef.DialogueFiles)
            {
                if (string.IsNullOrEmpty(dialogueFile)) continue;

                string dialoguePath = Path.Combine(worldDataPath, "dialogues", dialogueFile);
                var dialogues = await LoadJsonAsync<List<DialogueDefinition>>(dialoguePath);
                if (dialogues != null)
                {
                    foreach (var dialogue in dialogues)
                    {
                        dialogueTrees[dialogue.Id] = ConvertDialogueDefinition(dialogue);
                    }
                }
            }
        }

        // Load quests
        if (worldDef.QuestFiles != null)
        {
            foreach (string questFile in worldDef.QuestFiles)
            {
                if (string.IsNullOrEmpty(questFile)) continue;

                string questPath = Path.Combine(worldDataPath, "quests", questFile);
                var questDefs = await LoadJsonAsync<List<QuestDefinition>>(questPath);
                if (questDefs != null)
                {
                    foreach (var quest in questDefs)
                    {
                        quests[quest.Id] = ConvertQuestDefinition(quest);
                    }
                }
            }
        }

        // Load scripts
        string scriptsPath = Path.Combine(worldDataPath, "scripts.json");
        var loadedScripts = await LoadJsonAsync<Dictionary<string, string>>(scriptsPath);
        if (loadedScripts != null)
        {
            scripts = loadedScripts;
        }

        return new ResourceTable
        {
            DialogueTrees = dialogueTrees,
            Quests = quests,
            Scripts = scripts,
            StringPool = stringPool,
            TextureRefs = new Dictionary<string, int>(),
            SoundRefs = new Dictionary<string, int>()
        };
    }
}