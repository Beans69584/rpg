using System;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Commands.Navigation
{
    public class LeaveCommand : BaseCommand
    {
        public override string Name => "leave";
        public override string Description => "Leave the current building or location";
        public override string[] Aliases => ["exit", "go out"];

        public override void Execute(string args, GameState state)
        {
            Location? currentLocation = state.CurrentLocation;
            Building? currentBuilding = currentLocation?.CurrentBuilding;

            // If in a building, leave it
            if (currentBuilding != null)
            {
                state.GameLog.Add(new ColoredText($"Leaving {state.World?.GetString(currentBuilding.NameId)}...", ConsoleColor.Yellow));
                currentLocation!.CurrentBuilding = null;

                // Show location description
                state.GameLog.Add(new ColoredText($"=== {state.World?.GetString(currentLocation.NameId)} ===", ConsoleColor.Yellow));
                state.GameLog.Add(state.World?.GetLocationDescription(currentLocation) ?? "");

                // Show available buildings
                if (currentLocation.Buildings.Count > 0)
                {
                    state.GameLog.Add("");
                    state.GameLog.Add(new ColoredText("Buildings:", ConsoleColor.Cyan));
                    foreach (Building building in currentLocation.Buildings)
                    {
                        state.GameLog.Add($"  - {state.World?.GetString(building.NameId)} ({building.Type})");
                    }
                }
                return;
            }

            // If in a location, leave it
            if (currentLocation != null)
            {
                state.GameLog.Add(new ColoredText($"Leaving {state.World?.GetString(currentLocation.NameId)}...", ConsoleColor.Yellow));
                state.CurrentLocation = null;

                // Show region info
                state.CommandHandler.ExecuteCommand("look", state);
                return;
            }

            state.GameLog.Add("You're not inside anything to leave from.");
        }
    }
}