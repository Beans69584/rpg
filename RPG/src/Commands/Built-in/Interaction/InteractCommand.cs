using System;
using RPG.Core;
using RPG.UI.Windows;
using RPG.World.Data;

namespace RPG.Commands.Interaction
{
    public class InteractCommand : BaseCommand
    {
        public override string Name => "interact";
        public override string Description => "Interact with an NPC or object";
        public override string[] Aliases => ["talk", "use"];

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