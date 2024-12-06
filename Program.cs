using System.Text;
using System.IO.Compression;
using RPG.Commands;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Serilog;
using RPG.Utils;

namespace RPG
{
    /// <summary>
    /// The main entry class for the application.
    /// </summary>
    public static class Program
    {
        private const int MIN_WIDTH = 80;
        private const int MIN_HEIGHT = 24;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <seealso cref="Task"/>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task Main()
        {
            try
            {
                // Initialize logger
                Logger.Instance.Information("Application starting...");

                // Add these lines at the start of Main
                Console.OutputEncoding = Encoding.UTF8;
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        // Try to set UTF-8 codepage
                        Console.OutputEncoding = Encoding.UTF8;
                        nint handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
                        GetConsoleMode(handle, out uint mode);
                        SetConsoleMode(handle, mode | 0x4); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
                        Logger.Instance.Debug("Windows console mode configured for UTF-8");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Warning(ex, "Failed to configure Windows console mode for UTF-8");
                        // Fallback to ASCII if UTF-8 setup fails
                        GameSettings.Instance.Display.UseUnicodeBorders = false;
                        GameSettings.Instance.Save();
                    }
                }

                while (true)
                {
                    if (!CheckMinimumSize())
                    {
                        Logger.Instance.Warning("Window size too small: {Width}x{Height}",
                            Console.WindowWidth, Console.WindowHeight);
                        await ShowSizeErrorAsync();
                        continue;
                    }

                    int choice = await ShowMainMenuAsync();
                    switch (choice)
                    {
                        case 1: // Start New Game
                            Logger.Instance.Information("Starting new game");
                            await StartGameAsync(null);
                            break;
                        case 2: // Load Game
                            Logger.Instance.Information("Opening load game menu");
                            await ShowLoadGameMenuAsync();
                            break;
                        case 3: // Options
                            Logger.Instance.Information("Opening options menu");
                            await ShowOptionsMenuAsync();
                            break;
                        case 4: // Exit
                            Logger.Instance.Information("Application shutting down normally");
                            return;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Fatal(ex, "Unhandled exception in main loop");
                throw;
            }
            finally
            {
                Logger.Shutdown();
            }
        }

        private static bool CheckMinimumSize()
        {
            return Console.WindowWidth >= MIN_WIDTH && Console.WindowHeight >= MIN_HEIGHT;
        }

