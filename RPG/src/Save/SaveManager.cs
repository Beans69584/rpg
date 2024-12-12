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
    public enum SaveType
    {
        Manual,
        Autosave,
        Quicksave
    }

    public class SaveInfo
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public int PlayerLevel { get; set; }
        public string Location { get; set; } = "";
        public DateTime LastModified { get; set; }
        public SaveType Type { get; set; }
    }

    public class SaveMetadata
    {
        public string SaveId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastSavedAt { get; set; }
        public Dictionary<string, string> CustomData { get; set; } = [];
    }

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

                // Reinitialize non-serialized components
                state.WindowManager = windowManager;
                state.Localization.SetLanguage(GameSettings.CurrentLanguage);
                state.CurrentSaveMetadata = metadata;

                // Initialize WorldLoader with the saved world data
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

        public static bool SaveExists(string saveId)
        {
            string savePath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.sav");
            string metaPath = Path.Combine(PathUtilities.GetSavesDirectory(), $"{saveId}.meta");
            return File.Exists(savePath) && File.Exists(metaPath);
        }
    }

    public class SaveData
    {
        public GameState? GameState { get; set; }
        public WorldData? WorldData { get; set; }
    }
}