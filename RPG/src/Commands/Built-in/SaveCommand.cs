using System;
using System.Collections.Generic;
using System.Threading;
using RPG.Core;
using RPG.Save;
using RPG.UI;
using RPG.Utils;

namespace RPG.Commands.Builtin
{
    /// <summary>
    /// Command that handles saving the current game state to persistent storage.
    /// </summary>
    public class SaveCommand : BaseCommand
    {
        /// <summary>
        /// Gets the primary name of the command used to invoke it.
        /// </summary>
        /// <value>Returns "save" as the command name.</value>
        public override string Name => "save";

        /// <summary>
        /// Gets a brief description of the command's functionality.
        /// </summary>
        /// <value>Returns a description explaining the command's purpose.</value>
        public override string Description => "Save the current game state";

        /// <summary>
        /// Gets an array of alternative names that can be used to invoke this command.
        /// </summary>
        /// <value>Returns an array containing "s" as a shorthand alias.</value>
        public override string[] Aliases => ["s"];

        /// <summary>
        /// Executes the save command, storing the current game state to a file.
        /// </summary>
        /// <param name="args">The save name provided as an argument. If empty, the user will be prompted for a name.</param>
        /// <param name="state">The current game state to be saved.</param>
        /// <remarks>
        /// If a save with the same ID already exists, a backup will be created before saving.
        /// The save operation includes metadata such as player name, level, and location.
        /// </remarks>
        public override void Execute(string args, GameState state)
        {
            string? saveName = args;

            // If no save name provided, prompt for one
            if (string.IsNullOrWhiteSpace(saveName))
            {
                saveName = Program.ShowNewSaveDialogAsync(state.WindowManager, state).Result;
                if (string.IsNullOrWhiteSpace(saveName))
                {
                    state.GameLog.Add("Save cancelled");
                    return;
                }
            }

            try
            {
                string saveId = state.CurrentSaveMetadata?.SaveId ?? GenerateSaveId(saveName);

                // Create backup of existing save
                if (SaveManager.SaveExists(saveId))
                {
                    SaveManager.CreateBackup(saveId);
                }

                // Update metadata
                SaveMetadata metadata = new()
                {
                    SaveId = saveId,
                    CreatedAt = state.CurrentSaveMetadata?.CreatedAt ?? DateTime.UtcNow,
                    LastSavedAt = DateTime.UtcNow,
                    CustomData = new Dictionary<string, string>
                    {
                        ["DisplayName"] = saveName,
                        ["PlayerName"] = state.PlayerName,
                        ["PlayerLevel"] = state.Level.ToString(),
                        ["Location"] = state.World?.GetString(state.CurrentRegion?.NameId ?? 0) ?? "Unknown",
                        ["SaveType"] = SaveType.Manual.ToString()
                    }
                };

                state.CurrentSaveMetadata = metadata;

                // Save the game
                SaveManager.SaveGame(saveId, state);
                state.GameLog.Add(new ColoredText($"Game saved successfully as '{saveName}'", ConsoleColor.Green));
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to save game");
                state.GameLog.Add(new ColoredText("Error: Failed to save game", ConsoleColor.Red));
            }
        }

        private static string GenerateSaveId(string saveName)
        {
            return $"{saveName.ToLower().Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}