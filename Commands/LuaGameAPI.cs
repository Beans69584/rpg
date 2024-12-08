using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLua;

namespace RPG.Commands
{
    /// <summary>
    /// Represents a Lua game API that abstracts game state and provides helper methods for Lua scripts.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="LuaGameApi"/> class.
    /// </remarks>
    /// <param name="state">The current game state.</param>
    /// <param name="lua">The Lua interpreter to use.</param>
    public class LuaGameApi(GameState state, Lua lua)
    {
        private const float DEFAULT_TIME_SCALE = 0.1f;
        private const int MAX_TRAVEL_DURATION_MS = 3000; // 3 seconds max
        private readonly GameState _state = state;
        private readonly Lua _lua = lua;

        /// <summary>
        /// Logs a message to the game log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            _state.GameLog.Add(new ColoredText(message));
        }

        /// <summary>
        /// Logs a message to the game log with a specified color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color name (e.g., "red", "blue", "green", etc.)</param>
        public void LogColor(string message, string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                Log(message);
                return;
            }

            if (Enum.TryParse<ConsoleColor>(color, true, out ConsoleColor consoleColor))
            {
                _state.GameLog.Add(new ColoredText(message, consoleColor));
            }
            else
            {
                // If color is invalid, log with default color
                Log(message);
            }
        }

        /// <summary>
        /// Clears the game log.
        /// </summary>
        public void ClearLog()
        {
            _state.GameLog.Clear();
        }

        /// <summary>
        /// Gets the player's name.
        /// </summary>
        public string GetPlayerName()
        {
            return _state.PlayerName;
        }

        /// <summary>
        /// Sets the player's name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            _state.PlayerName = name;
        }

        /// <summary>
        /// Gets the player's health points.
        /// </summary>
        public int GetPlayerHP()
        {
            return _state.HP;
        }

        /// <summary>
        /// Sets the player's health points.
        /// </summary>
        public void SetPlayerHP(int hp)
        {
            _state.HP = Math.Clamp(hp, 0, _state.MaxHP);
        }

        /// <summary>
        /// Gets the player's maximum health points.
        /// </summary>
        /// <returns>The player's maximum health points.</returns>
        public int GetPlayerMaxHP()
        {
            return _state.MaxHP;
        }

        /// <summary>
        /// Sets the player's maximum health points.
        /// </summary>
        /// <param name="maxHp">The maximum health points to set.</param>
        public void SetPlayerMaxHP(int maxHp)
        {
            _state.MaxHP = Math.Max(1, maxHp);
        }

        /// <summary>
        /// Gets the player's level.
        /// </summary>
        /// <returns>The player's level.</returns>
        public int GetPlayerLevel()
        {
            return _state.Level;
        }

        /// <summary>
        /// Sets the player's level.
        /// </summary>
        /// <param name="level">The level to set.</param>
        public void SetPlayerLevel(int level)
        {
            _state.Level = Math.Max(1, level);
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds to sleep.</param>
        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        /// <summary>
        /// Asks the player a question and waits for their response.
        /// </summary>
        /// <param name="question">The question to ask the player.</param>
        /// <returns>The player's answer.</returns>
        public string AskQuestion(string question)
        {
            lock (_state.GameLog)
            {
                // Combine the question with the prompt on one line
                LogColor(question + " > ", "Yellow");
            }

            StringBuilder inputBuffer = new();
            _state.WindowManager.UpdateInputText("", ConsoleColor.Cyan);

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            string answer = inputBuffer.ToString().Trim();
                            // Log the answer on the same line as the question
                            _state.GameLog.RemoveAt(_state.GameLog.Count - 1);
                            LogColor($"{question} > {answer}", "Yellow");
                            Log("");

                            // Execute command directly
                            _state.CommandHandler.ExecuteCommand(answer, _state);

                            return answer;

                        case ConsoleKey.Backspace:
                            if (inputBuffer.Length > 0)
                            {
                                inputBuffer.Length--;
                                _state.WindowManager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.Cyan);
                            }
                            break;

                        case ConsoleKey.Escape:
                            return "";

                        default:
                            if (!char.IsControl(key.KeyChar) && inputBuffer.Length < 100)
                            {
                                inputBuffer.Append(key.KeyChar);
                                _state.WindowManager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.Cyan);
                            }
                            break;
                    }

                    _state.WindowManager.QueueRender();
                }

                Thread.Sleep(16); // Match the refresh rate used in Program.cs
            }
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="sides">The number of sides on the dice.</param>
        /// <returns>True if the dice roll is successful, false otherwise.</returns>
        public bool RollDice(int sides)
        {
            return Random.Shared.Next(1, sides + 1) == sides;
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="min">The minimum value of the random number.</param>
        /// <param name="max">The maximum value of the random number.</param>
        /// <returns>A random number between the specified range.</returns>
        public int GetRandomNumber(int min, int max)
        {
            return Random.Shared.Next(min, max + 1);
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="region">The region to get the name of.</param>
        /// <returns>The name of the region.</returns>
        public string GetRegionName(WorldRegion region)
        {
            return _state.World?.GetString(region.NameId) ?? "<invalid region>";
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="region">The region to get the description of.</param>
        /// <returns>The description of the region.</returns>
        public string GetRegionDescription(WorldRegion region)
        {
            return _state.World?.GetString(region.DescriptionId) ?? "<invalid region>";
        }

        /// <summary>
        /// Checks if a region name matches.
        /// </summary>
        /// <returns>True if the names match, false otherwise.</returns>
        public RegionWrapper? GetCurrentRegion()
        {
            return _state?.World == null || _state.CurrentRegion == null ? null : new RegionWrapper(_state.CurrentRegion, _state.World);
        }

        /// <summary>
        /// Sets the current region.
        /// </summary>
        /// <param name="region">The region to set.</param>
        public void SetCurrentRegion(RegionWrapper region)
        {
            if (region?.Region != null)
                _state.CurrentRegion = region.Region;
        }

        /// <summary>
        /// Gets the connected regions to the current region.
        /// </summary>
        /// <returns>A table of connected regions.</returns>
        public LuaTable GetConnectedRegions()
        {
            if (_state?.World == null || _state.CurrentRegion == null)
                return CreateEmptyTable();

            IEnumerable<RegionWrapper> regions = _state.World.GetConnectedRegions(_state.CurrentRegion)
                .Select(r => new RegionWrapper(r, _state.World));

            return regions.ToLuaTable(_lua) ?? CreateEmptyTable();
        }

        /// <summary>
        /// Gets the connected regions to a specified region.
        /// </summary>
        /// <param name="region">The region to get connected regions for.</param>
        /// <returns>A table of connected regions.</returns>
        public LuaTable? GetConnectedRegions(WorldRegion region)
        {
            LuaTable? table = _lua.DoString("return {}")[0] as LuaTable;
            if (_state.World == null || table == null)
                return table;

            List<WorldRegion> regions = [.. _state.World.GetConnectedRegions(region)];
            for (int i = 0; i < regions.Count; i++)
            {
                table[i + 1] = regions[i];
            }
            return table;
        }

        private LuaTable CreateEmptyTable()
        {
            return _lua.DoString("return {}")[0] as LuaTable ??
                   throw new InvalidOperationException("Failed to create Lua table");
        }

        /// <summary>
        /// Checks if a region name matches.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <param name="name">The name to check against.</param>
        /// <returns>True if the names match, false otherwise.</returns>
        public bool RegionNameMatches(RegionWrapper region, string name)
        {
            return region.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Navigates to a region.
        /// </summary>
        /// <returns>The region to navigate to.</returns>
        public LocationWrapper? GetCurrentLocation()
        {
            return _state?.World == null || _state.CurrentLocation == null ? null : new LocationWrapper(_state.CurrentLocation, _state.World);
        }

        /// <summary>
        /// Sets the current location.
        /// </summary>
        /// <param name="location">The location to set.</param>
        public void SetCurrentLocation(LocationWrapper location)
        {
            _state.CurrentLocation = location?.Location;
        }

        /// <summary>
        /// Gets the locations in the current region.
        /// </summary>
        /// <returns>A table of locations in the current region.</returns>
        public LuaTable GetLocationsInRegion()
        {
            if (_state?.World == null || _state.CurrentRegion == null)
                return CreateEmptyTable();

            IEnumerable<LocationWrapper> locations = WorldLoader.GetLocationsInRegion(_state.CurrentRegion)
                .Select(l => new LocationWrapper(l, _state.World));

            return locations.ToLuaTable(_lua) ?? CreateEmptyTable();
        }

        /// <summary>
        /// Checks if a location name matches.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <param name="name">The name to check against.</param>
        /// <returns>True if the names match, false otherwise.</returns>
        public bool LocationNameMatches(LocationWrapper location, string name)
        {
            return location.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Navigates to a region.
        /// </summary>
        /// <param name="location">The location to navigate to.</param>
        public void NavigateToLocation(LocationWrapper location)
        {
            if (location?.Location != null)
                _state.NavigateToLocation(location.Location);
        }

        /// <summary>
        /// Gets the distance between two regions.
        /// </summary>
        /// <param name="from">The starting region.</param>
        /// <param name="to">The destination region.</param>
        /// <returns>The distance between the regions.</returns>
        public double GetDistanceBetweenRegions(RegionWrapper from, RegionWrapper to)
        {
            if (from?.Region == null || to?.Region == null) return 0;

            float dx = to.Region.Position.X - from.Region.Position.X;
            float dy = to.Region.Position.Y - from.Region.Position.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Gets the position of a region.
        /// </summary>
        /// <param name="region">The region to get the position of.</param>
        /// <returns>The position of the region.</returns>
        public Vector2 GetRegionPosition(RegionWrapper region)
        {
            return region?.Region?.Position ?? new Vector2();
        }

        /// <summary>
        /// Calculates the travel time between two regions in minutes.
        /// </summary>
        /// <param name="from">The starting region.</param>
        /// <param name="to">The destination region.</param>
        /// <returns>The travel time in minutes.</returns>
        public int CalculateTravelTime(RegionWrapper from, RegionWrapper to)
        {
            double distance = GetDistanceBetweenRegions(from, to);
            return (int)Math.Ceiling(distance);
        }

        /// <summary>
        /// Simulates travel time in minutes.
        /// </summary>
        /// <param name="minutes"></param>
        public void SimulateTravelTime(int minutes)
        {
            // This is where you would add any time-based game mechanics
            Thread.Sleep(Math.Min(minutes * 100, 2000)); // Cap at 2 seconds real time
        }

        /// <summary>
        /// Simulates travel time with a progress bar.
        /// </summary>
        /// <param name="totalMinutes">The total travel time in minutes.</param>
        /// <param name="timeScale">The time scale to use (default is 0.1).</param>
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
