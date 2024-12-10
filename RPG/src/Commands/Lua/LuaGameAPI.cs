using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLua;

using RPG.Core;
using RPG.Common;
using RPG.World;
using RPG.World.Data;
using RPG.World.Generation;

namespace RPG.Commands.Lua
{
    /// <summary>
    /// Represents a Lua game API that abstracts game state and provides helper methods for Lua scripts.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="LuaGameApi"/> class.
    /// </remarks>
    /// <param name="state">The current game state.</param>
    /// <param name="lua">The Lua interpreter to use.</param>
    public class LuaGameApi(GameState state, NLua.Lua lua)
    {
        private const float DEFAULT_TIME_SCALE = 0.1f;
        private const int MAX_TRAVEL_DURATION_MS = 3000; // 3 seconds max
        private readonly GameState _state = state;
        private readonly NLua.Lua _lua = lua;

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
        /// Gets the player's gold amount.
        /// </summary>
        public int GetPlayerGold()
        {
            return _state.Gold;
        }

        /// <summary>
        /// Modifies the player's gold amount.
        /// </summary>
        public void ModifyPlayerGold(int amount)
        {
            _state.Gold = Math.Max(0, _state.Gold + amount);
        }

        /// <summary>
        /// Gets whether the player has enough gold.
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return _state.Gold >= amount;
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

        /// <summary>
        /// Gets a list of NPCs in the current location.
        /// </summary>
        public LuaTable GetNPCsInLocation()
        {
            if (_state?.World == null || _state.CurrentLocation == null)
                return CreateEmptyTable();

            List<Entity> npcs = _state.World.GetNPCsInLocation(_state.CurrentLocation);
            LuaTable table = CreateEmptyTable();
            for (int i = 0; i < npcs.Count; i++)
            {
                Entity npc = npcs[i];
                table[i + 1] = new Dictionary<string, object>
                {
                    ["name"] = _state.World.GetEntityName(npc),
                    ["level"] = npc.Level,
                    ["hp"] = npc.HP
                };
            }
            return table;
        }

        /// <summary>
        /// Gets a list of items in the current location.
        /// </summary>
        public LuaTable GetItemsInLocation()
        {
            if (_state?.World == null || _state.CurrentLocation == null)
                return CreateEmptyTable();

            List<Item> items = _state.World.GetItemsInLocation(_state.CurrentLocation);
            LuaTable table = CreateEmptyTable();
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                table[i + 1] = new Dictionary<string, object>
                {
                    ["name"] = _state.World.GetItemName(item),
                    ["description"] = _state.World.GetItemDescription(item)
                };
            }
            return table;
        }

        /// <summary>
        /// Gets random dialogue from an NPC.
        /// </summary>
        public string GetRandomNPCDialogue(Dictionary<string, object> npc)
        {
            if (_state?.World == null || npc == null)
                return "...";

            // Find the NPC in the current location
            List<Entity> npcs = _state.CurrentLocation == null ? [] : _state.World.GetNPCsInLocation(_state.CurrentLocation);
            Entity? targetNpc = npcs.FirstOrDefault(n => _state.World.GetEntityName(n) == npc["name"]?.ToString());

            return targetNpc != null ? _state.World.GetNPCDialogue(targetNpc) : "...";
        }

        /// <summary>
        /// Gets the route description between two regions.
        /// </summary>
        public LuaTable GetRoute(RegionWrapper from, RegionWrapper to)
        {
            if (_state?.World == null || from?.Region == null || to?.Region == null)
                return CreateEmptyTable();

            List<RoutePoint> route = _state.World.GetRoute(from.Region, to.Region);
            LuaTable table = CreateEmptyTable();

            for (int i = 0; i < route.Count; i++)
            {
                RoutePoint point = route[i];
                table[i + 1] = new Dictionary<string, object>
                {
                    ["description"] = _state.World.GetRouteDescription(point),
                    ["directions"] = _state.World.GetRouteDirections(point)
                };
            }

            return table;
        }

        /// <summary>
        /// Gets the buildings in the current location if it's a settlement.
        /// </summary>
        public LuaTable GetBuildings()
        {
            if (_state?.World == null || _state.CurrentLocation?.Buildings == null)
                return CreateEmptyTable();

            LuaTable table = CreateEmptyTable();
            for (int i = 0; i < _state.CurrentLocation.Buildings.Count; i++)
            {
                Building building = _state.CurrentLocation.Buildings[i];
                table[i + 1] = new Dictionary<string, object>
                {
                    ["name"] = _state.World.GetString(building.NameId),
                    ["description"] = _state.World.GetString(building.DescriptionId),
                    ["type"] = building.Type
                };
            }
            return table;
        }

