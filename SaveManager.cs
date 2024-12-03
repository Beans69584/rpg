using System.Text.Json;
using System.IO.Compression;

namespace RPG
{
    public static class SaveManager
    {
        private const int CURRENT_SAVE_VERSION = 1;
        private const int MAX_AUTOSAVES = 3;
        private const int MAX_BACKUPS = 5;

        private static readonly string SaveDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX
                    ? Environment.SpecialFolder.Personal
                    : Environment.SpecialFolder.ApplicationData
            ),
            Environment.OSVersion.Platform == PlatformID.Unix ||
            Environment.OSVersion.Platform == PlatformID.MacOSX
                ? "Library/Application Support/DemoRPG/Saves"
                : "DemoRPG/Saves"
        );

        private static readonly string BackupDirectory = Path.Combine(SaveDirectory, "Backups");
        private static readonly string AutosaveDirectory = Path.Combine(SaveDirectory, "Autosaves");

        static SaveManager()
        {
            Directory.CreateDirectory(SaveDirectory);
            Directory.CreateDirectory(BackupDirectory);
            Directory.CreateDirectory(AutosaveDirectory);
        }

        public static void Save(SaveData saveData, string slot, bool isAutosave = false)
        {
            var metadata = new SaveMetadata
            {
                Version = CURRENT_SAVE_VERSION,
                SaveTime = DateTime.UtcNow,
                LastPlayedCharacter = saveData.PlayerName,
                TotalPlayTime = saveData.TotalPlayTime,
                SaveType = isAutosave ? SaveType.Autosave : SaveType.Manual,
                WorldPath = saveData.WorldPath,
                CharacterLevel = saveData.Level
            };

            string path = GetSavePath(slot, isAutosave);
            
            // Create backup of existing save
            if (File.Exists(path) && !isAutosave)
            {
                CreateBackup(slot);
            }

            // Serialize and compress
            using var fs = File.Create(path);
            using var gz = new GZipStream(fs, CompressionMode.Optimal);
            using var writer = new BinaryWriter(gz);

            // Write metadata first
            var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata);
            writer.Write(metadataBytes.Length);
            writer.Write(metadataBytes);

            // Write save data
            var saveDataBytes = JsonSerializer.SerializeToUtf8Bytes(saveData);
            writer.Write(saveDataBytes.Length);
            writer.Write(saveDataBytes);

            // Cleanup old autosaves if needed
            if (isAutosave)
            {
                CleanupOldAutosaves();
            }
        }

        public static (SaveMetadata? Metadata, SaveData? Data) Load(string slot, bool isAutosave = false)
        {
            string path = GetSavePath(slot, isAutosave);
            if (!File.Exists(path)) return (null, null);

            try
            {
                using var fs = File.OpenRead(path);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                using var reader = new BinaryReader(gz);

                // Read metadata
                int metadataLength = reader.ReadInt32();
                var metadataBytes = reader.ReadBytes(metadataLength);
                var metadata = JsonSerializer.Deserialize<SaveMetadata>(metadataBytes);

                // Version check and migration if needed
                if (metadata?.Version < CURRENT_SAVE_VERSION)
                {
                    return LoadLegacySave(path);
                }

                // Read save data
                int saveDataLength = reader.ReadInt32();
                var saveDataBytes = reader.ReadBytes(saveDataLength);
                var saveData = JsonSerializer.Deserialize<SaveData>(saveDataBytes);

                return (metadata, saveData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading save {path}: {ex.Message}");
                return (null, null);
            }
        }

        public static List<SaveInfo> GetSaveFiles(bool includeAutosaves = true)
        {
            var saves = new List<SaveInfo>();
            
            // Get manual saves
            foreach (var file in Directory.GetFiles(SaveDirectory, "*.save"))
            {
                var info = GetSaveInfo(file, false);
                if (info != null) saves.Add(info);
            }

            // Get autosaves if requested
            if (includeAutosaves)
            {
                foreach (var file in Directory.GetFiles(AutosaveDirectory, "*.save"))
                {
                    var info = GetSaveInfo(file, true);
                    if (info != null) saves.Add(info);
                }
            }

            return saves.OrderByDescending(s => s.Metadata.SaveTime).ToList();
        }

        public static void CreateAutosave(SaveData saveData)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            Save(saveData, $"auto_{timestamp}", true);
        }

        public static void DeleteSave(string slot, bool isAutosave = false)
        {
            string path = GetSavePath(slot, isAutosave);
            if (File.Exists(path))
            {
                // Move to recycle bin instead of permanent deletion
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );
            }
        }

        public static bool RestoreBackup(string slot, int backupIndex)
        {
            string backupPath = GetBackupPath(slot, backupIndex);
            string targetPath = GetSavePath(slot, false);

            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, targetPath, true);
                return true;
            }

            return false;
        }

        private static SaveInfo? GetSaveInfo(string path, bool isAutosave)
        {
            try
            {
                var (metadata, _) = Load(Path.GetFileNameWithoutExtension(path), isAutosave);
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

        private static void CreateBackup(string slot)
        {
            string sourcePath = GetSavePath(slot, false);
            if (!File.Exists(sourcePath)) return;

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupPath = Path.Combine(BackupDirectory, $"{slot}_{timestamp}.backup");
            
            File.Copy(sourcePath, backupPath, true);
            CleanupOldBackups(slot);
        }

        private static void CleanupOldAutosaves()
        {
            var files = Directory.GetFiles(AutosaveDirectory, "*.save")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(MAX_AUTOSAVES);

            foreach (var file in files)
            {
                try { File.Delete(file); }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private static void CleanupOldBackups(string slot)
        {
            var files = Directory.GetFiles(BackupDirectory, $"{slot}_*.backup")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(MAX_BACKUPS);

            foreach (var file in files)
            {
                try { File.Delete(file); }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private static (SaveMetadata?, SaveData?) LoadLegacySave(string path)
        {
            // Handle loading and migrating old save formats
            try
            {
                string json = File.ReadAllText(path);
                var legacyData = JsonSerializer.Deserialize<SaveData>(json);
                if (legacyData == null) return (null, null);

                // Create metadata from legacy save
                var metadata = new SaveMetadata
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

        private static string GetSavePath(string slot, bool isAutosave) =>
            Path.Combine(
                isAutosave ? AutosaveDirectory : SaveDirectory,
                $"{slot}.save"
            );

        private static string GetBackupPath(string slot, int backupIndex)
        {
            var backups = Directory.GetFiles(BackupDirectory, $"{slot}_*.backup")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            return backupIndex < backups.Count ? backups[backupIndex] : "";
        }
    }

    public class SaveInfo
    {
        public string Slot { get; set; } = "";
        public bool IsAutosave { get; set; }
        public SaveMetadata Metadata { get; set; } = new();
        public string FilePath { get; set; } = "";
    }

    public class SaveMetadata
    {
        public int Version { get; set; }
        public DateTime SaveTime { get; set; }
        public string LastPlayedCharacter { get; set; } = "";
        public TimeSpan TotalPlayTime { get; set; }
        public SaveType SaveType { get; set; }
        public string WorldPath { get; set; } = "";
        public int CharacterLevel { get; set; }
        public Dictionary<string, string> CustomData { get; set; } = new();
    }

    public enum SaveType
    {
        Manual,
        Autosave,
        Quicksave
    }
}
