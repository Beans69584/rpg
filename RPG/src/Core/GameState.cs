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
        /// Initialises a new instance of the <see cref="GameState"/> class.
        /// </summary>
        /// <param name="manager">The console window manager to use.</param>
        public GameState(ConsoleWindowManager manager)
        {
            WindowManager = manager;
            Localization = new LocalizationManager() { CurrentCulture = System.Globalization.CultureInfo.CurrentCulture };
            Localization.SetLanguage(GameSettings.CurrentLanguage);
            CommandHandler = new CommandHandler();
            GameLog.Add(new ColoredText(Localization.GetString("Welcome_Message")));
            GameLog.Add(new ColoredText(Localization.GetString("Help_Hint")));
        }

        /// <summary>
        /// Processes the player input and updates the game state.
        /// </summary>
        /// <param name="slot">The save slot to use.</param>
        public void SaveGame(string slot)
        {
            SaveData saveData = new()
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
                Inventory = [.. Inventory],
                GameFlags = new Dictionary<string, bool>(GameFlags)
            };

            SaveManager.Save(saveData, slot);
            GameLog.Add(new ColoredText($"Game saved to slot {slot}"));
        }

        /// <summary>
        /// Loads the game state from a save slot.
        /// </summary>
        /// <param name="slot">The save slot to load from.</param>
        /// <returns>True if the game was loaded successfully, otherwise false.</returns>
        public bool LoadGame(string slot)
        {
            (SaveMetadata? metadata, SaveData? saveData) = SaveManager.Load(slot);
            if (metadata == null || saveData == null) return false;

            // Load world first without setting starting region
            if (!string.IsNullOrEmpty(saveData.WorldPath))
            {
                try
                {
                    LoadWorld(saveData.WorldPath, false);
                }
                catch (Exception ex)
                {
                    GameLog.Add(new ColoredText($"Failed to load world: {ex.Message}"));
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
            foreach (KeyValuePair<string, int> stat in saveData.Stats)
                Stats[stat.Key] = stat.Value;
            Inventory.Clear();
            Inventory.AddRange(saveData.Inventory);
            GameFlags.Clear();
            foreach (KeyValuePair<string, bool> flag in saveData.GameFlags)
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
                        GameLog.Add(new ColoredText("Your previous location was not found. Moving to a safe location..."));
                    }
                }

                if (CurrentRegion != null)
                {
                    GameLog.Add(new ColoredText($"You are in: {World.GetString(CurrentRegion.NameId)}"));
                    DescribeCurrentRegion();
                }
                else
                {
                    GameLog.Add(new ColoredText("ERROR: Could not find any valid location in the world!"));
                }
            }

            GameLog.Add(new ColoredText($"Game loaded from slot {slot}"));
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

        /// <summary>
        /// Loads a new game world from a file.
        /// </summary>
        /// <param name="worldPath">The path to the world file.</param>
        /// <param name="isNewGame">Whether this is a new game or not.</param>
        public void LoadWorld(string worldPath, bool isNewGame = false)
        {
            World = new WorldLoader(worldPath);
            CurrentWorldPath = worldPath;

            GameLog.Add(new ColoredText($"Loaded world: {World.GetWorldData().Header.Name}"));

            if (isNewGame)
            {
                // Only set starting region for new games
                CurrentRegion = World.GetStartingRegion();
                GameLog.Add(new ColoredText($"You are in: {World.GetString(CurrentRegion?.NameId ?? 0)}"));
                if (CurrentRegion != null)
                {
                    DescribeCurrentRegion();
                }
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
