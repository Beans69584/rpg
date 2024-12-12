using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RPG.UI;
using RPG.World;
using RPG.World.Data;
using RPG.Core.Player;
using RPG.Common;
using RPG.Core.Managers;
using RPG.Core.Systems;
using RPG.Core.Player.Common;
using RPG.Utils;
using RPG.Commands;
using RPG.Save;

namespace RPG.Core
{
    /// <summary>
    /// Represents a text message with an associated console colour.
    /// </summary>
    /// <param name="Text">The text content of the message.</param>
    /// <param name="Color">The colour to display the text in. Defaults to grey.</param>
    public record struct ColoredText(string Text, ConsoleColor Color = ConsoleColor.Gray)
    {
        /// <summary>
        /// Implicitly converts a string to a ColoredText with default grey colouring.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        public static implicit operator ColoredText(string text)
        {
            return new(text);
        }
    }

    /// <summary>
    /// Maintains the current state of the game, including player attributes, world state, and game progression.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// A chronological list of coloured text messages representing the game's log history.
        /// </summary>
        public List<ColoredText> GameLog { get; } = [];

        /// <summary>
        /// Manages the console window interface and display elements.
        /// </summary>
        [JsonIgnore]
        public ConsoleWindowManager WindowManager { get; set; }

        /// <summary>
        /// The name of the player character.
        /// </summary>
        public string PlayerName { get; set; } = "Hero";

        /// <summary>
        /// The current level of the player character.
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// The current hit points of the player character.
        /// </summary>
        public int HP { get; set; } = 100;

        /// <summary>
        /// The maximum hit points the player character can have.
        /// </summary>
        public int MaxHP { get; set; } = 100;

        /// <summary>
        /// The amount of gold currency the player possesses.
        /// </summary>
        public int Gold { get; set; } = 100;

        /// <summary>
        /// Collection of player statistics and their corresponding values.
        /// </summary>
        public Dictionary<string, int> Stats { get; } = [];

        /// <summary>
        /// List of items currently in the player's inventory.
        /// </summary>
        public List<string> Inventory { get; } = [];

        /// <summary>
        /// Dictionary of boolean flags tracking various game states and events.
        /// </summary>
        public Dictionary<string, bool> GameFlags { get; } = [];

        /// <summary>
        /// Handles player commands and interactions.
        /// </summary>
        [JsonIgnore]
        public CommandHandler CommandHandler { get; }

        /// <summary>
        /// Indicates whether the game is currently running.
        /// </summary>
        public bool Running { get; set; } = true;

        /// <summary>
        /// Manages localisation and language settings.
        /// </summary>
        [JsonIgnore]
        public LocalisationManager Localization { get; }

        /// <summary>
        /// Loads and manages the game world data.
        /// </summary>
        [JsonIgnore]
        public WorldLoader? World { get; set; }

        /// <summary>
        /// The current region the player is in.
        /// </summary>
        public WorldRegion? CurrentRegion { get; set; }

        /// <summary>
        /// The path to the current world data file.
        /// </summary>
        public string CurrentWorldPath { get; private set; } = "";

        /// <summary>
        /// The current location of the player within the world.
        /// </summary>
        public Location? CurrentLocation { get; set; }

        /// <summary>
        /// The path to the world data file.
        /// </summary>
        public string? WorldPath { get; set; }

        /// <summary>
        /// A set of locations the player has discovered.
        /// </summary>
        public HashSet<string> DiscoveredLocations { get; } = [];

        /// <summary>
        /// A dictionary tracking the exploration progress of each region.
        /// </summary>
        public Dictionary<int, float> RegionExploration { get; } = [];

        /// <summary>
        /// The total playtime of the current game session.
        /// </summary>
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The current position of the player within the world.
        /// </summary>
        public Vector2 PlayerPosition { get; set; } = new();

        /// <summary>
        /// The current player character.
        /// </summary>
        public Person? CurrentPlayer { get; set; }

        /// <summary>
        /// The current experience points of the player.
        /// </summary>
        public int CurrentExperience { get; set; }

        /// <summary>
        /// A dictionary tracking the player's reputation with various factions.
        /// </summary>
        public Dictionary<string, int> Reputation { get; } = [];

        /// <summary>
        /// Metadata for the current save file.
        /// </summary>
        public SaveMetadata? CurrentSaveMetadata { get; set; }

