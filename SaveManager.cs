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
            Environment.OSVersion.Platform == PlatformID.Unix ||
            Environment.OSVersion.Platform == PlatformID.MacOSX
                ? Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local/share")
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.OSVersion.Platform == PlatformID.Unix
                ? "demorpg/saves"
                : Environment.OSVersion.Platform == PlatformID.MacOSX
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
            SaveMetadata metadata = new SaveMetadata
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
            using FileStream fs = File.Create(path);
            using GZipStream gz = new GZipStream(fs, CompressionLevel.Optimal);
            using BinaryWriter writer = new BinaryWriter(gz);

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

        public static (SaveMetadata? Metadata, SaveData? Data) Load(string slot, bool isAutosave = false)
        {
            string path = GetSavePath(slot, isAutosave);
            if (!File.Exists(path)) return (null, null);

            try
            {
                using FileStream fs = File.OpenRead(path);
                using GZipStream gz = new GZipStream(fs, CompressionMode.Decompress);
                using BinaryReader reader = new BinaryReader(gz);

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

        public static List<SaveInfo> GetSaveFiles(bool includeAutosaves = true)
        {
            List<SaveInfo> saves = new List<SaveInfo>();

            // Get manual saves
            foreach (string file in Directory.GetFiles(SaveDirectory, "*.save"))
            {
                SaveInfo? info = GetSaveInfo(file, false);
                if (info != null) saves.Add(info);
            }

            // Get autosaves if requested
            if (includeAutosaves)
            {
                foreach (string file in Directory.GetFiles(AutosaveDirectory, "*.save"))
                {
                    SaveInfo? info = GetSaveInfo(file, true);
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
                (SaveMetadata metadata, SaveData _) = Load(Path.GetFileNameWithoutExtension(path), isAutosave);
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
            IEnumerable<string> files = Directory.GetFiles(AutosaveDirectory, "*.save")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(MAX_AUTOSAVES);

            foreach (string? file in files)
            {
                try { File.Delete(file); }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private static void CleanupOldBackups(string slot)
        {
            IEnumerable<string> files = Directory.GetFiles(BackupDirectory, $"{slot}_*.backup")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(MAX_BACKUPS);

            foreach (string? file in files)
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
                SaveData? legacyData = JsonSerializer.Deserialize<SaveData>(json);
                if (legacyData == null) return (null, null);

                // Create metadata from legacy save
                SaveMetadata metadata = new SaveMetadata
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
            List<string> backups = Directory.GetFiles(BackupDirectory, $"{slot}_*.backup")
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

        // Add this method to support deconstruction
        public void Deconstruct(out string slot, out SaveData data)
        {
            slot = Slot;
            // Load the actual save data when deconstructing
            (SaveMetadata _, SaveData saveData) = SaveManager.Load(Slot, IsAutosave);
            data = saveData ?? new SaveData();
        }
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
