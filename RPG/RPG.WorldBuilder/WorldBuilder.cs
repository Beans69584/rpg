using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RPG.Common;
using RPG.World.Data;
using Serilog;

namespace RPG.WorldBuilder
{
    /// <summary>
    /// Responsible for building a <see cref="WorldData"/> object from a <see cref="WorldDefinition"/>.
    /// </summary>
    public class WorldBuilder
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, int> stringPool = [];
        private int nextStringId = 0;
        private readonly string worldDataPath;
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="WorldBuilder"/> class.
        /// </summary>
        /// <param name="worldDataPath">The path to the world data files.</param>
        /// <param name="logger">The logger to use.</param>
        public WorldBuilder(string worldDataPath = "Data", ILogger? logger = null)
        {
            this.worldDataPath = worldDataPath;
            _logger = logger ?? Log.Logger;
            _logger.Information("WorldBuilder Initialised with data path: {Path}", worldDataPath);
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

        /// <summary>
        /// Builds a <see cref="WorldData"/> object from the specified world definition.
        /// </summary>
        /// <param name="worldName">The name of the world to build.</param>
        /// <returns>The built <see cref="WorldData"/> object.</returns>
        public async Task<WorldData> BuildWorldAsync(string worldName)
        {
            try
            {
                _logger.Information("Starting world build for {WorldName}", worldName);

                string worldPath = Path.Combine(worldDataPath, "worlds", $"{worldName}.json");
                WorldDefinition worldDef = await LoadJsonAsync<WorldDefinition>(worldPath)
                    ?? throw new FileNotFoundException($"World definition not found: {worldPath}");

                _logger.Information("Building resource table...");
                ResourceTable resources = await BuildResourceTableAsync(worldDef);

                _logger.Information("Loading NPCs...");
                List<Entity> npcs = await BuildNPCsAsync(worldDef.NPCFiles);

                _logger.Information("Loading items...");
                List<Item> items = await BuildItemsAsync(worldDef.ItemFiles);

                _logger.Information("Generating regions...");
                List<WorldRegion> regions = await BuildRegionsAsync(worldDef.RegionFiles);

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
                return new WorldData();
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
                    List<NpcDefinition>? npcDefs = await LoadJsonAsync<List<NpcDefinition>>(path);
                    if (npcDefs != null)
                    {
                        foreach (NpcDefinition npcDef in npcDefs)
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
                    List<ItemDefinition>? itemDefs = await LoadJsonAsync<List<ItemDefinition>>(path);
                    if (itemDefs != null)
                    {
                        foreach (ItemDefinition itemDef in itemDefs)
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
                    RegionDefinition? regionDef = await LoadJsonAsync<RegionDefinition>(path);
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
            Dictionary<int, DialogueNode> nodes = [];
            foreach (DialogueNodeDefinition nodeDef in def.Nodes)
            {
                nodes[nodeDef.Id] = new DialogueNode
                {
                    TextId = AddString(nodeDef.Text),
                    Responses = [.. nodeDef.Responses.Select(r => new DialogueResponse
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
                    })],
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
                Objectives = [.. def.Objectives.Select(o => new QuestObjective
                {
                    DescriptionId = AddString(o.Description),
                    Type = o.Type,
                    Target = o.Target,
                    Required = o.Required
                })],
                Rewards = [.. def.Rewards.Select(r => new Reward
                {
                    Type = r.Type,
                    Value = r.Value,
                    DescriptionId = AddString(r.Description)
                })]
            };
        }

        private Entity ConvertNPCDefinition(NpcDefinition def)
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
                Locations = [.. def.Locations.Select(l => new Location
                {
                    NameId = AddString(l.Name),
                    DescriptionId = AddString(l.Description),
                    Type = l.Type,
                    Position = l.Position,
                    IsDiscovered = l.IsDiscovered,
                    Buildings = [.. l.Buildings.Select(b => new Building
                    {
                        NameId = AddString(b.Name),
                        DescriptionId = AddString(b.Description),
                        Type = b.Type,
                        NPCs = b.NPCs
                    })]
                })]
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
                using FileStream stream = File.OpenRead(path);
                T? result = await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions);

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
            Dictionary<int, DialogueTree> dialogueTrees = [];
            Dictionary<int, Quest> quests = [];
            Dictionary<string, string> scripts = [];

            // Load dialogues
            if (worldDef.DialogueFiles != null)
            {
                foreach (string dialogueFile in worldDef.DialogueFiles)
                {
                    if (string.IsNullOrEmpty(dialogueFile)) continue;

                    string dialoguePath = Path.Combine(worldDataPath, "dialogues", dialogueFile);
                    List<DialogueDefinition>? dialogues = await LoadJsonAsync<List<DialogueDefinition>>(dialoguePath);
                    if (dialogues != null)
                    {
                        foreach (DialogueDefinition dialogue in dialogues)
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
                    List<QuestDefinition>? questDefs = await LoadJsonAsync<List<QuestDefinition>>(questPath);
                    if (questDefs != null)
                    {
                        foreach (QuestDefinition quest in questDefs)
                        {
                            quests[quest.Id] = ConvertQuestDefinition(quest);
                        }
                    }
                }
            }

            // Load scripts
            string scriptsPath = Path.Combine(worldDataPath, "scripts.json");
            Dictionary<string, string>? loadedScripts = await LoadJsonAsync<Dictionary<string, string>>(scriptsPath);
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
                TextureRefs = [],
                SoundRefs = []
            };
        }
    }
}