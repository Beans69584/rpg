using System;
using System.Collections.Generic;
using System.Threading;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Commands.Navigation
{
    public class NavigateCommand : BaseCommand
    {
        public override string Name => "go";
        public override string Description => "Travel to a connected region or location";
        public override string[] Aliases => ["move", "travel"];

        public override void Execute(string args, GameState state)
        {
            if (state.CurrentRegion == null)
            {
                state.GameLog.Add("No world loaded!");
                return;
            }

            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("Where do you want to go?");
                return;
            }

            Location? currentLocation = state.CurrentLocation;
            string targetName = args.Trim().ToLower();

            // First check if target is a location in current region
            if (currentLocation == null)
            {
                foreach (Location location in state.CurrentRegion.Locations)
                {
                    if (state.World?.GetString(location.NameId).ToLower() == targetName)
                    {
                        state.GameLog.Add(new ColoredText($"Moving to {state.World.GetString(location.NameId)}...", ConsoleColor.Yellow));
                        state.NavigateToLocation(location);
                        state.CommandHandler.ExecuteCommand("look", state);
                        return;
                    }
                }
            }

            // If we're in a location, first return to region
            if (currentLocation != null)
            {
                state.CurrentLocation = null;
                if (targetName is "back" or "outside")
                {
                    state.CommandHandler.ExecuteCommand("look", state);
                    return;
                }
            }

            // Get list of connected regions and find the target
            WorldRegion? targetRegion = null;
            List<string> availableRegions = [];

            foreach (int connectionId in state.CurrentRegion.Connections)
            {
                WorldRegion connectedRegion = state.World!.GetWorldData().Regions[connectionId];
                string regionName = state.World.GetString(connectedRegion.NameId).ToLower();
                availableRegions.Add(state.World.GetString(connectedRegion.NameId));

                if (regionName == targetName)
                {
                    targetRegion = connectedRegion;
                    break;
                }
            }

            if (targetRegion == null)
            {
                state.GameLog.Add($"Cannot travel to '{args.Trim()}'");
                state.GameLog.Add("Available destinations:");
                foreach (string name in availableRegions)
                {
                    state.GameLog.Add($" - {name}");
                }
                return;
            }

            // Travel to the target region
            state.GameLog.Add(new ColoredText($"+{new string('-', 38)}+", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText($"| Beginning journey to {state.World!.GetString(targetRegion.NameId)}".PadRight(39) + "|", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText($"+{new string('-', 38)}+", ConsoleColor.Cyan));
            state.GameLog.Add("");

            // Calculate travel time based on distance
            float dx = targetRegion.Position.X - state.CurrentRegion.Position.X;
            float dy = targetRegion.Position.Y - state.CurrentRegion.Position.Y;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            int travelTime = (int)Math.Ceiling(distance);

            SimulateTravelTime(state, travelTime);

            // Arrival
            state.GameLog.Add("");
            state.GameLog.Add(new ColoredText($"=== {state.World.GetString(targetRegion.NameId)} ===", ConsoleColor.Yellow));
            state.GameLog.Add(state.World.GetString(targetRegion.DescriptionId));

            // Show available locations
            state.GameLog.Add("");
            state.GameLog.Add(new ColoredText("Locations:", ConsoleColor.Cyan));
            foreach (Location location in targetRegion.Locations)
            {
                state.GameLog.Add($"  - {state.World.GetString(location.NameId)} ({location.Type})");
            }

            // Show connected regions
            state.GameLog.Add("");
            state.GameLog.Add(new ColoredText("Connected Regions:", ConsoleColor.Cyan));
            foreach (int connectionId in targetRegion.Connections)
            {
                WorldRegion connection = state.World.GetWorldData().Regions[connectionId];
                float connDx = connection.Position.X - targetRegion.Position.X;
                float connDy = connection.Position.Y - targetRegion.Position.Y;
                double connDistance = Math.Sqrt((connDx * connDx) + (connDy * connDy));
                int connTime = (int)Math.Ceiling(connDistance);

                state.GameLog.Add($"  - {state.World.GetString(connection.NameId)} ({FormatTravelTime(connTime)} away)");
            }

            state.CurrentRegion = targetRegion;
        }

        private static void SimulateTravelTime(GameState state, int totalMinutes)
        {
            const int updateInterval = 100;
            const float timeScale = 0.1f;
            const int maxDurationMs = 3000;
            const int borderWidth = 44;
            const int contentWidth = borderWidth - 4;

            float durationMs = Math.Min(totalMinutes * 1000 * timeScale, maxDurationMs);
            float elapsedMs = 0;

            while (elapsedMs < durationMs)
            {
                state.GameLog.Clear();

                // Progress display
                state.GameLog.Add(new ColoredText($"+{new string('-', borderWidth - 2)}+", ConsoleColor.Cyan));
                state.GameLog.Add(new ColoredText($"| {"Journey Status".PadLeft((contentWidth + 13) / 2),-contentWidth} |", ConsoleColor.Cyan));
                state.GameLog.Add(new ColoredText($"+{new string('-', borderWidth - 2)}+", ConsoleColor.Cyan));

                float progress = elapsedMs / durationMs;
                int remainingMinutes = (int)(totalMinutes * (1 - progress));

                string timeStr = $"Time: {FormatTravelTime(remainingMinutes)}";
                state.GameLog.Add(new ColoredText($"| {timeStr,-contentWidth} |", ConsoleColor.Yellow));

                int barLength = contentWidth - 6;
                int progressBars = (int)(progress * barLength);
                string bar = "[" + new string('#', progressBars);
                if (progressBars < barLength)
                    bar += ">";
                bar += new string('-', barLength - progressBars - 1) + "]";
                state.GameLog.Add(new ColoredText($"| {bar,-contentWidth} |", progressBars > barLength / 2 ? ConsoleColor.Green : ConsoleColor.Yellow));

                // Direction indicator cycles through compass points
                string[] directions = ["North ↑", "Northeast ↗", "East →", "Southeast ↘", "South ↓", "Southwest ↙", "West ←", "Northwest ↖"];
                string direction = directions[Environment.TickCount / 500 % 8];
                state.GameLog.Add(new ColoredText($"| {"Direction: " + direction.PadRight(contentWidth - 11)} |", ConsoleColor.Cyan));

                state.GameLog.Add(new ColoredText($"+{new string('-', borderWidth - 2)}+", ConsoleColor.Cyan));

                Thread.Sleep(updateInterval);
                elapsedMs += updateInterval;
            }

            state.GameLog.Clear();
        }

        private static string FormatTravelTime(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} minutes";

            int hours = minutes / 60;
            int mins = minutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
    }
}