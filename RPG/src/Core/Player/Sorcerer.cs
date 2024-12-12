using RPG.Core.Player.Common;
using RPG.World.Data;
using System;
using System.Collections.Generic;

namespace RPG.Core.Player
{
    public class Sorcerer : Magician
    {
        private GameState? gameState;
        public event Action? OnTransformToSith;

        private int evilLevel;
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

        public void Initialize(GameState state)
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
            sith.Initialize(gameState);

            OnTransformToSith?.Invoke();
            gameState.TransformPlayerClass(sith);
        }
    }
}