using System;
using System.Collections.Generic;
using System.Linq;

using RPG.UI;
using RPG.Save;
using RPG.Commands;
using RPG.World;
using RPG.World.Data;
using RPG.World.Generation;

namespace RPG.Core
{
    /// <summary>
    /// Represents a colored text entry in the game log.
    /// </summary>
    public record struct ColoredText(string Text, ConsoleColor Color = ConsoleColor.Gray)
    {
        /// <summary>
        /// Implicitly converts a string to ColoredText with default color.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        public static implicit operator ColoredText(string text)
        {
            return new(text);
        }
    }

    /// <summary>
    /// Manages the state of the game world.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Stores current GameLog to be displayed in the console.
        /// </summary>
        public List<ColoredText> GameLog { get; } = [];

        /// <summary>
        /// Adds a message to the game log.
        /// </summary>
        /// <param name="message">The message to add. Can be string or ColoredText.</param>
        public void AddLogMessage(ColoredText message)
        {
            GameLog.Add(message);
        }

        /// <summary>
        /// Manages the console window and input/output.
        /// </summary>
        public ConsoleWindowManager WindowManager { get; }
        /// <summary>
        /// The name of the player character.
        /// </summary>
        public string PlayerName { get; set; } = "Hero";
        /// <summary>
        /// The level of the player character.
        /// </summary>
        public int Level { get; set; } = 1;
        /// <summary>
        /// The current health points of the player character.
        /// </summary>
        public int HP { get; set; } = 100;
        /// <summary>
        /// The maximum health points of the player character.
        /// </summary>
        public int MaxHP { get; set; } = 100;
        /// <summary>
        /// The amount of gold the player character has.
        /// </summary>
        public int Gold { get; set; } = 100;
        /// <summary>
        /// The stats of the player character.
        /// </summary>
        public Dictionary<string, int> Stats { get; } = [];
        /// <summary>
        /// The inventory of the player character.
        /// </summary>
        public List<string> Inventory { get; } = [];
        /// <summary>
        /// The game flags that are set during gameplay.
        /// </summary>
        public Dictionary<string, bool> GameFlags { get; } = [];
        /// <summary>
        /// The command handler for processing player input.
        /// </summary>
        public CommandHandler CommandHandler { get; }
        /// <summary>
        /// Whether the game is running or not.
        /// </summary>
        public bool Running { get; set; } = true;
        /// <summary>
        /// The localization manager for the game.
        /// </summary>
        public LocalizationManager Localization { get; }
        /// <summary>
        /// The world loader for the game.
        /// </summary>
        public WorldLoader? World { get; private set; }
        /// <summary>
        /// The current region the player is in.
        /// </summary>
        public WorldRegion? CurrentRegion { get; set; }
        /// <summary>
        /// The current world path of the game.
        /// </summary>
        public string CurrentWorldPath { get; private set; } = "";
        /// <summary>
        /// The current location the player is in.
        /// </summary>
        public Location? CurrentLocation { get; set; }

        /// <summary>
        /// Path to the current world file
        /// </summary>
        public string? WorldPath { get; set; }

        /// <summary>
        /// Discovered locations in the world
        /// </summary>
        public HashSet<string> DiscoveredLocations { get; } = [];

        /// <summary>
        /// Region exploration progress
        /// </summary>
        public Dictionary<int, float> RegionExploration { get; } = [];

        /// <summary>
        /// Total play time for this save
        /// </summary>
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Last play session start time
        /// </summary>
        private DateTime SessionStartTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameState"/> class.
        /// </summary>
        /// <param name="manager">The console window manager to use.</param>
        public GameState(ConsoleWindowManager manager)
        {
            WindowManager = manager;
            Localization = new LocalizationManager() { CurrentCulture = System.Globalization.CultureInfo.CurrentCulture };
            Localization.SetLanguage(GameSettings.CurrentLanguage);
            CommandHandler = new CommandHandler();
            SessionStartTime = DateTime.Now;
            GameLog.Add(new ColoredText(Localization.GetString("Welcome_Message")));
            GameLog.Add(new ColoredText(Localization.GetString("Help_Hint")));
        }

        /// <summary>
        /// Updates total play time and saves the game
        /// </summary>
        public void SaveGame(string slot)
        {
            UpdatePlayTime();
            SaveData saveData = SaveData.CreateFromState(this);
            SaveManager.Save(saveData, slot);
            GameLog.Add(new ColoredText($"Game saved to slot {slot}"));
        }

