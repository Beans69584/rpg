using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Utils;
using RPG.World.Data;

namespace RPG.Core.Managers
{
    /// <summary>
    /// Manages the tracking, acceptance, completion and rewards for quests in the game.
    /// </summary>
    public class QuestManager(GameState state)
    {
        private readonly GameState _state = state;
        private readonly Dictionary<int, ActiveQuest> _activeQuests = [];
        private readonly Dictionary<int, Quest> _completedQuests = [];

        /// <summary>
        /// Attempts to accept a new quest for the player.
        /// </summary>
        /// <param name="quest">The quest to be accepted.</param>
        /// <returns>True if the quest was successfully accepted, false otherwise. Acceptance can fail if the quest is already active/completed,
        /// the player's level is too low, prerequisites are not met, or required flags are not set.</returns>
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

        /// <summary>
        /// Updates the progress of any active quest objectives that match the specified criteria.
        /// </summary>
        /// <param name="type">The type of objective to update (e.g., "kill", "collect").</param>
        /// <param name="target">The specific target of the objective (e.g., "goblin", "herb").</param>
        /// <param name="amount">The amount to increment the objective progress by. Defaults to 1.</param>
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

        /// <summary>
        /// Retrieves a list of all currently active quests.
        /// </summary>
        /// <returns>A new list containing all active quests.</returns>
        public List<ActiveQuest> GetActiveQuests()
        {
            return [.. _activeQuests.Values];
        }

        /// <summary>
        /// Retrieves a list of all completed quests.
        /// </summary>
        /// <returns>A new list containing all completed quests.</returns>
        public List<Quest> GetCompletedQuests()
        {
            return [.. _completedQuests.Values];
        }
    }

    /// <summary>
    /// Represents a quest that is currently in progress, tracking its objectives and completion status.
    /// </summary>
    public class ActiveQuest
    {
        /// <summary>
        /// Gets the base quest information and requirements.
        /// </summary>
        public Quest Quest { get; }
        private readonly Dictionary<QuestObjective, int> _progress = [];

        /// <summary>
        /// Initialises a new instance of the ActiveQuest class.
        /// </summary>
        /// <param name="quest">The base quest to track progress for.</param>
        public ActiveQuest(Quest quest)
        {
            Quest = quest;
            foreach (QuestObjective objective in quest.Objectives)
            {
                _progress[objective] = 0;
            }
        }

        /// <summary>
        /// Updates the progress of a specific objective within the quest.
        /// </summary>
        /// <param name="objective">The objective to update progress for.</param>
        /// <param name="amount">The amount to increment the progress by.</param>
        public void UpdateProgress(QuestObjective objective, int amount)
        {
            if (_progress.ContainsKey(objective))
            {
                _progress[objective] = Math.Min(_progress[objective] + amount, objective.Required);
            }
        }

        /// <summary>
        /// Checks if all objectives for the quest have been completed.
        /// </summary>
        /// <returns>True if all objectives have met their required amounts, false otherwise.</returns>
        public bool IsComplete()
        {
            return _progress.All(p => p.Value >= p.Key.Required);
        }

        /// <summary>
        /// Retrieves the current progress for a specific objective.
        /// </summary>
        /// <param name="objective">The objective to check progress for.</param>
        /// <returns>The current progress amount for the specified objective. Returns 0 if the objective is not found.</returns>
        public int GetProgress(QuestObjective objective)
        {
            return _progress.GetValueOrDefault(objective);
        }
    }
}