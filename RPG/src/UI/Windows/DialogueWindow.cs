using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using RPG.Core;
using RPG.World.Data;
using System.Linq;

namespace RPG.UI.Windows
{
    public class DialogueWindow : IAsyncDisposable
    {
        private readonly GameState _state;
        private readonly Region _dialogueRegion;
        private DialogueTree? _currentDialogue;
        private DialogueNode? _currentNode;
        private Entity? _currentNPC;
        private bool _isActive;
        private int _selectedOption;

        public DialogueWindow(GameState state)
        {
            _state = state;
            _dialogueRegion = new Region
            {
                Name = "Dialogue",
                BorderColor = ConsoleColor.Cyan,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = RenderDialogue
            };
        }

        public async Task ShowDialogueAsync(Entity npc, DialogueTree dialogue)
        {
            _currentNPC = npc;
            _currentDialogue = dialogue;
            _currentNode = dialogue.Nodes[dialogue.RootNodeId];
            _selectedOption = 0;
            _isActive = true;

            _state.InputSuspended = true;
            _state.WindowManager.AddRegion("Dialogue", _dialogueRegion);
            UpdateLayout();

            try
            {
                await RunDialogueLoopAsync();
            }
            finally
            {
                await DisposeAsync();
            }
        }

        private void RenderDialogue(Region region)
        {
            if (_currentNode == null || _currentDialogue == null || _currentNPC == null) return;

            List<ColoredText> content =
            [
                new($"=== {_state.World!.GetString(_currentNPC.NameId)} ===", ConsoleColor.Yellow),
                "",
                _state.World.GetString(_currentNode.TextId),
                ""
            ];

            // Handle end of dialogue nodes specially
            if (_currentNode.Responses.Count == 0)
            {
                content.Add(new ColoredText("[Press Enter to end conversation]", ConsoleColor.Gray));
            }
            else
            {
                // Filter available responses based on conditions
                List<DialogueResponse> availableResponses = [.. _currentNode.Responses.Where(IsResponseAvailable)];

                // Ensure selected option is valid
                if (_selectedOption >= availableResponses.Count)
                {
                    _selectedOption = 0;
                }

                // Add responses for nodes with choices
                for (int i = 0; i < availableResponses.Count; i++)
                {
                    string prefix = i == _selectedOption ? "> " : "  ";
                    content.Add(new ColoredText(
                        $"{prefix}{_state.World.GetString(availableResponses[i].TextId)}",
                        i == _selectedOption ? ConsoleColor.Cyan : ConsoleColor.Gray
                    ));
                }
            }

            _state.WindowManager.RenderWrappedText(region, content);
        }

        private void UpdateLayout()
        {
            _state.WindowManager.UpdateRegion("Dialogue", r =>
            {
                r.X = 2;
                r.Y = Console.WindowHeight - 15;
                r.Width = Console.WindowWidth - 4;
                r.Height = 13;
            });
        }

        private async Task RunDialogueLoopAsync()
        {
            while (_isActive)
            {
                _state.Input.Update();

                // Process key events directly
                foreach (ConsoleKeyInfo keyInfo in _state.Input.GetKeyEvents())
                {
                    ProcessKey(keyInfo.Key);
                }

                if (_state.WindowManager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(10); // Adjust delay as needed
            }
        }

        private void ProcessKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow when _currentNode?.Responses.Count > 0:
                    CycleOption(-1);
                    break;
                case ConsoleKey.DownArrow when _currentNode?.Responses.Count > 0:
                    CycleOption(1);
                    break;
                case ConsoleKey.Enter:
                    if (_currentNode?.Responses.Count > 0)
                        SelectCurrentOption();
                    else
                        _isActive = false;
                    break;
                case ConsoleKey.Escape:
                    _isActive = false;
                    break;
            }

            _state.WindowManager.QueueRender();
        }

        private void CycleOption(int direction)
        {
            if (_currentNode == null || _currentNode.Responses.Count == 0) return;

            int count = _currentNode.Responses.Count;
            _selectedOption = (_selectedOption + direction + count) % count;
            _state.WindowManager.QueueRender();
        }

