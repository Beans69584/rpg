using System;
using RPG.Core;
using RPG.World.Data;

namespace RPG.Commands.Interaction
{
    public class LookCommand : BaseCommand
    {
        public override string Name => "look";
        public override string Description => "Examine your surroundings or a specific object";
        public override string[] Aliases => [];

        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                if (state.CurrentLocation?.CurrentBuilding != null)
                {
                    ShowBuildingInfo(state);
                }
                else if (state.CurrentLocation != null)
                {
                    ShowLocationInfo(state);
                }
                else if (state.CurrentRegion != null)
                {
                    ShowRegionInfo(state);
                }
                else
                {
                    state.GameLog.Add(new ColoredText("There's nothing interesting to see here.", ConsoleColor.Gray));
                }
                return;
            }

            // Looking at a specific building
            if (state.CurrentLocation != null)
            {
                string targetName = args.Trim().ToLower();
                foreach (Building building in state.CurrentLocation.Buildings)
                {
                    if (state.World?.GetString(building.NameId).ToLower() == targetName)
                    {
                        ShowSpecificBuildingInfo(state, building);
                        return;
                    }
                }
            }

            state.GameLog.Add(new ColoredText("You don't see that here.", ConsoleColor.Red));
        }

        private static void ShowRegionInfo(GameState state)
        {
            state.GameLog.Add(new ColoredText($"Region: {state.World?.GetString(state.CurrentRegion!.NameId)}", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText(state.World?.GetString(state.CurrentRegion!.DescriptionId) ?? "", ConsoleColor.White));
            state.GameLog.Add("");

            // Show terrain info
            state.GameLog.Add(new ColoredText($"Terrain: {state.CurrentRegion!.Terrain}", ConsoleColor.Yellow));
            state.GameLog.Add("");

            // Show locations
            if (state.CurrentRegion.Locations.Count > 0)
            {
                state.GameLog.Add(new ColoredText("Locations:", ConsoleColor.Yellow));
                foreach (Location location in state.CurrentRegion.Locations)
                {
                    state.GameLog.Add(new ColoredText($"- {state.World?.GetString(location.NameId)} ({location.Type})", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }

            // Show NPCs in region
            if (state.CurrentRegion.NPCs.Count > 0)
            {
                state.GameLog.Add(new ColoredText("People in the area:", ConsoleColor.Yellow));
                foreach (int npcId in state.CurrentRegion.NPCs)
                {
                    Entity npc = state.World!.GetWorldData().NPCs[npcId];
                    state.GameLog.Add(new ColoredText($"- {state.World.GetString(npc.NameId)} (Level {npc.Level})", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }

            // Show connected regions
            state.GameLog.Add(new ColoredText("Connected regions:", ConsoleColor.Yellow));
            foreach (int connectionId in state.CurrentRegion.Connections)
            {
                WorldRegion connectedRegion = state.World!.GetWorldData().Regions[connectionId];
                state.GameLog.Add(new ColoredText($"- {state.World.GetString(connectedRegion.NameId)}", ConsoleColor.Gray));
            }
            state.GameLog.Add("");
        }

        private static void ShowLocationInfo(GameState state)
        {
            Location location = state.CurrentLocation!;
            state.GameLog.Add(new ColoredText($"Location: {state.World?.GetString(location.NameId)}", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText(state.World?.GetLocationDescription(location) ?? "", ConsoleColor.White));
            state.GameLog.Add("");

            // Show NPCs
            if (location.NPCs.Count > 0)
            {
                state.GameLog.Add(new ColoredText("People here:", ConsoleColor.Yellow));
                foreach (int npcId in location.NPCs)
                {
                    Entity npc = state.World!.GetWorldData().NPCs[npcId];
                    state.GameLog.Add(new ColoredText($"- {state.World.GetString(npc.NameId)} (Level {npc.Level} {npc.Role})", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }

            // Show buildings
            if (location.Buildings.Count > 0)
            {
                state.GameLog.Add(new ColoredText("Buildings:", ConsoleColor.Yellow));
                foreach (Building building in location.Buildings)
                {
                    state.GameLog.Add(new ColoredText($"- {state.World?.GetString(building.NameId)} ({building.Type})", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }
        }

        private static void ShowBuildingInfo(GameState state)
        {
            Building building = state.CurrentLocation!.CurrentBuilding!;
            state.GameLog.Add(new ColoredText($"Building: {state.World?.GetString(building.NameId)}", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText(state.World?.GetString(building.DescriptionId) ?? "", ConsoleColor.White));
            state.GameLog.Add("");

            // Show NPCs in building
            if (building.NPCs.Count > 0)
            {
                state.GameLog.Add(new ColoredText("People here:", ConsoleColor.Yellow));
                foreach (int npcId in building.NPCs)
                {
                    Entity npc = state.World!.GetWorldData().NPCs[npcId];
                    state.GameLog.Add(new ColoredText($"- {state.World.GetString(npc.NameId)} (Level {npc.Level})", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }

            // Show items in building
            if (building.Items.Count > 0)
            {
                state.GameLog.Add(new ColoredText("Items:", ConsoleColor.Yellow));
                foreach (int itemId in building.Items)
                {
                    Item item = state.World!.GetWorldData().Items[itemId];
                    state.GameLog.Add(new ColoredText($"- {state.World.GetString(item.NameId)}", ConsoleColor.Gray));
                }
                state.GameLog.Add("");
            }
        }

        private static void ShowSpecificBuildingInfo(GameState state, Building building)
        {
            state.GameLog.Add(new ColoredText($"Building: {state.World?.GetString(building.NameId)}", ConsoleColor.Cyan));
            state.GameLog.Add(new ColoredText(state.World?.GetString(building.DescriptionId) ?? "", ConsoleColor.White));
            state.GameLog.Add("");

            if (building.NPCs.Count > 0)
            {
                state.GameLog.Add(new ColoredText("People here:", ConsoleColor.Yellow));
                foreach (int npcId in building.NPCs)
                {
                    Entity npc = state.World!.GetWorldData().NPCs[npcId];
                    state.GameLog.Add(new ColoredText($"- {state.World.GetString(npc.NameId)} (Level {npc.Level})", ConsoleColor.Gray));
                }
            }
        }
    }
}