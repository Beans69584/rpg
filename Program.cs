using System.Text;
using System.IO.Compression;
using RPG;
using RPG.Commands;
using System.Text.Json;
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
                    await ShowLoadGameMenu();
                    break;
                case 3: // Options
                    await ShowOptionsMenu();
                    break;
                case 4: // Exit
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
                    choice == 4 ? $"> [{state.Localization.GetString("MainMenu_Exit")}] <" :
                                 $"  {state.Localization.GetString("MainMenu_Exit")}  ",
                    "",
                    "",
                    state.Localization.GetString("MainMenu_Copyright", DateTime.Now.Year),
                    state.Localization.GetString("MainMenu_CreatedFor")
                });

                // Modify menuItems to show available saves
                var saves = SaveManager.GetSaveFiles();
                if (saves.Any())
                {
                    menuItems.Add("");
                    menuItems.Add("Available Saves:");
                    foreach (var (slot, save) in saves)
                    {
                        menuItems.Add($"  {slot}: {save.DisplayName}");
                    }
                }

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
                        choice = choice == 1 ? 4 : choice - 1;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.DownArrow:
                        choice = choice == 4 ? 1 : choice + 1;
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

    private static async Task StartGame(string? loadSlot = null)
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);

        // Load world data first if starting new game
        if (loadSlot == null)
        {
            try
            {
                // Check if world exists, if not generate it
                if (!File.Exists("./World/world.dat"))
                {
                    state.GameLog.Add("Generating new world...");
                    
                    // Generate world using ProceduralWorldGenerator
                    var generator = new ProceduralWorldGenerator(Random.Shared.Next());
                    var worldConfig = generator.GenerateWorld();
                    
                    // Ensure directory exists
                    Directory.CreateDirectory("./World");

                    var builder = new OptimizedWorldBuilder("./World", worldConfig);
                    builder.Build();
                    
                    state.GameLog.Add($"Generated world '{worldConfig.Name}' with {worldConfig.Regions.Count} regions");
                }

                state.LoadWorld("./World/world.dat", true);
            }
            catch (Exception ex)
            {
                state.GameLog.Add($"Failed to load/generate world: {ex.Message}");
                state.GameLog.Add("Press any key to return to menu...");
                Console.ReadKey(true);
                return;
            }
        }
        // For loading saved games, let LoadGame handle world loading
        else if (!state.LoadGame(loadSlot))
        {
            state.GameLog.Add($"Failed to load save: {loadSlot}");
            await Task.Delay(2000);
            return;
        }

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

        // Register save/load commands
        commandHandler.RegisterCommand(new SaveCommand());
        commandHandler.RegisterCommand(new LoadCommand());

        // Load Lua commands
        var luaLoader = new LuaCommandLoader(state);
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

    private static async Task ShowMessageBox(string message)
    {
        using var manager = new ConsoleWindowManager();
        var messageBox = new Region
        {
            Name = "Message",
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var lines = new List<string>
                {
                    "",
                    message,
                    "",
                    "Press any key to continue..."
                };
                manager.RenderWrappedText(region, lines, ConsoleColor.White);
            }
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("Message", r =>
            {
                r.X = Console.WindowWidth / 4;
                r.Y = Console.WindowHeight / 3;
                r.Width = Console.WindowWidth / 2;
                r.Height = Console.WindowHeight / 3;
            });
        }

        manager.AddRegion("Message", messageBox);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                return;
            }

            if (manager.CheckResize())
            {
                UpdateLayout();
            }

            await Task.Delay(16);
        }
    }

    private static async Task ShowLoadGameMenu()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var saves = SaveManager.GetSaveFiles();
        var selectedIndex = 0;

        if (!saves.Any())
        {
            await ShowMessageBox(state.Localization.GetString("Load_NoSaves"));
            return;
        }

        var loadMenu = new Region
        {
            Name = state.Localization.GetString("Load_Title"),
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var menuItems = new List<string>
                {
                    "",
                    state.Localization.GetString("Load_Instructions"),
                    ""
                };

                for (int i = 0; i < saves.Count; i++)
                {
                    var (slot, save) = saves[i];
                    menuItems.Add(i == selectedIndex ?
                        $"> [{slot}] {save.DisplayName}" :
                        $"  {slot}: {save.DisplayName}");
                    menuItems.Add($"     {save.Description}");
                    menuItems.Add("");
                }

                manager.RenderWrappedText(region, menuItems, ConsoleColor.White);
            }
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("LoadMenu", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - 2;
                r.Height = Console.WindowHeight - 2;
            });
        }

        manager.AddRegion("LoadMenu", loadMenu);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + saves.Count) % saves.Count;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % saves.Count;
                        manager.QueueRender();
                        break;
                    case ConsoleKey.Enter:
                        var (slot, _) = saves[selectedIndex];
                        manager.Dispose(); // Dispose of the load menu manager
                        await StartGame(slot); // Start the game
                        return;
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