        /// <summary>
        /// Gets the landmarks along a route point.
        /// </summary>
        public LuaTable GetLandmarksAtPoint(Dictionary<string, object> routePoint)
        {
            if (_state?.World == null || routePoint == null)
                return CreateEmptyTable();

            // Find the actual route point from current route
            List<RoutePoint>? currentRoute = _state.CurrentRegion == null ? null :
                _state.World.GetRoute(_state.CurrentRegion, _state.CurrentRegion);
            RoutePoint? point = currentRoute?.FirstOrDefault(p =>
                _state.World.GetRouteDescription(p) == routePoint["description"]?.ToString());

            if (point == null)
                return CreateEmptyTable();

            IEnumerable<Landmark> landmarks = _state.World.GetRouteLandmarks(point);
            LuaTable table = CreateEmptyTable();

            int index = 1;
            foreach (Landmark landmark in landmarks)
            {
                table[index++] = new Dictionary<string, object>
                {
                    ["name"] = _state.World.GetString(landmark.NameId),
                    ["description"] = _state.World.GetString(landmark.DescriptionId),
                    ["type"] = landmark.Type
                };
            }

            return table;
        }

        /// <summary>
        /// Gets the NPCs in a specific building.
        /// </summary>
        public LuaTable GetNPCsInBuilding(Dictionary<string, object> building)
        {
            if (_state?.World == null || building == null)
                return CreateEmptyTable();

            // Find the building in the current location
            Building? currentBuilding = _state.CurrentLocation?.Buildings
                .FirstOrDefault(b => _state.World.GetString(b.NameId).Equals(
                    building["name"]?.ToString(),
                    StringComparison.OrdinalIgnoreCase));

            if (currentBuilding == null)
            {
                Log($"Debug: Building not found: {building["name"]}");
                return CreateEmptyTable();
            }

            LuaTable table = CreateEmptyTable();
            int index = 1;
            var worldData = _state.World.GetWorldData();

            foreach (int npcId in currentBuilding.NPCs)
            {
                // Skip invalid IDs
                if (npcId < 0 || npcId >= worldData.NPCs.Count)
                {
                    Log($"Warning: Skipping invalid NPC ID: {npcId}");
                    continue;
                }

                Entity npc = worldData.NPCs[npcId];
                string name = _state.World.GetEntityName(npc);

                table[index++] = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["level"] = npc.Level,
                    ["hp"] = npc.HP
                };
            }
            return table;
        }

        /// <summary>
        /// Gets the items in a specific building.
        /// </summary>
        public LuaTable GetItemsInBuilding(Dictionary<string, object> building)
        {
            if (_state?.World == null || building == null)
                return CreateEmptyTable();

            // Find the building in the current location
            Building? currentBuilding = _state.CurrentLocation?.Buildings
                .FirstOrDefault(b => _state.World.GetString(b.NameId) == building["name"]?.ToString());

            if (currentBuilding == null)
                return CreateEmptyTable();

            LuaTable table = CreateEmptyTable();
            int index = 1;
            var worldData = _state.World.GetWorldData();

            foreach (int itemId in currentBuilding.Items)
            {
                // Add bounds checking
                if (itemId >= 0 && itemId < worldData.Items.Count)
                {
                    Item item = worldData.Items[itemId];
                    table[index++] = new Dictionary<string, object>
                    {
                        ["name"] = _state.World.GetItemName(item),
                        ["description"] = _state.World.GetItemDescription(item)
                    };
                }
            }

            return table;
        }

        /// <summary>
        /// Gets the current building if the player is in one.
        /// </summary>
        public Dictionary<string, object>? GetCurrentBuilding()
        {
            if (_state?.World == null || _state.CurrentLocation?.CurrentBuilding == null)
                return null;

            var building = _state.CurrentLocation.CurrentBuilding;
            return new Dictionary<string, object>
            {
                ["name"] = _state.World.GetString(building.NameId),
                ["description"] = _state.World.GetString(building.DescriptionId),
                ["type"] = building.Type
            };
        }

        /// <summary>
        /// Sets the current building.
        /// </summary>
        public void SetCurrentBuilding(Dictionary<string, object>? building)
        {
            if (_state?.CurrentLocation == null)
                return;

            if (building == null)
            {
                _state.CurrentLocation.CurrentBuilding = null;
                return;
            }

            var targetBuilding = _state.CurrentLocation.Buildings.FirstOrDefault(b =>
                _state.World?.GetString(b.NameId) == building["name"]?.ToString());
            _state.CurrentLocation.CurrentBuilding = targetBuilding;
        }

        /// <summary>
        /// Takes gold from the player.
        /// </summary>
        /// <param name="amount">The amount of gold to take.</param>
        public void TakeGold(int amount)
        {
            _state.Gold = Math.Max(0, _state.Gold - amount);
        }
    }
}