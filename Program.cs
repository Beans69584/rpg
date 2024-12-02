using System.Text;
using RPG;
using RPG.Commands;

public class GameState
{
    public List<string> GameLog { get; } = new();
    public ConsoleWindowManager WindowManager { get; }
    public string PlayerName { get; set; } = "Hero";
    public int Level { get; set; } = 1;
    public int HP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public CommandHandler CommandHandler { get; }
    public bool Running { get; set; } = true;

    public GameState(ConsoleWindowManager manager)
    {
        WindowManager = manager;
        Localization = new LocalizationManager();
        Localization.SetLanguage(GameSettings.CurrentLanguage); // Use global setting
        CommandHandler = new CommandHandler();
        GameLog.Add(Localization.GetString("Welcome_Message"));
        GameLog.Add(Localization.GetString("Help_Hint"));
    }

    public LocalizationManager Localization { get; }
    public string CurrentLanguage
    {
        get => GameSettings.CurrentLanguage;
        set => GameSettings.CurrentLanguage = value;
    }


}
class Program
{
    private const int MIN_WIDTH = 80;
    private const int MIN_HEIGHT = 24;

    static async Task Main(string[] args)
    {
        while (true)
        {
            if (!CheckMinimumSize())
            {
                await ShowSizeError();
                continue;
            }

            var choice = await ShowMainMenu();
            switch (choice)
            {
                case 1: // Start New Game
                    await StartGame();
                    break;
                case 2: // Load Game
                    // Placeholder for load game functionality
                    break;
                case 3: // Options
                    await ShowOptionsMenu();
                    break;
                case 4: // Credits
                    // Placeholder for credits screen
                    break;
                case 5: // Exit
                    return;
            }
        }
    }

    private static bool CheckMinimumSize()
    {
        return Console.WindowWidth >= MIN_WIDTH && Console.WindowHeight >= MIN_HEIGHT;
    }

