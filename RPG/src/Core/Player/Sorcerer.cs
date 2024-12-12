using RPG.Core.Player.Common;
using RPG.World.Data;
using System;
using System.Collections.Generic;

namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a sorcerer player.
    /// </summary>
    public class Sorcerer : Magician
    {
        private GameState? gameState;
        /// <summary>
        /// Triggered when the player transforms to a Sith.
        /// </summary>
        public event Action? OnTransformToSith;

        private int evilLevel;
        /// <summary>
        /// The evil level of the sorcerer.
        /// </summary>
        public int EvilLevel
        {
            get => evilLevel;
            set
            {
                evilLevel = value;
                if (evilLevel > 100 && GetType() == typeof(Sorcerer))
                {
                    TransformToSith();
                }
            }
        }

        /// <summary>
        /// Initialises the sorcerer.
        /// </summary>
        /// <param name="state">The game state.</param>
        public void Initialise(GameState state)
        {
            gameState = state;
        }

        private void TransformToSith()
        {
            if (gameState == null) return;

            SithSorcerer sith = new()
            {
                Name = Name,
                Position = Position,
                Age = Age,
                HitPoints = HitPoints,
                Gold = Gold,
                MagicPower = MagicPower,
                KnownSpells = [.. KnownSpells],
                EvilLevel = evilLevel
            };
            sith.Initialise(gameState);

            OnTransformToSith?.Invoke();
            gameState.TransformPlayerClass(sith);
        }
    }
}