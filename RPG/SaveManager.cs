using System.Text.Json;
using System.IO.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPG.Utils;

namespace RPG
{
    /// <summary>
    /// Manages saving and loading game data to disk.
    /// </summary>
    public static class SaveManager
    {
        private const int CURRENT_SAVE_VERSION = 1;
        private const int MAX_AUTOSAVES = 3;
        private const int MAX_BACKUPS = 5;
        private static readonly string BaseDirectory = PathUtilities.GetSettingsDirectory();
        private static readonly string WorldsDirectory = Path.Combine(BaseDirectory, "Worlds");

        static SaveManager()
        {
            Directory.CreateDirectory(WorldsDirectory);
        }

        private static string GetWorldDirectory(string worldName)
        {
            if (string.IsNullOrEmpty(worldName))
                throw new ArgumentException("World name cannot be null or empty", nameof(worldName));

            string safeName = SanitizeWorldName(worldName);
            string worldPath = Path.Combine(WorldsDirectory, safeName);
            Directory.CreateDirectory(worldPath);
            Directory.CreateDirectory(Path.Combine(worldPath, "backups"));
            Directory.CreateDirectory(Path.Combine(worldPath, "autosaves"));
            return worldPath;
        }

        private static string SanitizeWorldName(string worldName)
        {
            // Replace invalid characters with underscores
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            return string.Join("_", worldName.Split(invalid.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        /// <summary>
        /// Saves the game data to disk.
        /// </summary>
        /// <param name="saveData">The game data to save.</param>
        /// <param name="slot">The save slot to use.</param>
        /// <param name="isAutosave">Whether this save is an autosave.</param>
        public static void Save(SaveData saveData, string slot, bool isAutosave = false)
        {
            string worldName = GetWorldNameFromPath(saveData.WorldPath);
            string worldDir = GetWorldDirectory(worldName);

            SaveMetadata metadata = new()
            {
                Version = CURRENT_SAVE_VERSION,
                SaveTime = DateTime.UtcNow,
                LastPlayedCharacter = saveData.PlayerName,
                TotalPlayTime = saveData.TotalPlayTime,
                SaveType = isAutosave ? SaveType.Autosave : SaveType.Manual,
                WorldPath = saveData.WorldPath,
                CharacterLevel = saveData.Level
            };

            string path = GetSavePath(worldDir, slot, isAutosave);

            // Create backup of existing save
            if (File.Exists(path) && !isAutosave)
            {
                CreateBackup(worldDir, slot);
            }

            // Serialize and compress
            using FileStream fs = File.Create(path);
            using GZipStream gz = new(fs, CompressionLevel.Optimal);
            using BinaryWriter writer = new(gz);

            byte[] metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata);
            writer.Write(metadataBytes.Length);
            writer.Write(metadataBytes);

            byte[] saveDataBytes = JsonSerializer.SerializeToUtf8Bytes(saveData);
            writer.Write(saveDataBytes.Length);
            writer.Write(saveDataBytes);

            // Cleanup old autosaves if needed
            if (isAutosave)
            {
                CleanupOldAutosaves();
            }
        }

        /// <summary>
        /// Loads the game data from disk.
        /// </summary>
        /// <param name="slot">The save slot to load.</param>
        /// <param name="isAutosave">Whether to load an autosave.</param>
        /// <returns>A tuple containing the save metadata and data.</returns>
        public static (SaveMetadata? Metadata, SaveData? Data) Load(string slot, bool isAutosave = false)
        {
            // We need to scan all world directories since we don't know which world the save belongs to
            foreach (string worldDir in Directory.GetDirectories(WorldsDirectory))
            {
                string path = GetSavePath(worldDir, slot, isAutosave);
                if (File.Exists(path))
                {
                    return LoadFromPath(path);
                }
            }

            return (null, null);
        }

        private static (SaveMetadata? Metadata, SaveData? Data) LoadFromPath(string path)
        {
            try
            {
                using FileStream fs = File.OpenRead(path);
                using GZipStream gz = new(fs, CompressionMode.Decompress);
                using BinaryReader reader = new(gz);

                // Read metadata using source generation
                int metadataLength = reader.ReadInt32();
                byte[] metadataBytes = reader.ReadBytes(metadataLength);
                SaveMetadata? metadata = JsonSerializer.Deserialize<SaveMetadata>(metadataBytes);

                // Version check and migration if needed
                if (metadata?.Version < CURRENT_SAVE_VERSION)
                {
                    return LoadLegacySave(path);
                }

                // Read save data using source generation
                int saveDataLength = reader.ReadInt32();
                byte[] saveDataBytes = reader.ReadBytes(saveDataLength);
                SaveData? saveData = JsonSerializer.Deserialize<SaveData>(saveDataBytes);

                return (metadata, saveData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading save {path}: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Gets a list of save files.
        /// </summary>
        /// <param name="includeAutosaves">Whether to include autosaves in the list.</param>
        /// <returns>A list of save files.</returns>
        public static List<SaveInfo> GetSaveFiles(bool includeAutosaves = true)
        {
            List<SaveInfo> saves = [];

            // Look in each world directory
            foreach (string worldDir in Directory.GetDirectories(WorldsDirectory))
            {
                // Get regular saves
                foreach (string file in Directory.GetFiles(worldDir, "*.save"))
                {
                    SaveInfo? info = GetSaveInfo(file, false);
                    if (info != null) saves.Add(info);
                }

                // Get autosaves if requested
                if (includeAutosaves)
                {
                    string autosaveDir = Path.Combine(worldDir, "autosaves");
                    if (Directory.Exists(autosaveDir))
                    {
                        foreach (string file in Directory.GetFiles(autosaveDir, "*.save"))
                        {
                            SaveInfo? info = GetSaveInfo(file, true);
                            if (info != null) saves.Add(info);
                        }
                    }
                }
            }

            return [.. saves.OrderByDescending(s => s.Metadata.SaveTime)];
        }

        /// <summary>
        /// Creates an autosave from the current game data.
        /// </summary>
        /// <param name="saveData">The game data to save.</param>
        public static void CreateAutosave(SaveData saveData)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            Save(saveData, $"auto_{timestamp}", true);
        }

        /// <summary>
        /// Deletes a save file.
        /// </summary>
        /// <param name="slot">The save slot to delete.</param>
        /// <param name="isAutosave">Whether to delete an autosave.</param>
        public static void DeleteSave(string slot, bool isAutosave = false)
        {
            // Need to scan all world directories since we don't know which world the save belongs to
            foreach (string worldDir in Directory.GetDirectories(WorldsDirectory))
            {
                string path = GetSavePath(worldDir, slot, isAutosave);
                if (File.Exists(path))
                {
                    // Move to recycle bin instead of permanent deletion
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        path,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                    );
                    break;
                }
            }
        }

        /// <summary>
        /// Restores a backup to the main save file.
        /// </summary>
        /// <param name="slot">The save slot to restore.</param>
        /// <param name="backupIndex">The index of the backup to restore.</param>
        /// <returns>True if the backup was restored successfully.</returns>
        public static bool RestoreBackup(string slot, int backupIndex)
        {
            // Scan world directories to find the correct backup
            foreach (string worldDir in Directory.GetDirectories(WorldsDirectory))
            {
                string backupPath = GetBackupPath(worldDir, slot, backupIndex);
                string targetPath = GetSavePath(worldDir, slot, false);

                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, targetPath, true);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a save file exists in the specified slot.
        /// </summary>
        /// <param name="slot">The save slot to check.</param>
        /// <param name="isAutosave">Whether to check for an autosave.</param>
        /// <returns>True if a save file exists in the slot; otherwise, false.</returns>
        public static bool SaveExists(string slot, bool isAutosave = false)
        {
            return Directory.EnumerateDirectories(WorldsDirectory)
                .Select(worldDir => GetSavePath(worldDir, slot, isAutosave))
                .Any(File.Exists);
        }

        private static SaveInfo? GetSaveInfo(string path, bool isAutosave)
        {
            try
            {
                (SaveMetadata? metadata, SaveData? _) = Load(Path.GetFileNameWithoutExtension(path), isAutosave);
                if (metadata != null)
                {
                    return new SaveInfo
                    {
                        Slot = Path.GetFileNameWithoutExtension(path),
                        IsAutosave = isAutosave,
                        Metadata = metadata,
                        FilePath = path
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading save info {path}: {ex.Message}");
            }
            return null;
        }

        private static void CreateBackup(string worldDir, string slot)
        {
            string sourcePath = GetSavePath(worldDir, slot, false);
            if (!File.Exists(sourcePath)) return;

            string backupDir = Path.Combine(worldDir, "backups");
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupPath = Path.Combine(backupDir, $"{slot}_{timestamp}.backup");

            File.Copy(sourcePath, backupPath, true);
            CleanupOldBackups(worldDir, slot);
        }

        private static void CleanupOldAutosaves()
        {
            // Cleanup autosaves in all world directories
            foreach (string worldDir in Directory.GetDirectories(WorldsDirectory))
            {
                string autosaveDir = Path.Combine(worldDir, "autosaves");
                if (Directory.Exists(autosaveDir))
                {
                    IEnumerable<string> files = Directory.GetFiles(autosaveDir, "*.save")
                        .OrderByDescending(File.GetLastWriteTime)
                        .Skip(MAX_AUTOSAVES);

                    foreach (string file in files)
                    {
                        try { File.Delete(file); }
                        catch { /* Ignore cleanup errors */ }
                    }
                }
            }
        }

        private static void CleanupOldBackups(string worldDir, string slot)
        {
            string backupDir = Path.Combine(worldDir, "backups");
            if (Directory.Exists(backupDir))
            {
                IEnumerable<string> files = Directory.GetFiles(backupDir, $"{slot}_*.backup")
                    .OrderByDescending(File.GetLastWriteTime)
                    .Skip(MAX_BACKUPS);

                foreach (string file in files)
                {
                    try { File.Delete(file); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private static (SaveMetadata?, SaveData?) LoadLegacySave(string path)
        {
            // Handle loading and migrating old save formats
            try
            {
                string json = File.ReadAllText(path);
                SaveData? legacyData = JsonSerializer.Deserialize<SaveData>(json);
                if (legacyData == null) return (null, null);

                // Create metadata from legacy save
                SaveMetadata metadata = new()
                {
                    Version = 0,
                    SaveTime = File.GetLastWriteTime(path),
                    LastPlayedCharacter = legacyData.PlayerName,
                    TotalPlayTime = TimeSpan.Zero, // Not available in legacy saves
                    SaveType = SaveType.Manual,
                    WorldPath = legacyData.WorldPath,
                    CharacterLevel = legacyData.Level
                };

                return (metadata, legacyData);
            }
            catch
            {
                return (null, null);
            }
        }

        private static string GetSavePath(string worldDir, string slot, bool isAutosave)
        {
            string saveDir = isAutosave ? Path.Combine(worldDir, "autosaves") : worldDir;
            return Path.Combine(saveDir, $"{slot}.save");
        }

        private static string GetBackupPath(string worldDir, string slot, int backupIndex)
        {
            string backupDir = Path.Combine(worldDir, "backups");
            List<string> backups = [.. Directory.GetFiles(backupDir, $"{slot}_*.backup").OrderByDescending(File.GetLastWriteTime)];
            return backupIndex < backups.Count ? backups[backupIndex] : "";
        }

        private static string GetWorldNameFromPath(string worldPath)
        {
            // Extract world name from world file path
            // Example path: /path/to/Worlds/MyWorld/world.dat
            string? dirName = Path.GetDirectoryName(worldPath);
            return string.IsNullOrEmpty(dirName)
                ? throw new ArgumentException("Invalid world path", nameof(worldPath))
                : Path.GetFileName(dirName) ?? "default";
        }
    }

    /// <summary>
    /// Represents a save file.
    /// </summary>
    public class SaveInfo
    {
        /// <summary>
        /// The save slot.
        /// </summary>
        public string Slot { get; set; } = "";
        /// <summary>
        /// Whether this save is an autosave.
        /// </summary>
        public bool IsAutosave { get; set; }
        /// <summary>
        /// The save metadata.
        /// </summary>
        public SaveMetadata Metadata { get; set; } = new();
        /// <summary>
        /// The file path of the save.
        /// </summary>
        public string FilePath { get; set; } = "";

        /// <summary>
        /// Deconstructs the save info into its components.
        /// </summary>
        /// <param name="slot">The save slot.</param>
        /// <param name="data">The save data.</param>
        public void Deconstruct(out string slot, out SaveData data)
        {
            slot = Slot;
            (SaveMetadata? _, SaveData? saveData) = SaveManager.Load(Slot, IsAutosave);
            data = saveData ?? new SaveData();
        }
    }

    /// <summary>
    /// Represents metadata for a save file.
    /// </summary>
    public class SaveMetadata
    {
        /// <summary>
        /// Version of the save file format.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The time the save was created.
        /// </summary>
        public DateTime SaveTime { get; set; }
        /// <summary>
        /// The name of the last played character.
        /// </summary>
        public string LastPlayedCharacter { get; set; } = "";
        /// <summary>
        /// The total play time of the save.
        /// </summary>
        public TimeSpan TotalPlayTime { get; set; }
        /// <summary>
        /// The type of save.
        /// </summary>
        public SaveType SaveType { get; set; }
        /// <summary>
        /// The path to the world file.
        /// </summary>
        public string WorldPath { get; set; } = "";
        /// <summary>
        /// The level of the last played character.
        /// </summary>
        public int CharacterLevel { get; set; }
        /// <summary>
        /// Custom data for the save.
        /// </summary>
        public Dictionary<string, string> CustomData { get; set; } = [];
    }

    /// <summary>
    /// Represents the type of save.
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Manual save.
        /// </summary>
        Manual,
        /// <summary>
        /// Autosave.
        /// </summary>
        Autosave,
        /// <summary>
        /// Quicksave.
        /// </summary>
        Quicksave
    }
}
