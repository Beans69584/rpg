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
        // Add these lines at the start of Main
        Console.OutputEncoding = Encoding.UTF8;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // Try to set UTF-8 codepage
                Console.OutputEncoding = Encoding.UTF8;
                var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x4); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
            }
            catch
            {
                // Fallback to ASCII if UTF-8 setup fails
                GameSettings.Instance.Display.UseUnicodeBorders = false;
                GameSettings.Instance.Save();
            }
        }

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
                    await StartGame(null);
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

    private static async Task StartGame(string? loadSlot = null)
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);

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
            int mapHeight = Console.WindowHeight - 6;
            int mapWidth = 40;

            manager.UpdateRegion("GameLog", r =>
            {
                r.X = 1;
                r.Y = 1;
                r.Width = Console.WindowWidth - mapWidth - 42;
                r.Height = mapHeight;
            });

            manager.UpdateRegion("Map", r =>
            {
                r.X = Console.WindowWidth - mapWidth - 41;
                r.Y = 1;
                r.Width = mapWidth;
                r.Height = mapHeight / 2;
            });

            manager.UpdateRegion("RegionMap", r =>
            {
                r.X = Console.WindowWidth - mapWidth - 41;
                r.Y = mapHeight / 2 + 2;
                r.Width = mapWidth;
                r.Height = mapHeight / 2 - 1;
            });

            manager.UpdateRegion("Stats", r =>
            {
                r.X = Console.WindowWidth - 40;
                r.Y = 1;
                r.Width = 39;
                r.Height = mapHeight;
            });

            manager.UpdateRegion("Input", r =>
            {
                r.X = 1;
                r.Y = Console.WindowHeight - 4;
                r.Width = Console.WindowWidth - 2;
                r.Height = 3;
            });
        }

        var worldMap = new Region
        {
            Name = "World Map",
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                manager.RenderMap(region, state.World.GetWorldData(), state.CurrentRegion);
            }
        };

        var regionMap = new Region
        {
            Name = "Region Map",
            BorderColor = ConsoleColor.Green,
            TitleColor = ConsoleColor.Green,
            RenderContent = (region) =>
            {
                manager.RenderRegionMap(region, state.CurrentRegion);
            }
        };

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
        manager.AddRegion("Map", worldMap);
        manager.AddRegion("RegionMap", regionMap);
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
    
    private static async Task ShowOptionsMenu()
    {
        using var manager = new ConsoleWindowManager();
        var state = new GameState(manager);
        var currentOption = 1;
        var languages = LocalizationManager.GetAvailableLanguages().ToList();
        var currentLanguageIndex = languages.FindIndex(c => c.Name == GameSettings.CurrentLanguage);
        var settings = GameSettings.Instance;
        const int MAX_OPTIONS = 7; // Updated number of options

        // FPS options (will be converted to refresh rate ms)
        int[] fpsOptions = { 30, 60, 120 };
        int currentFpsIndex = Array.FindIndex(fpsOptions, fps => 1000 / fps == settings.Display.RefreshRateMs);
        if (currentFpsIndex == -1) currentFpsIndex = 1; // Default to 60 FPS if current setting isn't in list

        state.Localization.LanguageChanged += (newLanguage) =>
        {
            manager.QueueRender();
            settings.Save();
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
                    $"   {state.Localization.GetString("Settings_General")}",
                    "   ===============",
                    "",
                    currentOption == 1 ?
                        $" > {state.Localization.GetString("Settings_Language_Label", languages[currentLanguageIndex].DisplayName)} [←/→]" :
                        $"   {state.Localization.GetString("Settings_Language_Label", languages[currentLanguageIndex].DisplayName)}",
                    "",
                    "",
                    $"   {state.Localization.GetString("Settings_Visual")}",
                    "   ===============",
                    "",
                    currentOption == 2 ?
                        $" > {state.Localization.GetString("Settings_Colors_Toggle", settings.Display.UseColors ? state.Localization.GetString("Settings_Disable") : state.Localization.GetString("Settings_Enable"))}" :
                        $"   {state.Localization.GetString("Settings_Colors_Label", settings.Display.UseColors ? state.Localization.GetString("Settings_On") : state.Localization.GetString("Settings_Off"))}",
                    "",
                    currentOption == 3 ?
                        $" > {state.Localization.GetString("Settings_BorderStyle_Label", GetBorderStyleDisplay(settings.Display, state))} [←/→]" :
                        $"   {state.Localization.GetString("Settings_BorderStyle_Label", GetBorderStyleDisplay(settings.Display, state))}",
                    "",
                    "",
                    $"   {state.Localization.GetString("Settings_Cursor")}",
                    "   ===============",
                    "",
                    currentOption == 4 ?
                        $" > {state.Localization.GetString("Settings_CursorBlink_Toggle", settings.Display.EnableCursorBlink ? state.Localization.GetString("Settings_Disable") : state.Localization.GetString("Settings_Enable"))}" :
                        $"   {state.Localization.GetString("Settings_CursorBlink_Label", settings.Display.EnableCursorBlink ? state.Localization.GetString("Settings_On") : state.Localization.GetString("Settings_Off"))}",
                    "",
                    currentOption == 5 ?
                        $" > {state.Localization.GetString("Settings_BlinkSpeed_Label", GetBlinkSpeedDisplay(settings.Display.CursorBlinkRateMs, state))} [←/→]" :
                        $"   {state.Localization.GetString("Settings_BlinkSpeed_Label", GetBlinkSpeedDisplay(settings.Display.CursorBlinkRateMs, state))}",
                    "",
                    "",
                    $"   {state.Localization.GetString("Settings_Performance")}",
                    "   ===========",
                    "",
                    currentOption == 6 ?
                        $" > {state.Localization.GetString("Settings_FrameRate_Label", fpsOptions[currentFpsIndex])} [←/→]" :
                        $"   {state.Localization.GetString("Settings_FrameRate_Label", fpsOptions[currentFpsIndex])}",
                    "",
                    "",
                    currentOption == 7 ?
                        $" > [{state.Localization.GetString("Settings_BackToMainMenu")}]" :
                        $"   {state.Localization.GetString("Settings_BackToMainMenu")}",
                    "",
                    "",
                    state.Localization.GetString("Settings_NavigationHint")
                };

                manager.RenderWrappedText(region, options, ConsoleColor.White);
            }
        };

        // Helper function to display border style
        static string GetBorderStyleDisplay(ConsoleDisplayConfig config, GameState state)
        {
            if (!config.UseUnicodeBorders) 
                return state.Localization.GetString("BorderStyle_ASCII");
            return config.UseCurvedBorders ? 
                state.Localization.GetString("BorderStyle_Curved") : 
                state.Localization.GetString("BorderStyle_Classic");
        }

        // Helper function to display blink speed
        static string GetBlinkSpeedDisplay(int ms, GameState state)
        {
            return ms switch
            {
                <= 250 => state.Localization.GetString("BlinkSpeed_Fast"),
                <= 400 => state.Localization.GetString("BlinkSpeed_Normal"),
                <= 700 => state.Localization.GetString("BlinkSpeed_Slow"),
                _ => state.Localization.GetString("BlinkSpeed_VerySlow")
            };
        }

        manager.AddRegion("Options", optionsMenu);
        UpdateLayout();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                bool settingsChanged = false;

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        currentOption = currentOption == 1 ? MAX_OPTIONS : currentOption - 1;
                        manager.QueueRender();
                        break;

                    case ConsoleKey.DownArrow:
                        currentOption = currentOption == MAX_OPTIONS ? 1 : currentOption + 1;
                        manager.QueueRender();
                        break;

                    case ConsoleKey.Spacebar:
                    case ConsoleKey.Enter:
                        settingsChanged = true;
                        switch (currentOption)
                        {
                            case 2: // Colors toggle
                                settings.Display.UseColors = !settings.Display.UseColors;
                                manager.UpdateDisplaySettings(settings.Display);
                                break;
                            case 4: // Cursor Blink toggle
                                settings.Display.EnableCursorBlink = !settings.Display.EnableCursorBlink;
                                manager.UpdateDisplaySettings(settings.Display);
                                break;
                            case 7: // Back
                                if (settingsChanged)
                                    settings.Save();
                                return;
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        settingsChanged = true;
                        switch (currentOption)
                        {
                            case 1: // Language cycling
                                if (key.Key == ConsoleKey.LeftArrow)
                                    currentLanguageIndex = (currentLanguageIndex - 1 + languages.Count) % languages.Count;
                                else
                                    currentLanguageIndex = (currentLanguageIndex + 1) % languages.Count;
                                
                                var newLanguage = languages[currentLanguageIndex].Name;
                                settings.UpdateLanguage(newLanguage);  // Update the language in settings
                                state.Localization.SetLanguage(newLanguage);  // Apply the language change
                                break;

                            case 3: // Border Style cycling
                                CycleBorderStyle(settings.Display, key.Key == ConsoleKey.RightArrow);
                                manager.UpdateDisplaySettings(settings.Display);
                                break;

                            case 5: // Blink Speed
                                if (key.Key == ConsoleKey.LeftArrow)
                                    settings.Display.CursorBlinkRateMs = Math.Max(200, settings.Display.CursorBlinkRateMs - 150);
                                else
                                    settings.Display.CursorBlinkRateMs = Math.Min(1000, settings.Display.CursorBlinkRateMs + 150);
                                manager.UpdateDisplaySettings(settings.Display);
                                break;

                            case 6: // Frame Rate
                                if (key.Key == ConsoleKey.LeftArrow)
                                    currentFpsIndex = (currentFpsIndex - 1 + fpsOptions.Length) % fpsOptions.Length;
                                else
                                    currentFpsIndex = (currentFpsIndex + 1) % fpsOptions.Length;
                                settings.Display.RefreshRateMs = 1000 / fpsOptions[currentFpsIndex];
                                manager.UpdateDisplaySettings(settings.Display);
                                break;
                        }
                        break;

                    case ConsoleKey.Escape:
                        if (settingsChanged)
                            settings.Save();
                        return;
                }
                
                if (settingsChanged)
                {
                    settings.Save();
                    manager.QueueRender();
                }
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
                    menuItems.Add($"     {state.Localization.GetString("Load_PlayTime", FormatPlayTime(saveInfo.Metadata.TotalPlayTime, state))}");
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
                        await StartGame(slot);
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

    private static string FormatPlayTime(TimeSpan time, GameState state)
    {
        if (time.TotalDays >= 1)
            return state.Localization.GetString("PlayTime_Days", 
                (int)time.TotalDays, time.Hours, time.Minutes);
        if (time.TotalHours >= 1)
            return state.Localization.GetString("PlayTime_Hours",
                (int)time.TotalHours, time.Minutes);
        return state.Localization.GetString("PlayTime_Minutes",
            time.Minutes, time.Seconds);
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

    private static void CycleBorderStyle(ConsoleDisplayConfig config, bool forward)
    {
        // Cycle order: ASCII -> Unicode Classic -> Unicode Curved -> ASCII
        if (forward)
        {
            if (!config.UseUnicodeBorders)
            {
                config.UseUnicodeBorders = true;
                config.UseCurvedBorders = false;
            }
            else if (!config.UseCurvedBorders)
            {
                config.UseCurvedBorders = true;
            }
            else
            {
                config.UseUnicodeBorders = false;
                config.UseCurvedBorders = false;
            }
        }
        else
        {
            if (config.UseCurvedBorders)
            {
                config.UseCurvedBorders = false;
            }
            else if (config.UseUnicodeBorders)
            {
                config.UseUnicodeBorders = false;
                config.UseCurvedBorders = false;
            }
            else
            {
                config.UseUnicodeBorders = true;
                config.UseCurvedBorders = true;
            }
        }
    }

    // Add these P/Invoke declarations at the bottom of the Program class
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);
}