    private static async Task ShowSizeError()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var errorRegion = new Region
        {
            Name = state.Localization.GetString("Error_Title"),
            BorderColor = ConsoleColor.Red,
            TitleColor = ConsoleColor.Red,
            RenderContent = (region) =>
            {
                var message = new List<string>
                {
                    "",
                    state.Localization.GetString("Error_WindowTooSmall"),
                    "",
                    state.Localization.GetString("Error_MinimumSize", MIN_WIDTH, MIN_HEIGHT),
                    state.Localization.GetString("Error_CurrentSize", Console.WindowWidth, Console.WindowHeight),
                    "",
                    state.Localization.GetString("Error_ResizePrompt"),
                    state.Localization.GetString("Error_ExitPrompt"),
                    "",
                    state.Localization.GetString("Error_ResizeTip")
                };
                manager.RenderWrappedText(region, message, ConsoleColor.White);
            }
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("Error", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - 2;
                r.Height = Console.WindowHeight - 2;
            });
        }

        manager.AddRegion("Error", errorRegion);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        if (CheckMinimumSize())
                            return;
                        break;
                    case ConsoleKey.Escape:
                        Environment.Exit(0);
                        break;
                }
            }

            if (manager.CheckResize())
            {
                UpdateLayout();
            }

            await Task.Delay(16);
        }
    }

    private static async Task<int> ShowMainMenu()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var choice = 1;
        const string version = "v0.1.0";

        // Update the mainMenu region to use localized strings
        var mainMenu = new Region
        {
            Name = state.Localization.GetString("MainMenu_Title", version),
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var logo = new List<string>
                {
                    @"______                      ____________ _____ ",
                    @"|  _  \                     | ___ \ ___ \  __ \",
                    @"| | | |___ _ __ ___   ___   | |_/ / |_/ / |  \/",
                    @"| | | / _ \ '_ ` _ \ / _ \  |    /|  __/| | __ ",
                    @"| |/ /  __/ | | | | | (_) | | |\ \| |   | |_\ \",
                    @"|___/ \___|_| |_| |_|\___/  \_| \_\_|    \____/"
                };

                var menuItems = new List<string>
                {
                    "",
                    "",
                };
                menuItems.AddRange(logo);
                menuItems.AddRange(new[]
                {
                    "",
                    state.Localization.GetString("MainMenu_NavigationHint"),
                    "",
                    choice == 1 ? $"> [{state.Localization.GetString("MainMenu_Start")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Start")}  ",
                    choice == 2 ? $"> [{state.Localization.GetString("MainMenu_Load")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Load")}  ",
                    choice == 3 ? $"> [{state.Localization.GetString("MainMenu_Options")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Options")}  ",
                    choice == 4 ? $"> [{state.Localization.GetString("MainMenu_Credits")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Credits")}  ",
                    choice == 5 ? $"> [{state.Localization.GetString("MainMenu_Exit")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Exit")}  ",
                    "",
                    "",
                    state.Localization.GetString("MainMenu_Copyright", DateTime.Now.Year),
                    state.Localization.GetString("MainMenu_CreatedFor")
                });

                manager.RenderWrappedText(region, menuItems, ConsoleColor.White);
            }
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("MainMenu", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - 2;
                r.Height = Console.WindowHeight - 2;
            });
        }

        manager.AddRegion("MainMenu", mainMenu);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        choice = choice == 1 ? 5 : choice - 1;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.DownArrow:
                        choice = choice == 5 ? 1 : choice + 1;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.Enter:
                        return choice;
                }
            }

            if (manager.CheckResize())
            {
                UpdateLayout();
            }

            await Task.Delay(16);
        }
    }

    private static async Task StartGame()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        SetupCommands(state);

        // Create layout regions with relative positioning
        void UpdateLayout()
        {
            manager.UpdateRegion("GameLog", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - 42;
                r.Height = Console.WindowHeight - 6;
            });

            manager.UpdateRegion("Stats", r =>
            {
                r.X = Console.WindowWidth - 40;
                r.Y = 1;
                r.Width = 39;
                r.Height = Console.WindowHeight - 6;
            });

            manager.UpdateRegion("Input", r =>
            {
                r.X = 1;
                r.Y = Console.WindowHeight - 4;
                r.Width = Console.WindowWidth - 2;
                r.Height = 3;
            });
        }

        var gameLog = new Region
        {
            Name = "Game Log",
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                manager.RenderWrappedText(region, state.GameLog.TakeLast(50), ConsoleColor.White);
            }
        };

        var statsPanel = new Region
        {
            Name = "Character Stats",
            BorderColor = ConsoleColor.Green,
            TitleColor = ConsoleColor.Green,
            RenderContent = (region) =>
            {
                var stats = new List<string>
                {
                    $"Player: {state.PlayerName}",
                    $"Level: {state.Level}",
                    $"HP: {state.HP}/{state.MaxHP}",
                    "Equipment:",
                    "- Rusty Sword",
                    "- Leather Armor",
                    "Gold: 100"
                };
                manager.RenderWrappedText(region, stats, ConsoleColor.White);
            }
        };

        var inputRegion = new Region
        {
            Name = "Input",
            BorderColor = ConsoleColor.Yellow,
            TitleColor = ConsoleColor.Yellow
        };

        // Add regions and set initial layout
        manager.AddRegion("GameLog", gameLog);
        manager.AddRegion("Stats", statsPanel);
        manager.AddRegion("Input", inputRegion);
        UpdateLayout();

        // Input handling
        var inputBuffer = new StringBuilder();

        while (state.Running)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        var command = inputBuffer.ToString().Trim();
                        if (!string.IsNullOrEmpty(command))
                        {
                            ProcessCommand(command, state);
                        }
                        inputBuffer.Clear();
                        manager.UpdateInputText("", ConsoleColor.White);
                        break;

                    case ConsoleKey.Backspace:
                        if (inputBuffer.Length > 0)
                        {
                            inputBuffer.Length = inputBuffer.Length - 1;
                            manager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.White);
                        }
                        break;

                    case ConsoleKey.Escape:
                        return;

                    default:
                        if (!char.IsControl(key.KeyChar) && inputBuffer.Length < 100)
                        {
                            inputBuffer.Append(key.KeyChar);
                            manager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.White);
                        }
                        break;
                }

                manager.QueueRender(); // Force a refresh after each keystroke
            }

            // Check for resize
            if (manager.CheckResize())
            {
                UpdateLayout();
            }

            await Task.Delay(16);
        }
    }

    private static void ProcessCommand(string input, GameState state)
    {
        if (!state.CommandHandler.ExecuteCommand(input, state))
        {
            state.GameLog.Add($"Unknown command: {input.Split(' ')[0]}");
        }
    }

    private static void SetupCommands(GameState state)
    {
        var commandHandler = state.CommandHandler;

        // Register built-in commands
        commandHandler.RegisterCommand(new HelpCommand(commandHandler));

        // Load Lua commands
        var luaLoader = new LuaCommandLoader("Commands/Lua", state);
        foreach (var command in luaLoader.LoadCommands())
        {
            commandHandler.RegisterCommand(command);
        }
    }

    private static async Task ShowOptionsMenu()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var currentOption = 1;
        var languages = LocalizationManager.GetAvailableLanguages().ToList();
        var currentLanguageIndex = languages.FindIndex(c => c.Name == GameSettings.CurrentLanguage);

        // Subscribe to language changes
        state.Localization.LanguageChanged += (newLanguage) =>
        {
            // Force refresh of all UI elements
            manager.QueueRender();
            // Save settings when language changes
            GameSettings.Instance.Save();
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("Options", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - 2;
                r.Height = Console.WindowHeight - 2;
            });
        }

        var optionsMenu = new Region
        {
            Name = state.Localization.GetString("Options_Title"),
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var options = new List<string>
                {
                    "",
                    state.Localization.GetString("Options_Instructions"),
                    "",
                    currentOption == 1 ?
                        $"> [{state.Localization.GetString("Options_Language")}]: {languages[currentLanguageIndex].DisplayName} <" :
                        $"  {state.Localization.GetString("Options_Language")}: {languages[currentLanguageIndex].DisplayName}  ",
                    "",
                    currentOption == 2 ?
                        $"> [{state.Localization.GetString("Options_Back")}] <" :
                        $"  {state.Localization.GetString("Options_Back")}  "
                };

                manager.RenderWrappedText(region, options, ConsoleColor.White);
            }
        };

        manager.AddRegion("Options", optionsMenu);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        currentOption = currentOption == 1 ? 2 : 1;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.DownArrow:
                        currentOption = currentOption == 2 ? 1 : 2;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.LeftArrow:
                        if (currentOption == 1)
                        {
                            currentLanguageIndex = (currentLanguageIndex - 1 + languages.Count) % languages.Count;
                            GameSettings.CurrentLanguage = languages[currentLanguageIndex].Name;
                            state.Localization.SetLanguage(GameSettings.CurrentLanguage);
                            manager.QueueRender();
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (currentOption == 1)
                        {
                            currentLanguageIndex = (currentLanguageIndex + 1) % languages.Count;
                            GameSettings.CurrentLanguage = languages[currentLanguageIndex].Name;
                            state.Localization.SetLanguage(GameSettings.CurrentLanguage);
                            manager.QueueRender();
                        }
                        break;
                    case ConsoleKey.Enter:
                        if (currentOption == 2)
                            return;
                        break;
                    case ConsoleKey.Escape:
                        return;
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