        /// <summary>
        /// Updates the total play time
        /// </summary>
        private void UpdatePlayTime()
        {
            TimeSpan sessionTime = DateTime.UtcNow - SessionStartTime;
            TotalPlayTime += sessionTime;
            SessionStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Loads the game state from a save slot.
        /// </summary>
        /// <param name="slot">The save slot to load from.</param>
        /// <returns>True if the game was loaded successfully, otherwise false.</returns>
        public bool LoadGame(string slot)
        {
            (SaveMetadata? metadata, SaveData? saveData) = SaveManager.Load(slot);
            if (metadata == null || saveData == null || saveData.World == null) return false;

            try
            {
                // Load world data first
                if (!string.IsNullOrEmpty(saveData.WorldPath))
                {
                    LoadWorld(saveData.WorldPath, false);
                }

                // Load player state
                PlayerName = saveData.PlayerName;
                Level = saveData.Level;
                HP = saveData.HP;
                MaxHP = saveData.MaxHP;
                Gold = saveData.Gold;
                Stats.Clear();
                foreach (KeyValuePair<string, int> stat in saveData.Stats)
                    Stats[stat.Key] = stat.Value;
                Inventory.Clear();
                Inventory.AddRange(saveData.Inventory);
                GameFlags.Clear();
                foreach (KeyValuePair<string, bool> flag in saveData.GameFlags)
                    GameFlags[flag.Key] = flag.Value;
                TotalPlayTime = saveData.TotalPlayTime;
                SessionStartTime = DateTime.Now;

                // Load world state
                if (World != null && World.GetWorldData().Regions.Count > saveData.CurrentRegionIndex)
                {
                    CurrentRegion = World.GetWorldData().Regions[saveData.CurrentRegionIndex];
                    DiscoveredLocations.Clear();
                    DiscoveredLocations.UnionWith(saveData.DiscoveredLocations);
                    RegionExploration.Clear();
                    foreach (KeyValuePair<int, float> kvp in saveData.RegionExploration)
                        RegionExploration[kvp.Key] = kvp.Value;
                }
                else
                {
                    GameLog.Add(new ColoredText("Warning: Could not restore previous location", ConsoleColor.Yellow));
                    CurrentRegion = FindDefaultLocation();
                }

                GameLog.Add(new ColoredText($"Game loaded from slot {slot}"));
                return true;
            }
            catch (Exception ex)
            {
                GameLog.Add(new ColoredText($"Error loading save: {ex.Message}", ConsoleColor.Red));
                return false;
            }
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

        /// <summary>
        /// Loads a new game world from a file.
        /// </summary>
        /// <param name="worldPath">The path to the world file.</param>
        /// <param name="isNewGame">Whether this is a new game or not.</param>
        public void LoadWorld(string worldPath, bool isNewGame = false)
        {
            WorldPath = worldPath;
            World = new WorldLoader(worldPath);
            CurrentWorldPath = worldPath;

            GameLog.Add(new ColoredText($"Loaded world: {World.GetWorldData().Header.Name}"));

            if (isNewGame)
            {
                // Initialize new game state
                CurrentRegion = FindDefaultLocation();
                SessionStartTime = DateTime.Now;
                TotalPlayTime = TimeSpan.Zero;
            }

            if (CurrentRegion != null)
            {
                GameLog.Add(new ColoredText($"You are in: {World.GetString(CurrentRegion?.NameId ?? 0)}"));
                DescribeCurrentRegion();
            }
        }

        private void DescribeCurrentRegion()
        {
            if (World == null || CurrentRegion == null) return;

            GameLog.Add(new ColoredText(""));
            GameLog.Add(new ColoredText(World.GetString(CurrentRegion.DescriptionId)));

            // List locations in region
            if (CurrentRegion.Locations.Any())
            {
                GameLog.Add(new ColoredText(""));
                GameLog.Add(new ColoredText("Locations in this area:"));
                foreach (Location location in CurrentRegion.Locations)
                {
                    GameLog.Add(new ColoredText($"  - {World.GetString(location.NameId)}"));
                }
            }

            // List NPCs in region or current location
            List<int> npcs = CurrentLocation != null ?
                      CurrentLocation.NPCs :
                      CurrentRegion.NPCs;

            if (npcs.Any())
            {
                GameLog.Add(new ColoredText(""));
                GameLog.Add(new ColoredText("You see:"));
                foreach (int npcIndex in npcs)
                {
                    Entity npc = World.GetWorldData().NPCs[npcIndex];
                    GameLog.Add(new ColoredText($"  - {World.GetString(npc.NameId)}"));
                }
            }
        }

        /// <summary>
        /// Navigates the player to a new location.
        /// </summary>
        /// <param name="location">The location to navigate to.</param>
        public void NavigateToLocation(Location location)
        {
            CurrentLocation = location;
            GameLog.Add(new ColoredText($"You are now at: {World?.GetString(location.NameId)}"));
            GameLog.Add(new ColoredText(World?.GetString(location.DescriptionId) ?? ""));
            DescribeCurrentLocation();
        }

        private void DescribeCurrentLocation()
        {
            if (World == null || CurrentLocation == null) return;

            // List NPCs in location
            if (CurrentLocation.NPCs.Any())
            {
                GameLog.Add(new ColoredText(""));
                GameLog.Add(new ColoredText("People here:"));
                foreach (int npcIndex in CurrentLocation.NPCs)
                {
                    Entity npc = World.GetWorldData().NPCs[npcIndex];
                    GameLog.Add(new ColoredText($"  - {World.GetString(npc.NameId)}"));
                }
            }

            // List items in location
            if (CurrentLocation.Items.Any())
            {
                GameLog.Add(new ColoredText(""));
                GameLog.Add(new ColoredText("Items:"));
                foreach (int itemIndex in CurrentLocation.Items)
                {
                    Item item = World.GetWorldData().Items[itemIndex];
                    GameLog.Add(new ColoredText($"  - {World.GetString(item.NameId)}"));
                }
            }
        }
    }
}