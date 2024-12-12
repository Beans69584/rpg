using System;
using System.Collections.Generic;
using RPG.Core;
using RPG.Core.Managers;
using RPG.World.Data;

namespace RPG.Commands.Character
{
    /// <summary>
    /// Represents a command that displays the player's active quests and their progress.
    /// </summary>
    public class QuestsCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>Returns "quests" as the command name.</value>
        public override string Name => "quests";

        /// <summary>
        /// Gets the description of the command's functionality.
        /// </summary>
        /// <value>Returns a brief description explaining the command's purpose.</value>
        public override string Description => "View your active quests";

        /// <summary>
        /// Gets the alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>Returns an array of command aliases.</value>
        public override string[] Aliases => [];

        /// <summary>
        /// Executes the quests command, displaying all active quests and their objectives.
        /// If no quests are active, informs the player of this. For each active quest,
        /// shows its name, description, and objectives with completion progress.
        /// </summary>
        /// <param name="args">The command arguments (unused in this implementation).</param>
        /// <param name="state">The current game state containing quest and world information.</param>
        public override void Execute(string args, GameState state)
        {
            List<ActiveQuest> quests = state.QuestManager.GetActiveQuests();

            if (quests.Count == 0)
            {
                state.GameLog.Add("You have no active quests.");
                return;
            }

            state.GameLog.Add(new ColoredText("=== Active Quests ===", ConsoleColor.Yellow));
            state.GameLog.Add("");

            foreach (ActiveQuest quest in quests)
            {
                state.GameLog.Add(new ColoredText(state.World?.GetString(quest.Quest.NameId) ?? "", ConsoleColor.Cyan));
                state.GameLog.Add(state.World?.GetString(quest.Quest.DescriptionId) ?? "");

                // Show objectives
                state.GameLog.Add("Objectives:");
                foreach (QuestObjective objective in quest.Quest.Objectives)
                {
                    int current = quest.GetProgress(objective);
                    string progress = $"{current}/{objective.Required}";
                    bool isComplete = current >= objective.Required;
                    state.GameLog.Add(new ColoredText(
                        $"- {state.World?.GetString(objective.DescriptionId)} ({progress})",
                        isComplete ? ConsoleColor.Green : ConsoleColor.White));
                }
                state.GameLog.Add("");
            }
        }
    }
}