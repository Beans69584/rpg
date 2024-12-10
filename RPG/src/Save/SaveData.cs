using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Save
{
    /// <summary>
    /// Represents save data for the game state
    /// </summary>
    public class SaveData
    {
        /// <summary>
        /// Gets or sets the player's name
        /// </summary>
        public string PlayerName { get; set; } = "Hero";
        /// <summary>
        /// Gets or sets the player's level
        /// </summary>
        public int Level { get; set; } = 1;
        /// <summary>
        /// Gets or sets the current hit points
        /// </summary>
        public int HP { get; set; } = 100;
        /// <summary>
        /// Gets or sets the maximum hit points
        /// </summary>
        public int MaxHP { get; set; } = 100;
        /// <summary>
        /// Gets or sets the current region identifier
        /// </summary>
        public string CurrentRegionId { get; set; } = "";
        /// <summary>
        /// Gets or sets the time when the game was saved
        /// </summary>
        public DateTime SaveTime { get; set; }
        /// <summary>
        /// Gets or sets the path to the world data
        /// </summary>
        public string WorldPath { get; set; } = "";
        /// <summary>
        /// Gets or sets the amount of gold
        /// </summary>
        public int Gold { get; set; } = 100;
        /// <summary>
        /// Gets or sets the player's statistics
        /// </summary>
        public Dictionary<string, int> Stats { get; set; } = [];
        /// <summary>
        /// Gets or sets the player's inventory items
        /// </summary>
        public List<string> Inventory { get; set; } = [];
        /// <summary>
        /// Gets or sets the game state flags
        /// </summary>
        public Dictionary<string, bool> GameFlags { get; set; } = [];
        /// <summary>
        /// Gets or sets the total time played
        /// </summary>
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;
        /// <summary>
        /// Gets or sets the last play session timestamp
        /// </summary>
        public DateTime LastPlayTime { get; set; } = DateTime.Now;
        /// <summary>
        /// Gets or sets the world seed
        /// </summary>
        public string WorldSeed { get; set; } = "";
        /// <summary>
        /// Gets or sets the world name
        /// </summary>
        public string WorldName { get; set; } = "";
        /// <summary>
        /// Gets or sets the world creation timestamp
        /// </summary>
        public DateTime WorldCreatedAt { get; set; }
        /// <summary>
        /// Gets the formatted display name for the save file
        /// </summary>
        [JsonIgnore]
        public string DisplayName => $"{PlayerName} - Level {Level} - {SaveTime:g}";

        /// <summary>
        /// Gets the formatted description of the save state
        /// </summary>
        [JsonIgnore]
        public string Description
        {
            get
            {
                List<string> stats =
                [
                    $"HP: {HP}/{MaxHP}",
                    $"Location: {CurrentRegionId}",
                    $"Gold: {Gold}",
                    $"Playtime: {FormatPlayTime(TotalPlayTime)}"
                ];
                return string.Join(" | ", stats);
            }
        }

        /// <summary>
        /// Gets or sets the world data at time of save
        /// </summary>
        public WorldData? World { get; set; }

        /// <summary>
        /// Gets or sets the current region ID
        /// </summary>
        public int CurrentRegionIndex { get; set; }

        /// <summary>
        /// Gets or sets the discovered locations
        /// </summary>
        public HashSet<string> DiscoveredLocations { get; set; } = [];

        /// <summary>
        /// Gets or sets the region exploration progress
        /// </summary>
        public Dictionary<int, float> RegionExploration { get; set; } = [];

        private static string FormatPlayTime(TimeSpan time)
        {
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{time.Minutes}m {time.Seconds}s";
        }

        /// <summary>
        /// Updates the total play time based on the last play session
        /// </summary>
        public void UpdatePlayTime()
        {
            DateTime now = DateTime.Now;
            TimeSpan sessionTime = now - LastPlayTime;
            TotalPlayTime += sessionTime;
            LastPlayTime = now;
        }

        /// <summary>
        /// Creates a new save from the current game state
        /// </summary>
        public static SaveData CreateFromState(GameState state)
        {
            return new SaveData
            {
                PlayerName = state.PlayerName,
                Level = state.Level,
                HP = state.HP,
                MaxHP = state.MaxHP,
                Gold = state.Gold,
                World = state.World?.GetWorldData(),
                CurrentRegionIndex = state.World?.GetWorldData().Regions.IndexOf(state.CurrentRegion ??
                    throw new InvalidOperationException("Current region is null")) ?? 0,
                DiscoveredLocations = state.DiscoveredLocations,
                RegionExploration = state.RegionExploration,
                SaveTime = DateTime.Now,
                TotalPlayTime = state.TotalPlayTime,
                LastPlayTime = DateTime.Now,
                WorldPath = state.WorldPath ?? "",
                WorldName = state.World?.GetWorldData().Header.Name ?? "Unknown World",
                WorldCreatedAt = state.World?.GetWorldData().Header.CreatedAt ?? DateTime.Now
            };
        }
    }
}
