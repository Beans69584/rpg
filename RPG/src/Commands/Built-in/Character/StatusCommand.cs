using System;
using RPG.Core;

namespace RPG.Commands.Character
{
    public class StatusCommand : BaseCommand
    {
        public override string Name => "status";
        public override string Description => "View your character status";
        public override string[] Aliases => ["stat", "stats"];

        public override void Execute(string args, GameState state)
        {
            state.GameLog.Add(new ColoredText("=== Character Status ===", ConsoleColor.Yellow));
            state.GameLog.Add("");
            state.GameLog.Add(new ColoredText($"Name: {state.PlayerName}", ConsoleColor.White));
            state.GameLog.Add(new ColoredText($"Level: {state.Level}", ConsoleColor.White));
            state.GameLog.Add(new ColoredText($"Class: {state.CurrentPlayer?.GetType().Name ?? "Unknown"}", ConsoleColor.White));
            state.GameLog.Add(new ColoredText($"HP: {state.HP}/{state.MaxHP}", ConsoleColor.White));
            state.GameLog.Add(new ColoredText($"Gold: {state.Gold}", ConsoleColor.Yellow));
            state.GameLog.Add("");

            state.GameLog.Add(new ColoredText("Experience:", ConsoleColor.Green));
            float progress = state.ExperienceSystem.GetLevelProgress() * 100;
            int requiredXp = state.ExperienceSystem.GetRequiredExperience(state.Level);
            state.GameLog.Add(new ColoredText($"{state.CurrentExperience} / {requiredXp} ({progress:F1}%)", ConsoleColor.Green));

            // XP bar
            int barWidth = 20;
            int filledWidth = (int)(progress * barWidth / 100);
            string bar = $"[{new string('=', filledWidth)}{new string('-', barWidth - filledWidth)}]";
            state.GameLog.Add(new ColoredText(bar, ConsoleColor.Green));
        }
    }
}