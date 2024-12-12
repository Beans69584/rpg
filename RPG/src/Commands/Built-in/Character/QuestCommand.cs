using System;
using System.Collections.Generic;
using RPG.Core;
using RPG.Core.Managers;
using RPG.World.Data;

namespace RPG.Commands.Character
{
    public class QuestsCommand : BaseCommand
    {
        public override string Name => "quests";
        public override string Description => "View your active quests";
        public override string[] Aliases => [];

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