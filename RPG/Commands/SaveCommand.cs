using System;
using System.Collections.Generic;
using System.Threading;
using RPG;

namespace RPG.Commands
{
    /// <summary>
    /// Built-in command that saves the game to a save slot.
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
        public override string Description => "Save game to a slot (1-5)";
        /// <summary>
        /// Gets a list of aliases for the command.
        /// </summary>
        public override string[] Aliases => ["s"];

        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        public override void Execute(string args, GameState state)
        {
            if (string.IsNullOrWhiteSpace(args) || !int.TryParse(args, out int slot) || slot < 1 || slot > 5)
            {
                state.GameLog.Add("Usage: save <1-5>");
                return;
            }

            string slotStr = slot.ToString();

            if (SaveManager.SaveExists(slotStr))
            {
                ConfirmOverwrite(slotStr, state);
            }
            else
            {
                state.SaveGame(slotStr);
            }
        }

        private void ConfirmOverwrite(string slot, GameState state)
        {
            bool selected = false;
            ConsoleWindowManager manager = state.WindowManager; // Use existing window manager

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
                        $"Save slot {slot} already exists.",
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
                                state.SaveGame(slot);
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
