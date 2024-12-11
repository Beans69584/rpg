using System;
using System.Collections.Generic;
using System.Threading;

using RPG;
using RPG.UI;
using RPG.Core;
using RPG.Commands;
using RPG.Save;

namespace RPG.Commands.Builtin
{
    /// <summary>
    /// Built-in command that saves the game.
    /// </summary>
    public class SaveCommand : BaseCommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public override string Name => "save";
        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        public override string Description => "Save the current game state";
        /// <summary>
        /// Gets a list of aliases for the command.
        /// </summary>
        public override string[] Aliases => ["s"];

        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The display name for the save (optional).</param>
        /// <param name="state">The current game state.</param>
        public override void Execute(string args, GameState state)
        {
            string displayName = string.IsNullOrWhiteSpace(args) ?
                $"{state.PlayerName}'s Save" : args.Trim();

            string uuid = Guid.NewGuid().ToString("N");

            // Store display name in metadata
            state.CurrentSaveMetadata.CustomData["DisplayName"] = displayName;

            if (SaveManager.SaveExists(uuid))
            {
                ConfirmOverwrite(uuid, displayName, state);
            }
            else
            {
                state.SaveGame(uuid);
                state.GameLog.Add($"Game saved as '{displayName}'");
            }
        }

        private void ConfirmOverwrite(string uuid, string displayName, GameState state)
        {
            bool selected = false;
            ConsoleWindowManager manager = state.WindowManager;

            // Store original regions to restore later
            Dictionary<string, Region> originalRegions = [];
            foreach (KeyValuePair<string, Region> regionKV in manager.GetRegions())
            {
                originalRegions[regionKV.Key] = regionKV.Value;
                manager.UpdateRegion(regionKV.Key, r => r.IsVisible = false);
            }

            Region dialog = new()
            {
                Name = "Confirm Save",
                BorderColor = ConsoleColor.Yellow,
                TitleColor = ConsoleColor.Yellow,
                RenderContent = (region) =>
                {
                    List<ColoredText> lines =
                    [
                        "",
                        $"Save '{displayName}' already exists.",
                        "Do you want to overwrite it?",
                        "",
                        selected ? "> [Yes]    No  <" : "  Yes   > [No] <",
                        ""
                    ];
                    manager.RenderWrappedText(region, lines);
                }
            };

            void UpdateLayout()
            {
                manager.UpdateRegion("Confirm", r =>
                {
                    r.X = Console.WindowWidth / 4;
                    r.Y = Console.WindowHeight / 3;
                    r.Width = Console.WindowWidth / 2;
                    r.Height = 8;
                });
            }

            manager.AddRegion("Confirm", dialog);
            UpdateLayout();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.RightArrow:
                            selected = !selected;
                            manager.QueueRender();
                            break;

                        case ConsoleKey.Enter:
                            if (selected)
                            {
                                state.SaveGame(uuid);
                                state.GameLog.Add($"Game saved as '{displayName}'");
                            }
                            RestoreRegions();
                            return;

                        case ConsoleKey.Escape:
                            RestoreRegions();
                            return;
                    }
                }

                if (manager.CheckResize())
                {
                    UpdateLayout();
                }

                Thread.Sleep(16);
            }

            void RestoreRegions()
            {
                manager.RemoveRegion("Confirm");
                foreach (KeyValuePair<string, Region> regionKV in originalRegions)
                {
                    manager.UpdateRegion(regionKV.Key, r => r.IsVisible = true);
                }
                manager.QueueRender();
            }
        }
    }
}
