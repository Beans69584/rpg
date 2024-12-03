using NLua;

namespace RPG.Commands
{
    public class LuaGameAPI
    {
        private const float DEFAULT_TIME_SCALE = 0.1f;
        private const int MAX_TRAVEL_DURATION_MS = 3000; // 3 seconds max
        private readonly GameState _state;
        private readonly Lua _lua;

        public LuaGameAPI(GameState state, Lua lua)
        {
            _state = state;
            _lua = lua;
        }

        // Game log methods
        public void Log(string message) => _state.GameLog.Add(message);
        public void LogColor(string message, string color)
        {
            // TODO: Implement colored text in game log
            _state.GameLog.Add(message);
        }
        public void ClearLog() => _state.GameLog.Clear();

        // Player state methods
        public string GetPlayerName() => _state.PlayerName;
        public void SetPlayerName(string name) => _state.PlayerName = name;
        public int GetPlayerHP() => _state.HP;
        public void SetPlayerHP(int hp) => _state.HP = Math.Clamp(hp, 0, _state.MaxHP);
        public int GetPlayerMaxHP() => _state.MaxHP;
        public void SetPlayerMaxHP(int maxHp) => _state.MaxHP = Math.Max(1, maxHp);
        public int GetPlayerLevel() => _state.Level;
        public void SetPlayerLevel(int level) => _state.Level = Math.Max(1, level);

        // Utility methods
        public void Sleep(int milliseconds) => Thread.Sleep(milliseconds);
        public string AskQuestion(string question)
        {
            Log(question);
            // TODO: Implement proper input handling
            return "";
        }
        // Combat helper methods
        public bool RollDice(int sides) => Random.Shared.Next(1, sides + 1) == sides;
        public int GetRandomNumber(int min, int max) => Random.Shared.Next(min, max + 1);

        // Region helper methods
        public string GetRegionName(WorldRegion region) =>
            _state.World?.GetString(region.NameId) ?? "<invalid region>";

        public string GetRegionDescription(WorldRegion region) =>
            _state.World?.GetString(region.DescriptionId) ?? "<invalid region>";

        public LuaTable GetConnectedRegions(WorldRegion region)
        {
            var table = _lua.DoString("return {}")[0] as LuaTable;
            if (_state.World == null || table == null) 
                return table;

            var regions = _state.World.GetConnectedRegions(region).ToList();
            for (int i = 0; i < regions.Count; i++)
            {
                table[i + 1] = regions[i];
            }
            return table;
        }

        // Updated region helper methods
        public RegionWrapper GetCurrentRegion()
        {
            if (_state?.World == null || _state.CurrentRegion == null)
                return null;
            return new RegionWrapper(_state.CurrentRegion, _state.World);
        }

        public void SetCurrentRegion(RegionWrapper region)
        {
            if (region?.Region != null)
                _state.CurrentRegion = region.Region;
        }

        public LuaTable GetConnectedRegions()
        {
            if (_state?.World == null || _state.CurrentRegion == null)
                return CreateEmptyTable();

            var regions = _state.World.GetConnectedRegions(_state.CurrentRegion)
                .Select(r => new RegionWrapper(r, _state.World));

            return regions.ToLuaTable(_lua);
        }

        private LuaTable CreateEmptyTable()
        {
            return _lua.DoString("return {}")[0] as LuaTable ?? 
                   throw new InvalidOperationException("Failed to create Lua table");
        }

        // Helper method to check if a region name matches
        public bool RegionNameMatches(RegionWrapper region, string name)
        {
            return region.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        // Location methods
        public LocationWrapper GetCurrentLocation()
        {
            if (_state?.World == null || _state.CurrentLocation == null)
                return null;
            return new LocationWrapper(_state.CurrentLocation, _state.World);
        }

        public void SetCurrentLocation(LocationWrapper location)
        {
            _state.CurrentLocation = location?.Location;
        }

        public LuaTable GetLocationsInRegion()
        {
            if (_state?.World == null || _state.CurrentRegion == null)
                return CreateEmptyTable();

            var locations = _state.World.GetLocationsInRegion(_state.CurrentRegion)
                .Select(l => new LocationWrapper(l, _state.World));

            return locations.ToLuaTable(_lua);
        }

        public bool LocationNameMatches(LocationWrapper location, string name)
        {
            return location.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        // Location navigation
        public void NavigateToLocation(LocationWrapper location)
        {
            if (location?.Location != null)
                _state.NavigateToLocation(location.Location);
        }

        // Region position and travel methods
        public double GetDistanceBetweenRegions(RegionWrapper from, RegionWrapper to)
        {
            if (from?.Region == null || to?.Region == null) return 0;
            
            var dx = to.Region.Position.X - from.Region.Position.X;
            var dy = to.Region.Position.Y - from.Region.Position.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Vector2 GetRegionPosition(RegionWrapper region)
        {
            return region?.Region?.Position ?? new Vector2();
        }

        // Travel time calculation (assumes 1 unit = 1 minute of travel time)
        public int CalculateTravelTime(RegionWrapper from, RegionWrapper to)
        {
            var distance = GetDistanceBetweenRegions(from, to);
            return (int)Math.Ceiling(distance);
        }

        // Simulate passing of time
        public void SimulateTravelTime(int minutes)
        {
            // This is where you would add any time-based game mechanics
            Thread.Sleep(Math.Min(minutes * 100, 2000)); // Cap at 2 seconds real time
        }

        public void SimulateTravelTimeWithProgress(int totalMinutes, float timeScale = DEFAULT_TIME_SCALE)
        {
            const int updateInterval = 100; // Update progress every 100ms
            float durationMs = Math.Min(totalMinutes * 1000 * timeScale, MAX_TRAVEL_DURATION_MS);
            float elapsedMs = 0;
            
            while (elapsedMs < durationMs)
            {
                ClearLog();
                Log($"=== Traveling ===");
                
                float progress = elapsedMs / durationMs;
                int remainingMinutes = (int)(totalMinutes * (1 - progress));
                
                Log($"Time remaining: {FormatTravelTime(remainingMinutes)}");
                
                // Create progress bar
                int barLength = 20;
                int progressBars = (int)(progress * barLength);
                string progressBar = "[" + new string('=', progressBars) + new string(' ', barLength - progressBars) + "]";
                Log(progressBar);
                
                Thread.Sleep(updateInterval);
                elapsedMs += updateInterval;
            }
            ClearLog();
        }

        private string FormatTravelTime(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} minutes";
            
            int hours = minutes / 60;
            int mins = minutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
    }
}
