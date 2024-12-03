namespace RPG
{
    public class GameState
    {
        public List<string> GameLog { get; } = new();
        public ConsoleWindowManager WindowManager { get; }
        public string PlayerName { get; set; } = "Hero";
        public int Level { get; set; } = 1;
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public int Gold { get; set; } = 100;
        public Dictionary<string, int> Stats { get; } = new();
        public List<string> Inventory { get; } = new();
        public Dictionary<string, bool> GameFlags { get; } = new();
        public CommandHandler CommandHandler { get; }
        public bool Running { get; set; } = true;
        public LocalizationManager Localization { get; }
        public WorldLoader? World { get; private set; }
        public WorldRegion? CurrentRegion { get; set; }
        public string CurrentWorldPath { get; private set; } = "";
        public Location? CurrentLocation { get; set; }

        public GameState(ConsoleWindowManager manager)
        {
            WindowManager = manager;
            Localization = new LocalizationManager();
            Localization.SetLanguage(GameSettings.CurrentLanguage);
            CommandHandler = new CommandHandler();
            GameLog.Add(Localization.GetString("Welcome_Message"));
            GameLog.Add(Localization.GetString("Help_Hint"));
        }

        public void SaveGame(string slot)
        {
            var saveData = new SaveData
            {
                PlayerName = PlayerName,
                Level = Level,
                HP = HP,
                MaxHP = MaxHP,
                CurrentRegionId = World?.GetString(CurrentRegion?.NameId ?? 0) ?? "",
                SaveTime = DateTime.Now,
                WorldPath = CurrentWorldPath,
                Gold = Gold,
                Stats = new Dictionary<string, int>(Stats),
                Inventory = new List<string>(Inventory),
                GameFlags = new Dictionary<string, bool>(GameFlags)
            };

            SaveManager.Save(saveData, slot);
            GameLog.Add($"Game saved to slot {slot}");
        }

        public bool LoadGame(string slot)
        {
            var saveData = SaveManager.Load(slot);
            if (saveData == null) return false;

            // Load world first without setting starting region
            if (!string.IsNullOrEmpty(saveData.WorldPath))
            {
                try
                {
                    LoadWorld(saveData.WorldPath, false);
                }
                catch (Exception ex)
                {
                    GameLog.Add($"Failed to load world: {ex.Message}");
                    return false;
                }
            }

            // Restore player state
            PlayerName = saveData.PlayerName;
            Level = saveData.Level;
            HP = saveData.HP;
            MaxHP = saveData.MaxHP;
            Gold = saveData.Gold;
            Stats.Clear();
            foreach (var stat in saveData.Stats)
                Stats[stat.Key] = stat.Value;
            Inventory.Clear();
            Inventory.AddRange(saveData.Inventory);
            GameFlags.Clear();
            foreach (var flag in saveData.GameFlags)
                GameFlags[flag.Key] = flag.Value;

            // Restore location with fallback to default
            if (World != null)
            {
                if (!string.IsNullOrEmpty(saveData.CurrentRegionId))
                {
                    CurrentRegion = World.GetRegionByName(saveData.CurrentRegionId);
                }

                // If region is null or invalid, move to default location
                if (CurrentRegion == null)
                {
                    CurrentRegion = FindDefaultLocation();
                    if (CurrentRegion != null)
                    {
                        GameLog.Add("Your previous location was not found. Moving to a safe location...");
                    }
                }

                if (CurrentRegion != null)
                {
                    GameLog.Add($"You are in: {World.GetString(CurrentRegion.NameId)}");
                    DescribeCurrentRegion();
                }
                else
                {
                    GameLog.Add("ERROR: Could not find any valid location in the world!");
                }
            }

            GameLog.Add($"Game loaded from slot {slot}");
            return true;
        }

        private WorldRegion? FindDefaultLocation()
        {
            if (World == null) return null;

            // Try to find locations in this priority order:
            return World.GetRegionByName("Riverdale Village")  // Try main village first
                ?? World.GetRegionByName("Village")           // Try generic village
                ?? World.GetRegionByName("Town")              // Try town
                ?? World.GetStartingRegion()                  // Try configured starting region
                ?? World.GetWorldData().Regions.FirstOrDefault(); // Take first region as last resort
        }

        public void LoadWorld(string worldPath, bool isNewGame = false)
        {
            World = new WorldLoader(worldPath);
            CurrentWorldPath = worldPath;

            GameLog.Add($"Loaded world: {World.GetWorldData().Header.Name}");

            if (isNewGame)
            {
                // Only set starting region for new games
                CurrentRegion = World.GetStartingRegion();
                GameLog.Add($"You are in: {World.GetString(CurrentRegion?.NameId ?? 0)}");
                if (CurrentRegion != null)
                {
                    DescribeCurrentRegion();
                }
            }
        }

        private void DescribeCurrentRegion()
        {
            if (World == null || CurrentRegion == null) return;

            GameLog.Add("");
            GameLog.Add(World.GetString(CurrentRegion.DescriptionId));
            
            // List locations in region
            if (CurrentRegion.Locations.Any())
            {
                GameLog.Add("");
                GameLog.Add("Locations in this area:");
                foreach (var location in CurrentRegion.Locations)
                {
                    GameLog.Add($"  - {World.GetString(location.NameId)}");
                }
            }

            // List NPCs in region or current location
            var npcs = CurrentLocation != null ? 
                      CurrentLocation.NPCs : 
                      CurrentRegion.NPCs;
            
            if (npcs.Any())
            {
                GameLog.Add("");
                GameLog.Add("You see:");
                foreach (var npcIndex in npcs)
                {
                    var npc = World.GetWorldData().NPCs[npcIndex];
                    GameLog.Add($"  - {World.GetString(npc.NameId)}");
                }
            }
        }

        public void NavigateToLocation(Location location)
        {
            CurrentLocation = location;
            GameLog.Add($"You are now at: {World?.GetString(location.NameId)}");
            GameLog.Add(World?.GetString(location.DescriptionId) ?? "");
            DescribeCurrentLocation();
        }

        private void DescribeCurrentLocation()
        {
            if (World == null || CurrentLocation == null) return;

            // List NPCs in location
            if (CurrentLocation.NPCs.Any())
            {
                GameLog.Add("");
                GameLog.Add("People here:");
                foreach (var npcIndex in CurrentLocation.NPCs)
                {
                    var npc = World.GetWorldData().NPCs[npcIndex];
                    GameLog.Add($"  - {World.GetString(npc.NameId)}");
                }
            }

            // List items in location
            if (CurrentLocation.Items.Any())
            {
                GameLog.Add("");
                GameLog.Add("Items:");
                foreach (var itemIndex in CurrentLocation.Items)
                {
                    var item = World.GetWorldData().Items[itemIndex];
                    GameLog.Add($"  - {World.GetString(item.NameId)}");
                }
            }
        }
    }
}
