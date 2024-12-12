using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using RPG.Utils;
using System.IO.Compression;
using RPG.Core;
using RPG.UI;
using RPG.Player.Common;
using RPG.Common;
using RPG.World;
using RPG.World.Data;

namespace RPG.Save
{
    /// <summary>
    /// Specifies the type of save file in the game system.
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Represents a save file created by explicit player action.
        /// </summary>
        Manual,
        /// <summary>
        /// Represents an automatically created save file at predetermined points in gameplay.
        /// </summary>
        Autosave,
        /// <summary>
        /// Represents a save file created using the quick save functionality.
        /// </summary>
        Quicksave
    }

    /// <summary>
    /// Contains displayable information about a save file.
    /// </summary>
    public class SaveInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the save file.
        /// </summary>
        public string Id { get; set; } = "";
        /// <summary>
        /// Gets or sets the user-friendly name shown in the save menu.
        /// </summary>
        public string DisplayName { get; set; } = "";
        /// <summary>
        /// Gets or sets the name of the player character in the save file.
        /// </summary>
        public string PlayerName { get; set; } = "";
        /// <summary>
        /// Gets or sets the experience level of the player character.
        /// </summary>
        public int PlayerLevel { get; set; }
        /// <summary>
        /// Gets or sets the current location name of the player character.
        /// </summary>
        public string Location { get; set; } = "";
        /// <summary>
        /// Gets or sets the date and time when the save file was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }
        /// <summary>
        /// Gets or sets the category of the save file.
        /// </summary>
        public SaveType Type { get; set; }
    }

    /// <summary>
    /// Contains metadata information about a save file.
    /// </summary>
    public class SaveMetadata
    {
        /// <summary>
        /// Gets or sets the unique identifier for the save file.
        /// </summary>
        public string SaveId { get; set; } = "";
        /// <summary>
        /// Gets or sets the date and time when the save file was initially created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Gets or sets the date and time of the most recent save operation.
        /// </summary>
        public DateTime LastSavedAt { get; set; }
        /// <summary>
        /// Gets or sets additional custom data associated with the save file.
        /// </summary>
        public Dictionary<string, string> CustomData { get; set; } = [];
    }

    /// <summary>
    /// Provides functionality for managing game save files, including saving, loading, and maintenance operations.
    /// </summary>
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new Vector2JsonConverter(),
                new PersonJsonConverter()
            }
        };

        /// <summary>
        /// Saves the current game state to a file with the specified identifier.
        /// </summary>
        /// <param name="saveId">The unique identifier for the save file.</param>
        /// <param name="state">The current game state to be saved.</param>
        public static void SaveGame(string saveId, GameState state)
        {
            string savesPath = PathUtilities.GetSavesDirectory();
            string savePath = Path.Combine(savesPath, $"{saveId}.sav");
            string metaPath = Path.Combine(savesPath, $"{saveId}.meta");

            Directory.CreateDirectory(savesPath);

            // Use existing metadata if available, otherwise create new
            SaveMetadata metadata = state.CurrentSaveMetadata ?? new SaveMetadata
            {
                SaveId = saveId,
                CreatedAt = DateTime.UtcNow,
                LastSavedAt = DateTime.UtcNow,
                CustomData = []
            };

            // Update metadata
            metadata.LastSavedAt = DateTime.UtcNow;
            metadata.CustomData["DisplayName"] = state.PlayerName;
            metadata.CustomData["PlayerName"] = state.PlayerName;
            metadata.CustomData["PlayerLevel"] = state.Level.ToString();
            metadata.CustomData["Location"] = state.World?.GetString(state.CurrentRegion?.NameId ?? 0) ?? "Unknown";
            metadata.CustomData["SaveType"] = SaveType.Manual.ToString();

            // Create save data container with full world state
            SaveData saveData = new()
            {
                GameState = state,
                WorldData = state.World?.GetWorldData() ?? throw new InvalidOperationException("No world data available to save")
            };

            // Save both game state and world data in one file
            using (FileStream fs = File.Create(savePath))
            using (GZipStream gzip = new(fs, CompressionMode.Compress))
            {
                JsonSerializer.Serialize(gzip, saveData, SerializerOptions);
            }

            // Save metadata
            File.WriteAllText(metaPath, JsonSerializer.Serialize(metadata, SerializerOptions));
        }

        /// <summary>
        /// Loads a game state from a save file with the specified identifier.
        /// </summary>
        /// <param name="saveId">The unique identifier of the save file to load.</param>
        /// <param name="windowManager">The console window manager instance for the game.</param>
        /// <returns>The loaded game state, or null if loading fails.</returns>
        public static GameState? LoadGame(string saveId, ConsoleWindowManager windowManager)
        {
            string savePath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.sav");
            string metaPath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.meta");

            if (!File.Exists(savePath) || !File.Exists(metaPath))
            {
                Logger.Instance.Error("Save file not found: {Path}", savePath);
                return null;
            }

            try
            {
                Logger.Instance.Debug("Starting to load save: {SaveId}", saveId);

                // Load metadata
                SaveMetadata? metadata = JsonSerializer.Deserialize<SaveMetadata>(
                    File.ReadAllText(metaPath), SerializerOptions);

                if (metadata == null)
                {
                    Logger.Instance.Error("Failed to load save metadata");
                    return null;
                }

                // Load save data
                using FileStream fs = File.OpenRead(savePath);
                using GZipStream gzip = new(fs, CompressionMode.Decompress);
                using MemoryStream ms = new();
                gzip.CopyTo(ms);
                ms.Position = 0;

                SaveData? saveData = JsonSerializer.Deserialize<SaveData>(ms.ToArray(), SerializerOptions);

                if (saveData?.GameState == null || saveData.WorldData == null)
                {
                    Logger.Instance.Error("Failed to deserialize save data");
                    return null;
                }

                GameState state = saveData.GameState;

                // ReInitialise non-serialized components
                state.WindowManager = windowManager;
                state.Localization.SetLanguage(GameSettings.CurrentLanguage);
                state.CurrentSaveMetadata = metadata;

                // Initialise WorldLoader with the saved world data
                state.World = new WorldLoader(saveData.WorldData);

                Logger.Instance.Information("Successfully loaded save: {SaveId}", saveId);
                return state;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to load save: {SaveId}", saveId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a list of all available save files and their information.
        /// </summary>
        /// <returns>A list of save file information, ordered by last modified date.</returns>
        public static List<SaveInfo> GetSaveFiles()
        {
            string savesPath = PathUtilities.GetSavesDirectory();
            if (!Directory.Exists(savesPath))
            {
                return [];
            }

            List<SaveInfo> saves = [];
            foreach (string metaFile in Directory.GetFiles(savesPath, "*.meta"))
            {
                try
                {
                    string json = File.ReadAllText(metaFile);
                    SaveMetadata? metadata = JsonSerializer.Deserialize<SaveMetadata>(json, SerializerOptions);

                    if (metadata != null)
                    {
                        saves.Add(new SaveInfo
                        {
                            Id = metadata.SaveId,
                            DisplayName = metadata.CustomData.GetValueOrDefault("DisplayName", "Unnamed Save"),
                            PlayerName = metadata.CustomData.GetValueOrDefault("PlayerName", "Unknown"),
                            PlayerLevel = int.Parse(metadata.CustomData.GetValueOrDefault("PlayerLevel", "1")),
                            Location = metadata.CustomData.GetValueOrDefault("Location", "Unknown"),
                            LastModified = metadata.LastSavedAt,
                            Type = Enum.Parse<SaveType>(metadata.CustomData.GetValueOrDefault("SaveType", "Manual"))
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex, "Failed to load save metadata: {File}", metaFile);
                }
            }

            return [.. saves.OrderByDescending(s => s.LastModified)];
        }

        /// <summary>
        /// Creates a backup copy of the specified save file.
        /// </summary>
        /// <param name="saveId">The unique identifier of the save file to back up.</param>
        public static void CreateBackup(string saveId)
        {
            string savesPath = PathUtilities.GetSavesDirectory();
            string backupsPath = PathUtilities.GetBackupsDirectory();
            string saveFile = Path.Combine(savesPath, $"{saveId}.sav");
            string metaFile = Path.Combine(savesPath, $"{saveId}.meta");

            if (!File.Exists(saveFile) || !File.Exists(metaFile))
            {
                return;
            }

            Directory.CreateDirectory(backupsPath);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(saveFile, Path.Combine(backupsPath, $"{saveId}_{timestamp}.sav"), true);
            File.Copy(metaFile, Path.Combine(backupsPath, $"{saveId}_{timestamp}.meta"), true);
        }

        /// <summary>
        /// Deletes a save file and its associated metadata.
        /// </summary>
        /// <param name="saveId">The unique identifier of the save file to delete.</param>
        /// <returns>True if the deletion was successful; otherwise, false.</returns>
        public static bool DeleteSave(string saveId)
        {
            try
            {
                string savesPath = PathUtilities.GetSavesDirectory();
                string saveFile = Path.Combine(savesPath, $"{saveId}.sav");
                string metaFile = Path.Combine(savesPath, $"{saveId}.meta");

                if (File.Exists(saveFile))
                {
                    File.Delete(saveFile);
                }
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to delete save: {SaveId}", saveId);
                return false;
            }
        }

        /// <summary>
        /// Creates an automatic save of the current game state, maintaining only the three most recent autosaves.
        /// </summary>
        /// <param name="state">The current game state to be saved.</param>
        public static void AutoSave(GameState state)
        {
            string autosavePath = PathUtilities.GetAutosavesDirectory();
            Directory.CreateDirectory(autosavePath);

            // Keep only last 3 autosaves
            IEnumerable<FileInfo> files = Directory.GetFiles(autosavePath, "*.sav")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .Skip(2);

            foreach (FileInfo? file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                    File.Delete(Path.ChangeExtension(file.FullName, ".meta"));
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex, "Failed to delete old autosave: {File}", file.Name);
                }
            }

            string saveId = $"autosave_{DateTime.Now:yyyyMMdd_HHmmss}";
            SaveGame(saveId, state);
        }

        /// <summary>
        /// Checks whether a save file with the specified identifier exists.
        /// </summary>
        /// <param name="saveId">The unique identifier to check.</param>
        /// <returns>True if both the save file and its metadata exist; otherwise, false.</returns>
        public static bool SaveExists(string saveId)
        {
            string savePath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.sav");
            string metaPath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.meta");
            return File.Exists(savePath) && File.Exists(metaPath);
        }
    }

    /// <summary>
    /// Represents the complete set of data required to save and restore a game state.
    /// </summary>
    public class SaveData
    {
        /// <summary>
        /// Gets or sets the current state of the game, including player and environment data.
        /// </summary>
        public GameState? GameState { get; set; }
        /// <summary>
        /// Gets or sets the persistent world data associated with the save.
        /// </summary>
        public WorldData? WorldData { get; set; }
    }
}