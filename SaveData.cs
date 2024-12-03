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
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;
        public DateTime LastPlayTime { get; set; } = DateTime.Now;
        
        [JsonIgnore]
        public string DisplayName => $"{PlayerName} - Level {Level} - {SaveTime:g}";
        
        [JsonIgnore]
        public string Description
        {
            get
            {
                var stats = new List<string>
                {
                    $"HP: {HP}/{MaxHP}",
                    $"Location: {CurrentRegionId}",
                    $"Gold: {Gold}",
                    $"Playtime: {FormatPlayTime(TotalPlayTime)}"
                };
                return string.Join(" | ", stats);
            }
        }

        private static string FormatPlayTime(TimeSpan time)
        {
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{time.Minutes}m {time.Seconds}s";
        }

        public void UpdatePlayTime()
        {
            var now = DateTime.Now;
            var sessionTime = now - LastPlayTime;
            TotalPlayTime += sessionTime;
            LastPlayTime = now;
        }
    }
}
