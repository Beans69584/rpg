using System;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Commands.Navigation
{
    /// <summary>
    /// Represents a command that allows the player to enter buildings or locations in the game world.
    /// </summary>
    public class EnterCommand : BaseCommand
    {
        /// <summary>
        /// Gets the primary name of the command used to enter buildings or locations.
        /// </summary>
        /// <value>The string "enter"</value>
        public override string Name => "enter";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>A string describing the command's purpose</value>
        public override string Description => "Enter a building or location";

        /// <summary>
        /// Gets alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array of strings containing alternative command names</value>
        public override string[] Aliases => ["go in"];

        /// <summary>
        /// Executes the enter command, allowing the player to move into buildings or locations.
        /// </summary>
        /// <param name="args">The name of the building or location to enter</param>
        /// <param name="state">The current game state containing world and player information</param>
        /// <remarks>
        /// The command handles two main scenarios:
        /// - Entering buildings when the player is in a location
        /// - Entering locations when the player is in a region
        /// 
        /// If the target cannot be found, the command lists available buildings or locations.
        /// When entering a building, information about NPCs and items present will be displayed.
        /// </remarks>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("What do you want to enter?");
                return;
            }

            Location? currentLocation = state.CurrentLocation;
            Building? currentBuilding = currentLocation?.CurrentBuilding;

            // Can't enter if already in a building
            if (currentBuilding != null)
            {
                state.GameLog.Add($"You are already inside {state.World?.GetString(currentBuilding.NameId)}");
                return;
            }

            string targetName = args.Trim().ToLower();

            // If in a location, try to enter a building
            if (currentLocation != null)
            {
                foreach (Building building in currentLocation.Buildings)
                {
                    if (state.World?.GetString(building.NameId).ToLower() == targetName)
                    {
                        state.GameLog.Add(new ColoredText($"Entering {state.World.GetString(building.NameId)}...", ConsoleColor.Yellow));
                        currentLocation.CurrentBuilding = building;

                        // Show building info
                        state.GameLog.Add(new ColoredText($"=== {state.World.GetString(building.NameId)} ===", ConsoleColor.Yellow));
                        state.GameLog.Add(state.World.GetString(building.DescriptionId));

                        // Show NPCs in building
                        if (building.NPCs.Count > 0)
                        {
                            state.GameLog.Add("");
                            state.GameLog.Add(new ColoredText("People here:", ConsoleColor.Cyan));
                            foreach (int npcId in building.NPCs)
                            {
                                Entity npc = state.World.GetWorldData().NPCs[npcId];
                                state.GameLog.Add($"  - {state.World.GetString(npc.NameId)} (Level {npc.Level} {npc.Role})");
                            }
                        }

                        // Show items in building
                        if (building.Items.Count > 0)
                        {
                            state.GameLog.Add("");
                            state.GameLog.Add(new ColoredText("Items:", ConsoleColor.Cyan));
                            foreach (int itemId in building.Items)
                            {
                                Item item = state.World.GetWorldData().Items[itemId];
                                state.GameLog.Add($"  - {state.World.GetString(item.NameId)}");
                            }
                        }
                        return;
                    }
                }

                state.GameLog.Add("There is no such building here.");
                state.GameLog.Add("Available buildings:");
                foreach (Building building in currentLocation.Buildings)
                {
                    state.GameLog.Add($"  - {state.World?.GetString(building.NameId)} ({building.Type})");
                }
                return;
            }

            // If in a region, try to enter a location
            if (state.CurrentRegion != null)
            {
                foreach (Location location in state.CurrentRegion.Locations)
                {
                    if (state.World?.GetString(location.NameId).ToLower() == targetName)
                    {
                        state.GameLog.Add(new ColoredText($"Entering {state.World.GetString(location.NameId)}...", ConsoleColor.Yellow));
                        state.NavigateToLocation(location);
                        return;
                    }
                }

                state.GameLog.Add("There is no such location here.");
                state.GameLog.Add("Available locations:");
                foreach (Location location in state.CurrentRegion.Locations)
                {
                    state.GameLog.Add($"  - {state.World?.GetString(location.NameId)} ({location.Type})");
                }
            }
        }
    }
}