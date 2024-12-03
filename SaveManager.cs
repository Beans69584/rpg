using System.Text.Json;

namespace RPG
{
    public static class SaveManager
    {
        private static readonly string SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DemoRPG",
            "Saves"
        );

        static SaveManager()
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        public static void Save(SaveData saveData, string slot)
        {
            string path = GetSavePath(slot);
            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(path, json);
        }

        public static SaveData? Load(string slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SaveData>(json);
        }

        public static List<(string Slot, SaveData Data)> GetSaveFiles()
        {
            var saves = new List<(string, SaveData)>();
            foreach (var file in Directory.GetFiles(SaveDirectory, "*.save"))
            {
                try
                {
                    string slot = Path.GetFileNameWithoutExtension(file);
                    string json = File.ReadAllText(file);
                    var saveData = JsonSerializer.Deserialize<SaveData>(json);
                    if (saveData != null)
                    {
                        saves.Add((slot, saveData));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading save {file}: {ex.Message}");
                }
            }
            return saves;
        }

        private static string GetSavePath(string slot) =>
            Path.Combine(SaveDirectory, $"{slot}.save");
    }
}
