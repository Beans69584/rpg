using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPG.Core;
using RPG.Common;
using RPG.World.Data;
using System.Linq;

namespace RPG.Combat
{
    /// <summary>
    /// Manages turn-based combat encounters between the player and enemy entities.
    /// Handles combat initialisation, turn order, action processing, and combat resolution.
    /// </summary>
    public class CombatSystem(GameState state)
    {
        private readonly GameState _state = state;
        private readonly List<CombatEntity> _playerTeam = [];
        private readonly List<CombatEntity> _enemyTeam = [];
        private readonly Queue<CombatAction> _actionQueue = new();
        private bool _isCombatActive;

        /// <summary>
        /// Initiates a combat encounter between the player and a group of enemies.
        /// </summary>
        /// <param name="enemies">The list of enemy entities to battle against.</param>
        /// <returns>
        /// A task that represents the asynchronous combat operation.
        /// The task completes when the combat encounter ends through victory, defeat, or fleeing.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to start combat without a player character being set.
        /// </exception>
        public async Task StartCombatAsync(List<Entity> enemies)
        {
            _isCombatActive = true;
            InitialiseCombat(enemies);

            while (_isCombatActive)
            {
                await ProcessCombatRoundAsync();

                if (CheckCombatEnd())
                {
                    EndCombat();
                    break;
                }

                await Task.Delay(16); // Small delay between rounds
            }
        }

        private void InitialiseCombat(List<Entity> enemies)
        {
            // Clear previous combat state
            _playerTeam.Clear();
            _enemyTeam.Clear();
            _actionQueue.Clear();

            WorldData? worldData = _state.World?.GetWorldData();

            if (_state.CurrentPlayer is null)
                throw new InvalidOperationException("Cannot start combat without a player");

            _playerTeam.Add(new CombatEntity(_state.CurrentPlayer));

            // Add enemies to combat
            foreach (Entity enemy in enemies)
            {
                _enemyTeam.Add(new CombatEntity(enemy, worldData!));
            }

            // Initial combat message
            _state.GameLog.Add(new ColoredText("Combat started!", ConsoleColor.Red));
            foreach (Entity enemy in enemies)
            {
                _state.GameLog.Add($"A {worldData!.GetString(enemy.NameId)} appears!");
            }
        }

        private async Task ProcessCombatRoundAsync()
        {
            // Calculate turn order based on speed/initiative
            List<CombatEntity> turnOrder = CalculateTurnOrder();

            foreach (CombatEntity entity in turnOrder)
            {
                if (!_isCombatActive) break;

                if (entity.IsPlayer)
                {
                    await HandlePlayerTurnAsync(entity);
                }
                else
                {
                    await HandleEnemyTurnAsync(entity);
                }

                // Process any queued actions
                while (_actionQueue.Count > 0)
                {
                    CombatAction action = _actionQueue.Dequeue();
                    await ProcessActionAsync(action);
                }
            }
        }

        private List<CombatEntity> CalculateTurnOrder()
        {
            List<CombatEntity> allEntities = [.. _playerTeam, .. _enemyTeam];

            // Sort by speed/initiative
            allEntities.Sort((a, b) => b.Stats.Speed.CompareTo(a.Stats.Speed));

            return allEntities;
        }

        private async Task HandlePlayerTurnAsync(CombatEntity player)
        {
            _state.GameLog.Add(new ColoredText("\nYour turn!", ConsoleColor.Cyan));
            DisplayCombatStatus();

            bool validAction = false;
            while (!validAction && _isCombatActive)
            {
                _state.GameLog.Add("Choose your action:");
                _state.GameLog.Add("1) Attack");
                _state.GameLog.Add("2) Defend");
                _state.GameLog.Add("3) Use Item");
                _state.GameLog.Add("4) Flee");

                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case '1':
                        validAction = HandlePlayerAttack(player);
                        break;
                    case '2':
                        validAction = HandlePlayerDefend(player);
                        break;
                    case '3':
                        validAction = await HandlePlayerItemUseAsync(player);
                        break;
                    case '4':
                        validAction = HandlePlayerFlee(player);
                        break;
                }
            }
        }

        private bool HandlePlayerAttack(CombatEntity player)
        {
            if (_enemyTeam.Count == 0) return false;

            _state.GameLog.Add("\nChoose a target:");
            for (int i = 0; i < _enemyTeam.Count; i++)
            {
                _state.GameLog.Add($"{i + 1}) {_enemyTeam[i].Name} (HP: {_enemyTeam[i].CurrentHP}/{_enemyTeam[i].MaxHP})");
            }

            ConsoleKeyInfo key = Console.ReadKey(true);
            int targetIndex = key.KeyChar - '1';
            if (targetIndex >= 0 && targetIndex < _enemyTeam.Count)
            {
                CombatEntity target = _enemyTeam[targetIndex];
                _actionQueue.Enqueue(new CombatAction
                {
                    Type = CombatActionType.Attack,
                    Source = player,
                    Target = target
                });
                return true;
            }

            return false;
        }