        private void SelectCurrentOption()
        {
            if (_currentNode == null || _currentDialogue == null) return;

            // Filter available responses based on conditions
            List<DialogueResponse> availableResponses = [.. _currentNode.Responses.Where(IsResponseAvailable)];

            if (_selectedOption >= availableResponses.Count) return;

            DialogueResponse response = availableResponses[_selectedOption];

            // Process response actions
            foreach (DialogueAction action in response.Actions)
            {
                ProcessAction(action);
            }

            // Process node actions
            if (_currentDialogue.Nodes.TryGetValue(response.NextNodeId, out DialogueNode? nextNode))
            {
                foreach (DialogueAction action in nextNode.Actions)
                {
                    ProcessAction(action);
                }
            }

            // Move to next node or end dialogue
            if (!_currentDialogue.Nodes.ContainsKey(response.NextNodeId))
            {
                _isActive = false;
                return;
            }

            _currentNode = _currentDialogue.Nodes[response.NextNodeId];
            _selectedOption = 0;
            _state.WindowManager.QueueRender();
        }

        private bool EvaluateCondition(string condition)
        {
            // Split condition into parts (e.g., "hasflag:quest_started")
            string[] parts = condition.Split(':');
            return parts.Length == 2
                && parts[0].ToLower() switch
                {
                    "hasflag" => _state.GetFlag(parts[1]),
                    "notflag" => !_state.GetFlag(parts[1]),
                    "hasitem" => _state.Inventory.Contains(parts[1]),
                    "level" => _state.Level >= int.Parse(parts[1]),
                    "reputation" => _state.GetReputation(parts[1].Split('=')[0]) >= int.Parse(parts[1].Split('=')[1]),
                    _ => false
                };
        }

        private bool IsResponseAvailable(DialogueResponse response)
        {
            // Check both condition and required flags
            return (string.IsNullOrEmpty(response.Condition) ||
                EvaluateCondition(response.Condition))
                && (response.RequiredFlags == null ||
                !response.RequiredFlags.Any(flag => !_state.GetFlag(flag)));
        }

        private void ProcessAction(DialogueAction action)
        {
            switch (action.Type.ToLower())
            {
                case "setflag":
                    _state.SetFlag(action.Target, bool.Parse(action.Value));
                    break;

                case "givequest":
                    if (int.TryParse(action.Target, out int questId) &&
                        _state.World?.GetWorldData().Resources.Quests.TryGetValue(questId, out Quest? quest) == true)
                    {
                        _state.QuestManager.AcceptQuest(quest);
                        _state.GameLog.Add(new ColoredText($"New quest: {_state.World.GetString(quest.NameId)}", ConsoleColor.Yellow));
                    }
                    break;

                case "giveitem":
                    if (int.TryParse(action.Target, out int itemId) &&
                        _state.World?.GetWorldData().Items.Count > itemId)
                    {
                        Item item = _state.World.GetWorldData().Items[itemId];
                        _state.Inventory.Add(_state.World.GetString(item.NameId));
                        _state.GameLog.Add(new ColoredText($"Received: {_state.World.GetString(item.NameId)}", ConsoleColor.Green));
                    }
                    break;

                case "givegold":
                    if (int.TryParse(action.Value, out int gold))
                    {
                        _state.Gold += gold;
                        _state.GameLog.Add(new ColoredText($"Received {gold} gold", ConsoleColor.Yellow));
                    }
                    break;

                case "reputation":
                    string[] parts = action.Value.Split('=');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int amount))
                    {
                        _state.ModifyReputation(parts[0], amount);
                    }
                    break;

                case "experience":
                    if (int.TryParse(action.Value, out int xp))
                    {
                        _state.ExperienceSystem.AddExperience(xp);
                    }
                    break;
            }
        }

        public ValueTask DisposeAsync()
        {
            _state.WindowManager.RemoveRegion("Dialogue");
            _state.InputSuspended = false;
            return ValueTask.CompletedTask;
        }
    }
}
