using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Utils;
using RPG.World.Data;

namespace RPG.Core.Managers
{
    public class QuestManager(GameState state)
    {
        private readonly GameState _state = state;
        private readonly Dictionary<int, ActiveQuest> _activeQuests = [];
        private readonly Dictionary<int, Quest> _completedQuests = [];

        public bool AcceptQuest(Quest quest)
        {
            Logger.Debug($"Attempting to accept quest: {quest.NameId}");

            // Check if quest is already active or completed
            if (_activeQuests.ContainsKey(quest.GiverId))
            {
                Logger.Debug("Quest already active");
                return false;
            }

            if (_completedQuests.ContainsKey(quest.GiverId))
            {
                Logger.Debug("Quest already completed");
                return false;
            }

            // Check level requirement
            if (quest.MinLevel > _state.Level)
            {
                Logger.Debug($"Level too low. Required: {quest.MinLevel}, Current: {_state.Level}");
                return false;
            }

            // Check prerequisites
            foreach (int prereqId in quest.PrerequisiteQuests)
            {
                if (!_completedQuests.ContainsKey(prereqId))
                {
                    Logger.Debug($"Missing prerequisite quest: {prereqId}");
                    return false;
                }
            }

            // Check required flags
            foreach (string flag in quest.RequiredFlags)
            {
                if (!_state.GameFlags.TryGetValue(flag, out bool value) || !value)
                {
                    Logger.Debug($"Missing required flag: {flag}");
                    return false;
                }
            }

            _activeQuests[quest.GiverId] = new ActiveQuest(quest);
            Logger.Debug("Quest accepted successfully");
            return true;
        }

        public void UpdateObjective(string type, string target, int amount = 1)
        {
            foreach (ActiveQuest activeQuest in _activeQuests.Values)
            {
                foreach (QuestObjective objective in activeQuest.Quest.Objectives)
                {
                    if (objective.Type == type && objective.Target == target)
                    {
                        activeQuest.UpdateProgress(objective, amount);
                        CheckQuestCompletion(activeQuest);
                    }
                }
            }
        }

        private void CheckQuestCompletion(ActiveQuest quest)
        {
            if (quest.IsComplete())
            {
                _activeQuests.Remove(quest.Quest.GiverId);
                _completedQuests[quest.Quest.GiverId] = quest.Quest;

                // Give rewards
                foreach (Reward reward in quest.Quest.Rewards)
                {
                    GiveReward(reward);
                }

                _state.GameLog.Add(new ColoredText($"Quest Complete: {_state.World?.GetString(quest.Quest.NameId)}", ConsoleColor.Green));
            }
        }

        private void GiveReward(Reward reward)
        {
            switch (reward.Type.ToLower())
            {
                case "gold":
                    _state.Gold += reward.Value;
                    _state.GameLog.Add(new ColoredText($"Received {reward.Value} gold", ConsoleColor.Yellow));
                    break;

                case "xp":
                case "experience":
                    _state.ExperienceSystem.AddExperience(reward.Value);
                    break;

                case "item":
                    _state.Inventory.Add(_state.World?.GetString(reward.DescriptionId) ?? "Unknown Item");
                    _state.GameLog.Add(new ColoredText($"Received {_state.World?.GetString(reward.DescriptionId)}", ConsoleColor.Green));
                    break;
            }
        }

        public List<ActiveQuest> GetActiveQuests()
        {
            return [.. _activeQuests.Values];
        }

        public List<Quest> GetCompletedQuests()
        {
            return [.. _completedQuests.Values];
        }
    }

    public class ActiveQuest
    {
        public Quest Quest { get; }
        private readonly Dictionary<QuestObjective, int> _progress = [];

        public ActiveQuest(Quest quest)
        {
            Quest = quest;
            foreach (QuestObjective objective in quest.Objectives)
            {
                _progress[objective] = 0;
            }
        }

        public void UpdateProgress(QuestObjective objective, int amount)
        {
            if (_progress.ContainsKey(objective))
            {
                _progress[objective] = Math.Min(_progress[objective] + amount, objective.Required);
            }
        }

        public bool IsComplete()
        {
            return _progress.All(p => p.Value >= p.Key.Required);
        }

        public int GetProgress(QuestObjective objective)
        {
            return _progress.GetValueOrDefault(objective);
        }
    }
}