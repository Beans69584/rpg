using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPG.Core;
using RPG.Core.Player;
using RPG.Core.Player.Common;

namespace RPG.UI.Windows
{
    /// <summary>
    /// Represents a window for creating a new character.
    /// </summary>
    public class CharacterCreationWindow
    {
        private readonly GameState state;
        private readonly CharacterFactory characterFactory;
        private readonly Dictionary<string, int> attributes;
        private string selectedClass = "warrior";
        private int attributePoints = 30;
        private int selectedAttribute = 0;
        private readonly string[] attributeNames = ["Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma"];
        private readonly string[] classOptions = ["warrior", "barbarian", "paladin", "sorcerer", "conjurer"];
        private readonly string[] classDescriptions = [
            "Warriors excel in combat, focusing on weapon mastery and defensive tactics.",
            "Barbarians channel their rage to become unstoppable forces of destruction.",
            "Paladins combine martial prowess with divine magic to protect the innocent.",
            "Sorcerers wield innate magical abilities through their bloodline.",
            "Conjurers master the art of summoning creatures and magical effects."
        ];
        private readonly Dictionary<string, ConsoleColor> classColors = new()
        {
            ["warrior"] = ConsoleColor.DarkRed,
            ["barbarian"] = ConsoleColor.DarkMagenta,
            ["paladin"] = ConsoleColor.DarkYellow,
            ["sorcerer"] = ConsoleColor.DarkBlue,
            ["conjurer"] = ConsoleColor.DarkCyan
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="CharacterCreationWindow"/> class.
        /// </summary>
        /// <param name="state">The current game state.</param>
        public CharacterCreationWindow(GameState state)
        {
            this.state = state;
            this.characterFactory = new CharacterFactory(state);
            this.attributes = new Dictionary<string, int>
            {
                ["Strength"] = 10,
                ["Dexterity"] = 10,
                ["Constitution"] = 10,
                ["Intelligence"] = 10,
                ["Wisdom"] = 10,
                ["Charisma"] = 10
            };
        }

        /// <summary>
        /// Displays the character creation window and returns the created character.
        /// </summary>
        /// <returns>The created character, or null if the player cancels.</returns>
        public async Task<Person?> ShowAsync()
        {
            using ConsoleWindowManager manager = state.WindowManager;

            Region creationRegion = new()
            {
                Name = state.Localization.GetString("CharacterCreation_Title"),
                BorderColor = ConsoleColor.DarkCyan,  // Changed from Cyan to DarkCyan
                TitleColor = ConsoleColor.Gray,       // Changed from White to Gray
                RenderContent = (region) =>
                {
                    int currentClassIndex = Array.IndexOf(classOptions, selectedClass);
                    List<ColoredText> content = [];

                    // Calculate consistent padding
                    int innerWidth = region.Width - 4;  // -4 for borders and minimal padding

                    // Get border characters from current settings
                    Dictionary<string, char> boxChars;
                    if (GameSettings.Instance.Display.UseUnicodeBorders)
                    {
                        boxChars = GameSettings.Instance.Display.UseCurvedBorders
                            ? new Dictionary<string, char>
                            {
                                ["topLeft"] = '╭',
                                ["topRight"] = '╮',
                                ["bottomLeft"] = '╰',
                                ["bottomRight"] = '╯',
                                ["horizontal"] = '─',
                                ["vertical"] = '│'
                            }
                            : new Dictionary<string, char>
                            {
                                ["topLeft"] = '┌',
                                ["topRight"] = '┐',
                                ["bottomLeft"] = '└',
                                ["bottomRight"] = '┘',
                                ["horizontal"] = '─',
                                ["vertical"] = '│'
                            };
                    }
                    else
                    {
                        boxChars = new Dictionary<string, char>
                        {
                            ["topLeft"] = '+',
                            ["topRight"] = '+',
                            ["bottomLeft"] = '+',
                            ["bottomRight"] = '+',
                            ["horizontal"] = '-',
                            ["vertical"] = '|'
                        };
                    }

                    string horizontalLine = new(boxChars["horizontal"], innerWidth);

                    // Class Selection Section
                    content.Add(new ColoredText($"{boxChars["topLeft"]}{horizontalLine}{boxChars["topRight"]}", ConsoleColor.Gray));
                    content.Add(new ColoredText($"{boxChars["vertical"]} {state.Localization.GetString("CharacterCreation_ClassSelect").PadRight(innerWidth - 1)}{boxChars["vertical"]}", ConsoleColor.Gray));
                    content.Add(new ColoredText($"{boxChars["vertical"]}{horizontalLine}{boxChars["vertical"]}", ConsoleColor.Gray));

                    // Class options
                    foreach (string className in classOptions)
                    {
                        bool isSelected = className == selectedClass;
                        string displayName = className.ToUpper();
                        string display = isSelected ? $"{boxChars["vertical"]} ▶ [{displayName}]" : $"{boxChars["vertical"]}   {displayName} ";
                        content.Add(new ColoredText($"{display.PadRight(innerWidth + 1)}{boxChars["vertical"]}", isSelected ? classColors[className] : ConsoleColor.Gray));
                    }

                    // Class description
                    content.Add(new ColoredText($"{boxChars["vertical"]}{horizontalLine}{boxChars["vertical"]}", ConsoleColor.Gray));
                    string desc = classDescriptions[currentClassIndex];
                    content.Add(new ColoredText($"{boxChars["vertical"]} {desc.PadRight(innerWidth - 1)}{boxChars["vertical"]}", ConsoleColor.Gray));
                    content.Add(new ColoredText($"{boxChars["bottomLeft"]}{horizontalLine}{boxChars["bottomRight"]}", ConsoleColor.Gray));
                    content.Add("");

                    // Attributes Section
                    content.Add(new ColoredText($"{boxChars["topLeft"]}{horizontalLine}{boxChars["topRight"]}", ConsoleColor.Gray));
                    string pointsText = state.Localization.GetString("CharacterCreation_AttributePoints", attributePoints);
                    content.Add(new ColoredText($"{boxChars["vertical"]} {pointsText.PadRight(innerWidth - 1)}{boxChars["vertical"]}",
                        attributePoints > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkYellow));
                    content.Add(new ColoredText($"{boxChars["vertical"]}{horizontalLine}{boxChars["vertical"]}", ConsoleColor.Gray));

                    for (int i = 0; i < attributeNames.Length; i++)
                    {
                        string attribute = attributeNames[i];
                        int value = attributes[attribute];
                        bool isSelected = i == selectedAttribute;

                        int barWidth = 20; // Fixed width for consistency
                        int filledBlocks = (int)((value - 5) * (barWidth / 15.0)); // Scale 5-20 to 0-barWidth
                        string bar = new string('■', filledBlocks) + new string('□', barWidth - filledBlocks);

                        string display = isSelected ?
                            $"{boxChars["vertical"]} ▶ {attribute,-13} {value,2} {bar}" :
                            $"{boxChars["vertical"]}   {attribute,-13} {value,2} {bar}";

                        display = $"{display.PadRight(innerWidth + 1)}{boxChars["vertical"]}";

                        ConsoleColor barColor = value switch
                        {
                            >= 18 => ConsoleColor.DarkGreen,
                            >= 14 => ConsoleColor.DarkCyan,
                            >= 10 => ConsoleColor.DarkYellow,
                            _ => ConsoleColor.DarkRed
                        };
                        content.Add(new ColoredText(display, isSelected ? ConsoleColor.Gray : barColor));
                    }

                    content.Add(new ColoredText($"{boxChars["bottomLeft"]}{horizontalLine}{boxChars["bottomRight"]}", ConsoleColor.Gray));
                    content.Add("");

                    // Controls
                    content.Add(new ColoredText(state.Localization.GetString("CharacterCreation_Controls"), ConsoleColor.Gray));
                    content.Add(new ColoredText(state.Localization.GetString("CharacterCreation_ConfirmPrompt"), ConsoleColor.Gray));

                    manager.RenderWrappedText(region, content);
                }
            };

            void UpdateLayout()
            {
                manager.UpdateRegion("Creation", r =>
                {
                    r.X = 1;
                    r.Y = 1;
                    r.Width = Console.WindowWidth - 2;
                    r.Height = Console.WindowHeight - 2;
                });
            }

            manager.AddRegion("Creation", creationRegion);
            UpdateLayout();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedAttribute = (selectedAttribute - 1 + attributeNames.Length) % attributeNames.Length;
                            manager.QueueRender();
                            break;

                        case ConsoleKey.DownArrow:
                            selectedAttribute = (selectedAttribute + 1) % attributeNames.Length;
                            manager.QueueRender();
                            break;

                        case ConsoleKey.LeftArrow:
                            if (key.Modifiers == ConsoleModifiers.Control)
                            {
                                // Cycle class selection left
                                int currentIndex = Array.IndexOf(classOptions, selectedClass);
                                currentIndex = (currentIndex - 1 + classOptions.Length) % classOptions.Length;
                                selectedClass = classOptions[currentIndex];
                            }
                            else if (attributes[attributeNames[selectedAttribute]] > 5)
                            {
                                // Decrease selected attribute
                                attributes[attributeNames[selectedAttribute]]--;
                                attributePoints++;
                            }
                            manager.QueueRender();
                            break;

                        case ConsoleKey.RightArrow:
                            if (key.Modifiers == ConsoleModifiers.Control)
                            {
                                // Cycle class selection right
                                int currentIndex = Array.IndexOf(classOptions, selectedClass);
                                currentIndex = (currentIndex + 1) % classOptions.Length;
                                selectedClass = classOptions[currentIndex];
                            }
                            else if (attributePoints > 0 && attributes[attributeNames[selectedAttribute]] < 20)
                            {
                                // Increase selected attribute
                                attributes[attributeNames[selectedAttribute]]++;
                                attributePoints--;
                            }
                            manager.QueueRender();
                            break;

                        case ConsoleKey.Enter:
                            try
                            {
                                return characterFactory.CreateCharacter(selectedClass, state.PlayerName, attributes);
                            }
                            catch (Exception ex)
                            {
                                state.GameLog.Add(new ColoredText($"Error creating character: {ex.Message}", ConsoleColor.Red));
                                return null;
                            }

                        case ConsoleKey.Escape:
                            return null;
                    }
                }

                if (manager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(16);
            }
        }
    }
}
