using System.Text.Json.Serialization;

namespace RPG
{
    public class SaveData
    {
        public string PlayerName { get; set; } = "Hero";
        public int Level { get; set; } = 1;
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public string CurrentRegionId { get; set; } = "";
        public DateTime SaveTime { get; set; }
        public string WorldPath { get; set; } = "";
        public int Gold { get; set; } = 100;
        public Dictionary<string, int> Stats { get; set; } = new();
        public List<string> Inventory { get; set; } = new();
        public Dictionary<string, bool> GameFlags { get; set; } = new();
        
        [JsonIgnore]
        public string DisplayName => $"{PlayerName} - Level {Level} - {SaveTime:g}";
        
        [JsonIgnore]
        public string Description => 
            $"HP: {HP}/{MaxHP} | Location: {CurrentRegionId} | Gold: {Gold}";
    }
}
