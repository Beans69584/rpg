using System.Text;
using System.IO.Compression;
using RPG;
using RPG.Commands;
using RPG.Plugins;
using System.Text.Json;

class Program
{
    private const int MIN_WIDTH = 80;
    private const int MIN_HEIGHT = 24;

    static async Task Main(string[] args)
    {
        // Create plugins directory if it doesn't exist
        Directory.CreateDirectory("./plugins");
        Directory.CreateDirectory("./plugins/lua");
        Directory.CreateDirectory("./plugins/native");

        // Initialize plugin manager early
        var pluginManager = new PluginManager();

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
                    await StartGame(null, pluginManager);
                    break;
                case 2: // Load Game
                    await ShowLoadGameMenu(pluginManager);
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

        var mainMenu = new Region
        {
            Name = state.Localization.GetString("MainMenu_Title", version),
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var logo = new List<string>
                {
                    @"██████╗ ███████╗███╗   ███╗ ██████╗ ",
                    @"██╔══██╗██╔════╝████╗ ████║██╔═══██╗",
                    @"██║  ██║█████╗  ██╔████╔██║██║   ██║",
                    @"██║  ██║██╔══╝  ██║╚██╔╝██║██║   ██║",
                    @"██████╔╝███████╗██║ ╚═╝ ██║╚██████╔╝",
                    @"╚═════╝ ╚══════╝╚═╝     ╚═╝ ╚═════╝ "
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

    private static async Task StartGame(string? loadSlot = null, PluginManager pluginManager = null)
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);

        // Initialize command handler with plugin manager
        state.CommandHandler.Initialize(pluginManager);

        // Create plugin context and load plugins
        var context = new PluginContext(state, manager, state.CommandHandler, pluginManager);
        pluginManager?.Initialize(context);
        await LoadPlugins(context);

        if (loadSlot == null)
        {
            try
            {
                if (!File.Exists("./World/world.dat"))
                {
                    state.GameLog.Add("Generating new world...");

                    var generator = new ProceduralWorldGenerator(Random.Shared.Next());
                    var worldConfig = generator.GenerateWorld();

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
        else if (!state.LoadGame(loadSlot))
        {
            state.GameLog.Add($"Failed to load save: {loadSlot}");
            await Task.Delay(2000);
            return;
        }

        SetupCommands(state);

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

        manager.AddRegion("GameLog", gameLog);
        manager.AddRegion("Stats", statsPanel);
        manager.AddRegion("Input", inputRegion);
        UpdateLayout();

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
                            await ProcessCommand(command, state);
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

                manager.QueueRender();
            }

            if (manager.CheckResize())
            {
                UpdateLayout();
            }

            await Task.Delay(16);
        }

        // Cleanup plugins when game ends
        UnloadPlugins(context);
    }

    private static async Task ProcessCommand(string input, GameState state)
    {
        await state.CommandHandler.ExecuteCommand(input, state);
    }

    private static void SetupCommands(GameState state)
    {
        var commandHandler = state.CommandHandler;

        commandHandler.RegisterCommand(new HelpCommand(commandHandler));
        commandHandler.RegisterCommand(new SaveCommand());
        commandHandler.RegisterCommand(new LoadCommand());

        var luaLoader = new LuaCommandLoader(state);
        foreach (var command in luaLoader.LoadCommands())
        {
            commandHandler.RegisterCommand(command);
        }
    }

    private static async Task LoadPlugins(PluginContext context)
    {
        try
        {
            string luaPath = Path.GetFullPath("./plugins/lua");
            string nativePath = Path.GetFullPath("./plugins/native");

            context.LogDebug($"Loading plugins from:");
            context.LogDebug($"Lua Path: {luaPath}");
            context.LogDebug($"Native Path: {nativePath}");

            // Similar for Lua plugins...
            if (Directory.Exists("./plugins/lua"))
            {
                var luaFiles = Directory.GetFiles("./plugins/lua", "*.lua");
                context.LogDebug($"Found {luaFiles.Length} Lua plugins");
                foreach (var file in luaFiles)
                {
                    context.LogDebug($"Loading Lua plugin: {Path.GetFileName(file)}");
                    context.PluginManager.LoadPlugin(file);
                }
            }
            else
            {
                context.LogDebug("Lua plugins directory not found");
            }

            // Initialize plugins
            var plugins = context.PluginManager.GetPlugins().ToList();
            context.LogDebug($"Initializing {plugins.Count} plugins");
            foreach (var plugin in plugins)
            {
                try
                {
                    context.LogDebug($"Initializing plugin: {plugin.Name}");
                    plugin.Initialize(context);
                }
                catch (Exception ex)
                {
                    context.LogDebug($"Failed to initialize plugin {plugin.Name}: {ex.Message}");
                    context.LogDebug($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            context.LogDebug($"Error loading plugins: {ex.Message}");
            context.LogDebug($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private static void UnloadPlugins(PluginContext context)
    {
        foreach (var plugin in context.PluginManager.GetPlugins())
        {
            try
            {
                plugin.Shutdown();
            }
            catch (Exception ex)
            {
                context.LogDebug($"Error shutting down plugin {plugin.Name}: {ex.Message}");
            }
        }
    }

    private static async Task ShowOptionsMenu()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var currentOption = 1;
        var languages = LocalizationManager.GetAvailableLanguages().ToList();
        var currentLanguageIndex = languages.FindIndex(c => c.Name == GameSettings.CurrentLanguage);

        state.Localization.LanguageChanged += (newLanguage) =>
        {
            manager.QueueRender();
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

    private static async Task ShowLoadGameMenu(PluginManager pluginManager)
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);

        var saves = SaveManager.GetSaveFiles();
        if (!saves.Any())
        {
            var messageBox = new Region
            {
                Name = state.Localization.GetString("Load_Title"),
                BorderColor = ConsoleColor.Yellow,
                TitleColor = ConsoleColor.Yellow,
                RenderContent = (region) =>
                {
                    var lines = new List<string>
                    {
                        "",
                        state.Localization.GetString("Load_NoSaves"),
                        "",
                        "Press any key to return..."
                    };
                    manager.RenderWrappedText(region, lines, ConsoleColor.White);
                }
            };

            void UpdateMessageLayout()
            {
                manager.UpdateRegion("Message", r =>
                {
                    r.X = Console.WindowWidth / 4;
                    r.Y = Console.WindowHeight / 3;
                    r.Width = Console.WindowWidth / 2;
                    r.Height = 6;
                });
            }

            manager.AddRegion("Message", messageBox);
            UpdateMessageLayout();

            while (!Console.KeyAvailable)
            {
                if (manager.CheckResize())
                {
                    UpdateMessageLayout();
                }
                await Task.Delay(16);
            }
            Console.ReadKey(true);
            return;
        }

        var selectedIndex = 0;
        const int itemsPerPage = 5;
        var currentPage = 0;
        var totalPages = (int)Math.Ceiling(saves.Count / (double)itemsPerPage);

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
                    state.Localization.GetString("Load_DeleteInstructions"),
                    ""
                };

                var pageStart = currentPage * itemsPerPage;
                var pageSaves = saves.Skip(pageStart).Take(itemsPerPage).ToList();

                for (int i = 0; i < pageSaves.Count; i++)
                {
                    var saveInfo = pageSaves[i];
                    var index = pageStart + i;
                    var saveType = saveInfo.Metadata.SaveType == SaveType.Autosave ? "[Auto]" : "[Manual]";
                    var saveDate = saveInfo.Metadata.SaveTime.ToString("yyyy-MM-dd HH:mm");

                    menuItems.Add(index == selectedIndex ?
                        $"> [{saveInfo.Slot}] {saveType} {saveDate}" :
                        $"  {saveInfo.Slot}: {saveType} {saveDate}");
                    menuItems.Add($"     {saveInfo.Metadata.LastPlayedCharacter} - Level {saveInfo.Metadata.CharacterLevel}");
                    menuItems.Add($"     Play time: {FormatPlayTime(saveInfo.Metadata.TotalPlayTime)}");
                    menuItems.Add("");
                }

                if (totalPages > 1)
                {
                    menuItems.Add("");
                    menuItems.Add($"Page {currentPage + 1} of {totalPages}");
                    menuItems.Add("Use Left/Right arrows to navigate pages");
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
                        currentPage = selectedIndex / itemsPerPage;
                        manager.QueueRender();
                        break;

                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % saves.Count;
                        currentPage = selectedIndex / itemsPerPage;
                        manager.QueueRender();
                        break;

                    case ConsoleKey.LeftArrow:
                        if (totalPages > 1)
                        {
                            currentPage = (currentPage - 1 + totalPages) % totalPages;
                            selectedIndex = currentPage * itemsPerPage;
                            manager.QueueRender();
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (totalPages > 1)
                        {
                            currentPage = (currentPage + 1) % totalPages;
                            selectedIndex = currentPage * itemsPerPage;
                            manager.QueueRender();
                        }
                        break;

                    case ConsoleKey.Delete:
                        var (slotToDelete, _) = saves[selectedIndex];
                        if (await ConfirmDelete(slotToDelete))
                        {
                            SaveManager.DeleteSave(slotToDelete);
                            saves = SaveManager.GetSaveFiles();
                            if (!saves.Any())
                            {
                                manager.UpdateRegion("LoadMenu", r => r.IsVisible = false);
                                var messageBox = new Region
                                {
                                    Name = state.Localization.GetString("Load_Title"),
                                    BorderColor = ConsoleColor.Yellow,
                                    TitleColor = ConsoleColor.Yellow,
                                    RenderContent = (region) =>
                                    {
                                        var lines = new List<string>
                                        {
                                            "",
                                            state.Localization.GetString("Load_NoSaves"),
                                            "",
                                            "Press any key to return..."
                                        };
                                        manager.RenderWrappedText(region, lines, ConsoleColor.White);
                                    }
                                };

                                void UpdateMessageLayout()
                                {
                                    manager.UpdateRegion("Message", r =>
                                    {
                                        r.X = Console.WindowWidth / 4;
                                        r.Y = Console.WindowHeight / 3;
                                        r.Width = Console.WindowWidth / 2;
                                        r.Height = 6;
                                    });
                                }

                                manager.AddRegion("Message", messageBox);
                                UpdateMessageLayout();
                                manager.QueueRender();

                                while (!Console.KeyAvailable)
                                {
                                    if (manager.CheckResize())
                                    {
                                        UpdateMessageLayout();
                                    }
                                    await Task.Delay(16);
                                }
                                Console.ReadKey(true);
                                return;
                            }

                            totalPages = (int)Math.Ceiling(saves.Count / (double)itemsPerPage);
                            selectedIndex = Math.Min(selectedIndex, saves.Count - 1);
                            currentPage = selectedIndex / itemsPerPage;
                            manager.QueueRender();
                        }
                        break;

                    case ConsoleKey.Enter:
                        var (slot, _) = saves[selectedIndex];
                        manager.Dispose();
                        await StartGame(slot, pluginManager);
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

    private static string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalDays >= 1)
            return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        return $"{time.Minutes}m {time.Seconds}s";
    }

    private static async Task<bool> ConfirmDelete(string slot)
    {
        using var manager = new ConsoleWindowManager();
        var selected = false;

        var dialog = new Region
        {
            Name = "Confirm",
            BorderColor = ConsoleColor.Red,
            TitleColor = ConsoleColor.Red,
            RenderContent = (region) =>
            {
                var lines = new List<string>
                {
                    "",
                    $"Are you sure you want to delete save {slot}?",
                    "",
                    "This action cannot be undone.",
                    "",
                    selected ? "> [Yes]    No  <" : "  Yes   > [No] <",
                    ""
                };
                manager.RenderWrappedText(region, lines, ConsoleColor.White);
            }
        };

        void UpdateLayout()
        {
            manager.UpdateRegion("Confirm", r =>
            {
                r.X = Console.WindowWidth / 4;
                r.Y = Console.WindowHeight / 3;
                r.Width = Console.WindowWidth / 2;
                r.Height = Console.WindowHeight / 3;
            });
        }

        manager.AddRegion("Confirm", dialog);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        selected = !selected;
                        manager.QueueRender();
                        break;

                    case ConsoleKey.Enter:
                        return selected;

                    case ConsoleKey.Escape:
                        return false;
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