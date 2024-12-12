using System;
using RPG.Core;

namespace RPG.Commands.Character
{
    /// <summary>
    /// Represents a command that displays the current status of the player character,
    /// including vital statistics and experience progress.
    /// </summary>
    public class StatusCommand : BaseCommand
    {
        /// <summary>
        /// Gets the primary name of the command used to invoke it from the game console.
        /// </summary>
        /// <value>Returns "status" as the command name.</value>
        public override string Name => "status";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>Returns a description explaining the command's purpose.</value>
        public override string Description => "View your character status";

        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>Returns an array of strings containing "stat" and "stats" as aliases.</value>
        public override string[] Aliases => ["stat", "stats"];

        /// <summary>
        /// Executes the status command, displaying a detailed overview of the player character's
        /// current statistics and experience progress.
        /// </summary>
        /// <param name="args">Additional arguments passed to the command (not used in this implementation).</param>
        /// <param name="state">The current game state containing player information and statistics.</param>
        /// <remarks>
        /// Displays information including:
        /// - Character name and level
        /// - Character class
        /// - Current and maximum HP
        /// - Gold amount
        /// - Experience progress with a visual progress bar
        /// </remarks>
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