        /// <summary>
        /// Handles player input and interactions.
        /// </summary>
        [JsonIgnore]
        public Input Input { get; }

        /// <summary>
        /// Manages quests and quest progression.
        /// </summary>
        [JsonIgnore]
        public QuestManager QuestManager { get; }

        /// <summary>
        /// Manages the player's experience points and level progression.
        /// </summary>
        [JsonIgnore]
        public ExperienceSystem ExperienceSystem { get; }

        /// <summary>
        /// Indicates whether player input is currently suspended.
        /// </summary>
        [JsonIgnore]
        public bool InputSuspended { get; set; }

        /// <summary>
        /// Event triggered when the player's character class changes.
        /// </summary>
        public event Action<Person>? OnPlayerClassChanged;

        /// <summary>
        /// Initialises a new instance of the <see cref="GameState"/> class.
        /// </summary>
        [JsonConstructor]
        public GameState()
        {
            CommandHandler = new CommandHandler();
            Input = new Input();
            Localization = new LocalisationManager() { CurrentCulture = System.Globalization.CultureInfo.CurrentCulture };
            QuestManager = new QuestManager(this);
            ExperienceSystem = new ExperienceSystem(this);
            WindowManager = null!;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="GameState"/> class with a specified console window manager.
        /// </summary>
        public GameState(ConsoleWindowManager manager) : this()
        {
            WindowManager = manager;
            Localization.SetLanguage(GameSettings.CurrentLanguage);

            GameLog.Add(new ColoredText(Localization.GetString("Welcome_Message")));
            GameLog.Add(new ColoredText(Localization.GetString("Help_Hint")));
        }

        /// <summary>
        /// Adds a new message to the game's log history.
        /// </summary>
        /// <param name="message">The coloured text message to add to the log.</param>
        public void AddLogMessage(ColoredText message)
        {
            GameLog.Add(message);
        }

        /// <summary>
        /// Moves the player to a new location and updates the game state accordingly.
        /// </summary>
        /// <param name="location">The destination location to navigate to.</param>
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

        private readonly Dictionary<string, bool> _flags = [];

        /// <summary>
        /// Sets a game flag to track the state of specific game events or conditions.
        /// </summary>
        /// <param name="flag">The identifier of the flag to set.</param>
        /// <param name="value">The boolean value to assign to the flag.</param>
        public void SetFlag(string flag, bool value)
        {
            _flags[flag] = value;
        }

        /// <summary>
        /// Retrieves the current state of a game flag.
        /// </summary>
        /// <param name="flag">The identifier of the flag to check.</param>
        /// <returns>True if the flag exists and is set to true; otherwise, false.</returns>
        public bool GetFlag(string flag)
        {
            return _flags.TryGetValue(flag, out bool value) && value;
        }

        /// <summary>
        /// Adjusts the player's reputation with a specific faction.
        /// </summary>
        /// <param name="faction">The identifier of the faction.</param>
        /// <param name="amount">The amount to modify the reputation by. Positive values increase reputation, negative values decrease it.</param>
        public void ModifyReputation(string faction, int amount)
        {
            if (!Reputation.ContainsKey(faction))
            {
                Reputation[faction] = 0;
            }
            Reputation[faction] = Math.Clamp(Reputation[faction] + amount, -100, 100);

            string message = amount > 0 ?
                $"Reputation increased with {faction}" :
                $"Reputation decreased with {faction}";
            GameLog.Add(new ColoredText(message, amount > 0 ? ConsoleColor.Green : ConsoleColor.Red));
        }

        /// <summary>
        /// Retrieves the current reputation value for a specific faction.
        /// </summary>
        /// <param name="faction">The identifier of the faction.</param>
        /// <returns>The current reputation value, defaulting to 0 if not previously set.</returns>
        public int GetReputation(string faction)
        {
            return Reputation.GetValueOrDefault(faction, 0);
        }

        /// <summary>
        /// Automatically saves the current game state.
        /// </summary>
        public void AutoSave()
        {
            try
            {
                SaveManager.AutoSave(this);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to create autosave");
            }
        }

        /// <summary>
        /// Changes the player's character class and triggers associated events.
        /// </summary>
        /// <param name="newPlayerClass">The new character class to transform the player into.</param>
        public void TransformPlayerClass(Person newPlayerClass)
        {
            CurrentPlayer = newPlayerClass;
            OnPlayerClassChanged?.Invoke(newPlayerClass);
        }
    }
}