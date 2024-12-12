using System;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Commands.Navigation
{
    /// <summary>
    /// Represents a command that allows the player to exit their current building or location.
    /// </summary>
    public class LeaveCommand : BaseCommand
    {
        /// <summary>
        /// Gets the primary name of the command used to invoke it.
        /// </summary>
        /// <value>The string "leave".</value>
        public override string Name => "leave";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>A description explaining the command's purpose.</value>
        public override string Description => "Leave the current building or location";

        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array of strings containing "exit" and "go out".</value>
        public override string[] Aliases => ["exit", "go out"];

        /// <summary>
        /// Executes the leave command, allowing the player to exit their current building or location.
        /// If in a building, returns to the containing location. If in a location, returns to the region view.
        /// </summary>
        /// <param name="args">Additional command arguments (not used).</param>
        /// <param name="state">The current game state containing player location and world information.</param>
        /// <remarks>
        /// The command will display appropriate messages depending on whether the player leaves a building or location.
        /// If the player is not inside anything that can be left, an appropriate message is shown.
        /// </remarks>
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