using System;
using System.Collections.Generic;
using System.Threading;
using RPG.Core;
using RPG.Save;
using RPG.UI;
using RPG.Utils;

namespace RPG.Commands.Builtin
{
    public class SaveCommand : BaseCommand
    {
        public override string Name => "save";
        public override string Description => "Save the current game state";
        public override string[] Aliases => ["s"];

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