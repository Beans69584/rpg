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
    public record struct ColoredText(string Text, ConsoleColor Color = ConsoleColor.Gray)
    {
        public static implicit operator ColoredText(string text)
        {
            return new(text);
        }
    }

    public class GameState
    {
        public List<ColoredText> GameLog { get; } = [];

        [JsonIgnore]
        public ConsoleWindowManager WindowManager { get; set; }

        public string PlayerName { get; set; } = "Hero";
        public int Level { get; set; } = 1;
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public int Gold { get; set; } = 100;
        public Dictionary<string, int> Stats { get; } = [];
        public List<string> Inventory { get; } = [];
        public Dictionary<string, bool> GameFlags { get; } = [];

        [JsonIgnore]
        public CommandHandler CommandHandler { get; }

        public bool Running { get; set; } = true;

        [JsonIgnore]
        public LocalizationManager Localization { get; }

        [JsonIgnore]
        public WorldLoader? World { get; set; }

        public WorldRegion? CurrentRegion { get; set; }
        public string CurrentWorldPath { get; private set; } = "";
        public Location? CurrentLocation { get; set; }
        public string? WorldPath { get; set; }
        public HashSet<string> DiscoveredLocations { get; } = [];
        public Dictionary<int, float> RegionExploration { get; } = [];
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;
        public Vector2 PlayerPosition { get; set; } = new();
        public Person? CurrentPlayer { get; set; }
        public int CurrentExperience { get; set; }
        public Dictionary<string, int> Reputation { get; } = [];
        public SaveMetadata? CurrentSaveMetadata { get; set; }

        [JsonIgnore]
        public Input Input { get; }

        [JsonIgnore]
        public QuestManager QuestManager { get; }

        [JsonIgnore]
        public ExperienceSystem ExperienceSystem { get; }

        [JsonIgnore]
        public bool InputSuspended { get; set; }

        public event Action<Person>? OnPlayerClassChanged;

        [JsonConstructor]
        public GameState()
        {
            CommandHandler = new CommandHandler();
            Input = new Input();
            Localization = new LocalizationManager() { CurrentCulture = System.Globalization.CultureInfo.CurrentCulture };
            QuestManager = new QuestManager(this);
            ExperienceSystem = new ExperienceSystem(this);
            WindowManager = null!;
        }

        public GameState(ConsoleWindowManager manager) : this()
        {
            WindowManager = manager;
            Localization.SetLanguage(GameSettings.CurrentLanguage);

            GameLog.Add(new ColoredText(Localization.GetString("Welcome_Message")));
            GameLog.Add(new ColoredText(Localization.GetString("Help_Hint")));
        }

        public void AddLogMessage(ColoredText message)
        {
            GameLog.Add(message);
        }

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

        public void SetFlag(string flag, bool value)
        {
            _flags[flag] = value;
        }

        public bool GetFlag(string flag)
        {
            return _flags.TryGetValue(flag, out bool value) && value;
        }

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

        public int GetReputation(string faction)
        {
            return Reputation.GetValueOrDefault(faction, 0);
        }

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

        public void TransformPlayerClass(Person newPlayerClass)
        {
            CurrentPlayer = newPlayerClass;
            OnPlayerClassChanged?.Invoke(newPlayerClass);
        }
    }
}