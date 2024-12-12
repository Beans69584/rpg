using System;
using System.Linq;
using RPG.Core;
using RPG.UI.Windows;
using RPG.World.Data;

namespace RPG.Commands.Interaction
{
    /// <summary>
    /// Represents a command that handles interaction between the player and NPCs or objects in the game world.
    /// </summary>
    public class InteractCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command used to invoke the interaction.
        /// </summary>
        /// <value>The string "interact".</value>
        public override string Name => "interact";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>A string explaining the command's purpose.</value>
        public override string Description => "Interact with an NPC or object";

        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>An array of strings containing "talk" and "use".</value>
        public override string[] Aliases => ["talk", "use"];

        /// <summary>
        /// Executes the interaction command, allowing the player to converse with NPCs or interact with objects.
        /// </summary>
        /// <param name="args">The name of the NPC or object to interact with.</param>
        /// <param name="state">The current game state containing world and player information.</param>
        /// <remarks>
        /// The method performs the following actions:
        /// - Validates that the player is in a building
        /// - Locates the specified NPC within the current building
        /// - Displays NPC information if found
        /// - Initiates a dialogue if the NPC has an available dialogue tree
        /// If the NPC cannot be found, lists all available NPCs in the current location.
        /// </remarks>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                state.GameLog.Add("Who or what do you want to interact with?");
                return;
            }

            Location? currentLocation = state.CurrentLocation;
            Building? currentBuilding = currentLocation?.CurrentBuilding;

            if (currentBuilding == null)
            {
                state.GameLog.Add("You need to be in a building to interact with someone.");
                return;
            }

            string targetName = args.Trim().ToLower();
            Entity? targetNpc = null;

            foreach (int npcId in currentBuilding.NPCs)
            {
                Entity npc = state.World!.GetWorldData().NPCs[npcId];
                string npcName = state.World.GetString(npc.NameId).ToLower();

                if (npcName == targetName || npcName.Contains(targetName))
                {
                    targetNpc = npc;
                    break;
                }
            }

            if (targetNpc == null)
            {
                state.GameLog.Add($"You don't see '{args.Trim()}' here.");
                state.GameLog.Add("Available NPCs:");
                foreach (int npcId in currentBuilding.NPCs)
                {
                    Entity npc = state.World!.GetWorldData().NPCs[npcId];
                    state.GameLog.Add($"  - {state.World.GetString(npc.NameId)} (Level {npc.Level} {npc.Role})");
                }
                return;
            }

            // Show NPC info
            state.GameLog.Add(new ColoredText($"=== Talking to {state.World!.GetString(targetNpc.NameId)} ===", ConsoleColor.Yellow));
            state.GameLog.Add(new ColoredText($"Level {targetNpc.Level} {targetNpc.Role}", ConsoleColor.Cyan));
            state.GameLog.Add("");

            // Start dialogue if available
            if (targetNpc.DialogueTreeRefs.Count > 0)
            {
                int dialogueId = targetNpc.DialogueTreeRefs[0];
                DialogueTree dialogueTree = state.World.GetWorldData().Resources.DialogueTrees[dialogueId];

                // Check if dialogue has required flags
                bool canStart = true;
                foreach (string flag in dialogueTree.RequiredFlags)
                {
                    if (!state.GetFlag(flag))
                    {
                        canStart = false;
                        break;
                    }
                }

                if (!canStart)
                {
                    state.GameLog.Add($"{state.World.GetString(targetNpc.NameId)} has nothing to say right now.");
                    return;
                }

                DialogueWindow dialogue = new(state);
                _ = dialogue.ShowDialogueAsync(targetNpc, dialogueTree);
            }
            else
            {
                state.GameLog.Add($"{state.World.GetString(targetNpc.NameId)} nods quietly.");
            }
        }
    }
}