        private static async Task ShowSizeErrorAsync()
        {
            using ConsoleWindowManager manager = new();
            GameState state = new(manager);
            Region errorRegion = new()
            {
                Name = state.Localization.GetString("Error_Title"),
                BorderColor = ConsoleColor.Red,
                TitleColor = ConsoleColor.Red,
                RenderContent = (region) =>
                {
                    List<ColoredText> message =
                    [
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
                    ];
                    manager.RenderWrappedText(region, message);
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
                    ConsoleKeyInfo key = Console.ReadKey(true);
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

        private static async Task<int> ShowMainMenuAsync()
        {
            using ConsoleWindowManager manager = new();
            GameState state = new(manager);
            int choice = 1;
            const string version = "v0.1.0";

            Region mainMenu = new()
            {
                Name = state.Localization.GetString("MainMenu_Title", version),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<string> logo =
                    [
                        @"██████╗ ███████╗███╗   ███╗ ██████╗ ",
                        @"██╔══██╗██╔════╝████╗ ████║██╔═══██╗",
                        @"██║  ██╗█████╗  ██╔████╔██║██║   ██║",
                        @"██║  ██║██╔══╝  ██║╚██╔╝██║██║   ██║",
                        @"██████╔╝███████╗██║ ╚═╝ ██║╚██████╔╝",
                        @"╚═════╝ ╚══════╝╚═╝     ╚═╝ ╚═════╝ "
                    ];

                    List<ColoredText> menuItems =
                    [
                        "",
                        "",
                        .. logo,
                        .. new[]
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
                        },
                    ];

                    List<SaveInfo> saves = SaveManager.GetSaveFiles();
                    if (saves.Any())
                    {
                        menuItems.Add("");
                        menuItems.Add("Available Saves:");
                        foreach ((string slot, SaveData save) in saves)
                        {
                            menuItems.Add($"  {slot}: {save.DisplayName}");
                        }
                    }

                    manager.RenderWrappedText(region, menuItems);
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
                    ConsoleKeyInfo key = Console.ReadKey(true);
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

        private static async Task StartGameAsync(string? loadSlot = null)
        {
            try
            {
                Logger.Instance.Information("Starting game session. Load slot: {LoadSlot}",
                    loadSlot ?? "New Game");

                using ConsoleWindowManager manager = new();
                GameState state = new(manager);

                if (loadSlot == null)
                {
                    // Generate new world with unique seed
                    int worldSeed = Guid.NewGuid().GetHashCode();
                    Logger.Instance.Debug("Generating new world with seed: {Seed}", worldSeed);
                    ProceduralWorldGenerator generator = new(worldSeed);
                    WorldConfig worldConfig = generator.GenerateWorld();

                    // Create world directory under settings directory
                    string worldDir = Path.Combine(PathUtilities.GetSettingsDirectory(), "Worlds", worldConfig.Name.Replace(" ", "_"));
                    Directory.CreateDirectory(worldDir);

                    OptimizedWorldBuilder builder = new(worldDir, worldConfig);
                    builder.Build();

                    state.LoadWorld(Path.Combine(worldDir, "world.dat"), true);
                }
                else if (!state.LoadGame(loadSlot))
                {
                    Logger.Instance.Error("Failed to load game from slot: {Slot}", loadSlot);
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
                        r.Y = (mapHeight / 2) + 2;
                        r.Width = mapWidth;
                        r.Height = (mapHeight / 2) - 1;
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

                Region worldMap = new()
                {
                    Name = "World Map",
                    BorderColor = ConsoleColor.Blue,
                    TitleColor = ConsoleColor.Cyan,
                    RenderContent = (region) => manager.RenderMap(region, state.World?.GetWorldData() ?? new(), state.CurrentRegion ?? throw new InvalidOperationException("Current region is null"))
                };

                Region regionMap = new()
                {
                    Name = "Region Map",
                    BorderColor = ConsoleColor.Green,
                    TitleColor = ConsoleColor.Green,
                    RenderContent = (region) => manager.RenderRegionMap(region, state.CurrentRegion ?? throw new InvalidOperationException("Current region is null"))
                };

                Region gameLog = new()
                {
                    Name = "Game Log",
                    BorderColor = ConsoleColor.Blue,
                    TitleColor = ConsoleColor.Cyan,
                    RenderContent = (region) =>
                    {
                        manager.RenderWrappedText(region, state.GameLog.TakeLast(50));
                    }
                };

                Region statsPanel = new()
                {
                    Name = "Character Stats",
                    BorderColor = ConsoleColor.Green,
                    TitleColor = ConsoleColor.Green,
                    RenderContent = (region) =>
                    {
                        List<ColoredText> stats =
                        [
                            $"Player: {state.PlayerName}",
                            $"Level: {state.Level}",
                            $"HP: {state.HP}/{state.MaxHP}",
                            "Equipment:",
                            "- Rusty Sword",
                            "- Leather Armor",
                            "Gold: 100"
                        ];
                        manager.RenderWrappedText(region, stats);
                    }
                };

                Region inputRegion = new()
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

                StringBuilder inputBuffer = new();

                while (state.Running)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);

                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                string command = inputBuffer.ToString().Trim();
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
                                    inputBuffer.Length--;
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
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Error in StartGameAsync");
                throw;
            }
        }

        private static void ProcessCommand(string input, GameState state)
        {
            try
            {
                Logger.Instance.Debug("Processing command: {Command}", input);
                state.CommandHandler.ExecuteCommand(input, state);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Error processing command: {Command}", input);
                state.GameLog.Add($"Error executing command: {ex.Message}");
            }
        }

        private static void SetupCommands(GameState state)
        {
            CommandHandler commandHandler = state.CommandHandler;

            commandHandler.RegisterCommand(new HelpCommand(commandHandler));
            commandHandler.RegisterCommand(new SaveCommand());
            commandHandler.RegisterCommand(new LoadCommand());

            LuaCommandLoader luaLoader = new(state);
            foreach (ICommand command in luaLoader.LoadCommands())
            {
                commandHandler.RegisterCommand(command);
            }
        }

        private static async Task ShowOptionsMenuAsync()
        {
            using ConsoleWindowManager manager = new();
            GameState state = new(manager);
            int currentOption = 1;
            List<System.Globalization.CultureInfo> languages = [.. LocalizationManager.GetAvailableLanguages()];
            int currentLanguageIndex = languages.FindIndex(c => c.Name == GameSettings.CurrentLanguage);
            GameSettings settings = GameSettings.Instance;
            const int MAX_OPTIONS = 7; // Updated number of options

            // FPS options (will be converted to refresh rate ms)
            int[] fpsOptions = [30, 60, 120];
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

            Region optionsMenu = new()
            {
                Name = state.Localization.GetString("Options_Title"),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<ColoredText> options =
                    [
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
                    ];

                    manager.RenderWrappedText(region, options);
                }
            };

            // Helper function to display border style
            static string GetBorderStyleDisplay(ConsoleDisplayConfig config, GameState state)
            {
                if (!config.UseUnicodeBorders)
                {
                    return state.Localization.GetString("BorderStyle_ASCII");
                }
                else
                {
                    return config.UseCurvedBorders
                        ? state.Localization.GetString("BorderStyle_Curved")
                        : state.Localization.GetString("BorderStyle_Classic");
                }
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
                    ConsoleKeyInfo key = Console.ReadKey(true);
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
                                    currentLanguageIndex = key.Key == ConsoleKey.LeftArrow
                                        ? (currentLanguageIndex - 1 + languages.Count) % languages.Count
                                        : (currentLanguageIndex + 1) % languages.Count;

                                    string newLanguage = languages[currentLanguageIndex].Name;
                                    settings.UpdateLanguage(newLanguage);  // Update the language in settings
                                    state.Localization.SetLanguage(newLanguage);  // Apply the language change
                                    break;

                                case 3: // Border Style cycling
                                    CycleBorderStyle(settings.Display, key.Key == ConsoleKey.RightArrow);
                                    manager.UpdateDisplaySettings(settings.Display);
                                    break;

                                case 5: // Blink Speed
                                    settings.Display.CursorBlinkRateMs = key.Key == ConsoleKey.LeftArrow
                                        ? Math.Max(200, settings.Display.CursorBlinkRateMs - 150)
                                        : Math.Min(1000, settings.Display.CursorBlinkRateMs + 150);
                                    manager.UpdateDisplaySettings(settings.Display);
                                    break;

                                case 6: // Frame Rate
                                    currentFpsIndex = key.Key == ConsoleKey.LeftArrow
                                        ? (currentFpsIndex - 1 + fpsOptions.Length) % fpsOptions.Length
                                        : (currentFpsIndex + 1) % fpsOptions.Length;
                                    settings.Display.RefreshRateMs = 1000 / fpsOptions[currentFpsIndex];
                                    manager.UpdateDisplaySettings(settings.Display);
                                    break;
                            }
                            break;

                        case ConsoleKey.Escape:
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

        private static async Task ShowLoadGameMenuAsync()
        {
            using ConsoleWindowManager manager = new();
            GameState state = new(manager);

            List<SaveInfo> saves = SaveManager.GetSaveFiles();
            if (!saves.Any())
            {
                Region messageBox = new()
                {
                    Name = state.Localization.GetString("Load_Title"),
                    BorderColor = ConsoleColor.Yellow,
                    TitleColor = ConsoleColor.Yellow,
                    RenderContent = (region) =>
                    {
                        List<ColoredText> lines =
                        [
                            "",
                            state.Localization.GetString("Load_NoSaves"),
                            "",
                            "Press any key to return..."
                        ];
                        manager.RenderWrappedText(region, lines);
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

            int selectedIndex = 0;
            const int itemsPerPage = 5;
            int currentPage = 0;
            int totalPages = (int)Math.Ceiling(saves.Count / (double)itemsPerPage);

            Region loadMenu = new()
            {
                Name = state.Localization.GetString("Load_Title"),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<ColoredText> menuItems =
                    [
                        "",
                        state.Localization.GetString("Load_Instructions"),
                        state.Localization.GetString("Load_DeleteInstructions"),
                        ""
                    ];

                    int pageStart = currentPage * itemsPerPage;
                    List<SaveInfo> pageSaves = [.. saves.Skip(pageStart).Take(itemsPerPage)];

                    for (int i = 0; i < pageSaves.Count; i++)
                    {
                        SaveInfo saveInfo = pageSaves[i];
                        int index = pageStart + i;
                        string saveType = saveInfo.Metadata.SaveType == SaveType.Autosave ? "[Auto]" : "[Manual]";
                        string saveDate = saveInfo.Metadata.SaveTime.ToString("yyyy-MM-dd HH:mm");

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

                    manager.RenderWrappedText(region, menuItems);
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
                    ConsoleKeyInfo key = Console.ReadKey(true);
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
                            (string slotToDelete, SaveData _) = saves[selectedIndex];
                            if (await ConfirmDeleteAsync(slotToDelete))
                            {
                                SaveManager.DeleteSave(slotToDelete);
                                saves = SaveManager.GetSaveFiles();
                                if (!saves.Any())
                                {
                                    manager.UpdateRegion("LoadMenu", r => r.IsVisible = false);
                                    Region messageBox = new()
                                    {
                                        Name = state.Localization.GetString("Load_Title"),
                                        BorderColor = ConsoleColor.Yellow,
                                        TitleColor = ConsoleColor.Yellow,
                                        RenderContent = (region) =>
                                        {
                                            List<ColoredText> lines =
                                            [
                                                "",
                                                state.Localization.GetString("Load_NoSaves"),
                                                "",
                                                "Press any key to return..."
                                            ];
                                            manager.RenderWrappedText(region, lines);
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
                            (string slot, SaveData _) = saves[selectedIndex];
                            await manager.DisposeAsync();
                            await StartGameAsync(slot);
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
#pragma warning disable IDE0046 // Convert to conditional expression
            if (time.TotalDays >= 1)
            {
                return state.Localization.GetString("PlayTime_Days",
                    (int)time.TotalDays, time.Hours, time.Minutes);
            }
            else
            {
                return time.TotalHours >= 1
                    ? state.Localization.GetString("PlayTime_Hours",
                                (int)time.TotalHours, time.Minutes)
                    : state.Localization.GetString("PlayTime_Minutes",
                            time.Minutes, time.Seconds);
            }
#pragma warning restore IDE0046 // Convert to conditional expression
        }

        private static async Task<bool> ConfirmDeleteAsync(string slot)
        {
            using ConsoleWindowManager manager = new();
            bool selected = false;

            Region dialog = new()
            {
                Name = "Confirm",
                BorderColor = ConsoleColor.Red,
                TitleColor = ConsoleColor.Red,
                RenderContent = (region) =>
                {
                    List<ColoredText> lines =
                    [
                        "",
                        $"Are you sure you want to delete save {slot}?",
                        "",
                        "This action cannot be undone.",
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
                    r.Height = Console.WindowHeight / 3;
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
}