        private bool HandlePlayerDefend(CombatEntity player)
        {
            _actionQueue.Enqueue(new CombatAction
            {
                Type = CombatActionType.Defend,
                Source = player,
                Target = player
            });
            return true;
        }

        private async Task<bool> HandlePlayerItemUseAsync(CombatEntity player)
        {
            // Implement item usage
            _ = player; // Unused variable warning
            _state.GameLog.Add("Item usage not implemented yet.");
            await Task.Delay(1000);
            return false;
        }

        private bool HandlePlayerFlee(CombatEntity player)
        {
            int fleeChance = CalculateFleeChance(player);
            if (Random.Shared.Next(100) < fleeChance)
            {
                _state.GameLog.Add(new ColoredText("You successfully fled from combat!", ConsoleColor.Green));
                _isCombatActive = false;
                return true;
            }

            _state.GameLog.Add(new ColoredText("Failed to flee!", ConsoleColor.Red));
            return true;
        }

        private async Task HandleEnemyTurnAsync(CombatEntity enemy)
        {
            _state.GameLog.Add($"\n{enemy.Name}'s turn!");
            await Task.Delay(500); // Dramatic pause

            // Simple AI: Always attack player
            _actionQueue.Enqueue(new CombatAction
            {
                Type = CombatActionType.Attack,
                Source = enemy,
                Target = _playerTeam[0]
            });
        }

        private Task ProcessActionAsync(CombatAction action)
        {
            switch (action.Type)
            {
                case CombatActionType.Attack:
                    ProcessAttack(action);
                    break;
                case CombatActionType.Defend:
                    ProcessDefend(action);
                    break;
            }

            return Task.Delay(500); // Give time to read the action result
        }

        private void ProcessAttack(CombatAction action)
        {
            int damage = CalculateDamage(action.Source, action.Target);
            action.Target.CurrentHP -= damage;

            _state.GameLog.Add(new ColoredText(
                $"{action.Source.Name} attacks {action.Target.Name} for {damage} damage!",
                ConsoleColor.Yellow));

            if (action.Target.CurrentHP <= 0)
            {
                HandleEntityDeath(action.Target);
            }
        }

        private void ProcessDefend(CombatAction action)
        {
            action.Source.IsDefending = true;
            _state.GameLog.Add($"{action.Source.Name} takes a defensive stance!");
        }

        private void HandleEntityDeath(CombatEntity entity)
        {
            if (entity.IsPlayer)
            {
                _state.GameLog.Add(new ColoredText("You have been defeated!", ConsoleColor.Red));
                _isCombatActive = false;
            }
            else
            {
                _state.GameLog.Add(new ColoredText($"{entity.Name} has been defeated!", ConsoleColor.Green));
                _enemyTeam.Remove(entity);
            }
        }

        private bool CheckCombatEnd()
        {
            return _playerTeam.Count == 0 || _enemyTeam.Count == 0;
        }

        private void EndCombat()
        {
            if (_playerTeam.Count > 0)
            {
                _state.GameLog.Add(new ColoredText("Victory!", ConsoleColor.Green));
                // Handle experience and loot
            }
            _isCombatActive = false;
        }

        private void DisplayCombatStatus()
        {
            _state.GameLog.Add("\nCombat Status:");
            _state.GameLog.Add($"Player HP: {_playerTeam[0].CurrentHP}/{_playerTeam[0].MaxHP}");
            _state.GameLog.Add("\nEnemies:");
            foreach (CombatEntity enemy in _enemyTeam)
            {
                _state.GameLog.Add($"{enemy.Name}: HP {enemy.CurrentHP}/{enemy.MaxHP}");
            }
        }

        private int CalculateDamage(CombatEntity attacker, CombatEntity defender)
        {
            int baseDamage = attacker.Stats.Strength;
            if (defender.IsDefending)
            {
                baseDamage = (int)(baseDamage * 0.5f);
                defender.IsDefending = false;
            }
            return Math.Max(1, baseDamage - defender.Stats.Defense);
        }

        private int CalculateFleeChance(CombatEntity entity)
        {
            // Base 50% chance, modified by speed difference
            int baseChance = 50;
            int speedDiff = entity.Stats.Speed - _enemyTeam.Max(e => e.Stats.Speed);
            return Math.Clamp(baseChance + (speedDiff * 5), 20, 80);
        }
    }
}