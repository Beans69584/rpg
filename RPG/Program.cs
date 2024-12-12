using System.Text;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Serilog;

using RPG.UI;
using RPG.Core;
using RPG.Utils;
using RPG.World;
using RPG.World.Data;
using RPG.Commands;
using RPG.Commands.Builtin;
using RPG.Common;
using RPG.Core.Player;
using RPG.Core.Player.Common;
using RPG.Core.Managers;
using RPG.UI.Windows;
using System.Reflection;
using RPG.Save;

namespace RPG
{
    /// <summary>
    /// The main entry class for the application.
    /// </summary>
    public static class Program
    {
        #region Constants
        private const int MIN_WIDTH = 80;
        private const int MIN_HEIGHT = 24;

        #endregion

        #region Methods
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


                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Logger.Instance.Debug("Console mode configured for UTF-8");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warning(ex, "Failed to configure console mode for UTF-8");
                    // Fallback to ASCII if UTF-8 setup fails
                    GameSettings.Instance.Display.UseUnicodeBorders = false;
                    GameSettings.Instance.Save();
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
            string version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0";

            Region mainMenu = new()
            {
                Name = state.Localization.GetString("MainMenu_Title", version),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<string> logo =
                    [
                        @"██████╗ ███████╗ ██████╗ ██████╗ ██╗     ███████╗     ██████╗██╗      █████╗ ███████╗███████╗███████╗███████╗",
                        @"██╔══██╗██╔════╝██╔═══██╗██╔══██╗██║     ██╔════╝    ██╔════╝██║     ██╔══██╗██╔════╝██╔════╝██╔════╝██╔════╝",
                        @"██████╔╝█████╗  ██║   ██║██████╔╝██║     █████╗      ██║     ██║     ███████║███████╗███████╗█████╗  ███████╗",
                        @"██╔═══╝ ██╔══╝  ██║   ██║██╔═══╝ ██║     ██╔══╝      ██║     ██║     ██╔══██║╚════██║╚════██║██╔══╝  ╚════██║",
                        @"██║     ███████╗╚██████╔╝██║     ███████╗███████╗    ╚██████╗███████╗██║  ██║███████║███████║███████╗███████║",
                        @"╚═╝     ╚══════╝ ╚═════╝ ╚═╝     ╚══════╝╚══════╝     ╚═════╝╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝╚══════╝"
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
                        },
                    ];

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
        }

        private static async Task StartGameAsync(string? saveId = null)
        {
            GameState state;

            if (saveId != null)
            {
                // Loading existing save
                using ConsoleWindowManager loadManager = new();
                state = SaveManager.LoadGame(saveId, loadManager) ?? new GameState(loadManager);

                if (state.World == null)
                {
                    // Handle failed load
                    state.GameLog.Add(new ColoredText("Error: Failed to load save file", ConsoleColor.Red));
                    await Task.Delay(2000);
                    return;
                }
            }
            else
            {
                // New game setup
                using ConsoleWindowManager selectionManager = new();
                state = new GameState(selectionManager);

                // Get player name
                string? playerName = await ShowPlayerNameDialogAsync(selectionManager, state);
                if (string.IsNullOrEmpty(playerName))
                {
                    return;
                }
                state.PlayerName = playerName;

                // Create character
                CharacterCreationWindow charCreation = new(state);
                Person? character = await charCreation.ShowAsync();
                if (character == null)
                {
                    return;
                }
                state.CurrentPlayer = character;

                // Load initial world data
                try
                {
                    WorldData worldData = GenerateWorld();
                    state.World = new WorldLoader(worldData);
                    state.CurrentRegion = state.World.GetStartingRegion();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex, "Failed to load world data");
                    state.GameLog.Add("Error: Failed to load world data");
                    await Task.Delay(2000);
                    return;
                }
            }
            using ConsoleWindowManager gameManager = new();
            state.WindowManager = gameManager;

            if (state.World == null)
            {
                Logger.Instance.Error("World or current region is null after load");
                state.GameLog.Add("Error: Failed to load world data");
                await Task.Delay(2000);
                return;
            }

            state.CurrentRegion ??= state.World.GetStartingRegion();

            SetupCommands(state);

            void UpdateLayout()
            {
                int logHeight = Console.WindowHeight - 6;
                int statsWidth = 30;

                // Game log on the left
                gameManager.UpdateRegion("GameLog", r =>
                {
                    r.X = 1;
                    r.Y = 1;
                    r.Width = Console.WindowWidth - statsWidth - 3; // Leave space for stats panel
                    r.Height = logHeight;
                });

                // Stats panel on the right 
                gameManager.UpdateRegion("Stats", r =>
                {
                    r.X = Console.WindowWidth - statsWidth - 1;
                    r.Y = 1;
                    r.Width = statsWidth;
                    r.Height = logHeight;
                });

                // Input at the bottom
                gameManager.UpdateRegion("Input", r =>
                {
                    r.X = 1;
                    r.Y = Console.WindowHeight - 4;
                    r.Width = Console.WindowWidth - 2;
                    r.Height = 3;
                });
            }

            Region gameLog = new()
            {
                Name = "Game Log",
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    gameManager.RenderWrappedText(region, state.GameLog.TakeLast(50));
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
                        $"Gold: {state.Gold}",
                    ];
                    gameManager.RenderWrappedText(region, stats);
                }
            };

            Region inputRegion = new()
            {
                Name = "Input",
                BorderColor = ConsoleColor.Yellow,
                TitleColor = ConsoleColor.Yellow
            };

            gameManager.AddRegion("GameLog", gameLog);
            gameManager.AddRegion("Stats", statsPanel);
            gameManager.AddRegion("Input", inputRegion);
            UpdateLayout();

            StringBuilder inputBuffer = new();

            while (state.Running)
            {
                state.Input.Update();

                if (!state.InputSuspended)
                {
                    // Handle backspace
                    if (state.Input.IsKeyPressed(ConsoleKey.Backspace) && inputBuffer.Length > 0)
                    {
                        inputBuffer.Length--;
                        gameManager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.White);
                    }
                    // Handle enter
                    else if (state.Input.IsKeyPressed(ConsoleKey.Enter))
                    {
                        string command = inputBuffer.ToString().Trim();
                        if (!string.IsNullOrEmpty(command))
                        {
                            ProcessCommand(command, state);
                        }
                        inputBuffer.Clear();
                        gameManager.UpdateInputText("", ConsoleColor.White);
                    }
                    // Handle escape
                    else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                    {
                        return;
                    }
                    // Handle regular input
                    else
                    {
                        // Check for printable characters
                        foreach (char c in state.Input.GetPressedChars())
                        {
                            if (inputBuffer.Length < 100)
                            {
                                inputBuffer.Append(c);
                                gameManager.UpdateInputText(inputBuffer.ToString(), ConsoleColor.White);
                            }
                        }
                    }

                    gameManager.QueueRender();
                }

                if (gameManager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(16);
            }
        }

        private static WorldData GenerateWorld()
        {
            // Load the prebuilt world from the worlds directory
            string worldPath = PathUtilities.GetPrebuiltWorldPath("ravenkeep");

            if (!File.Exists(worldPath))
            {
                throw new FileNotFoundException($"Prebuilt world not found at: {worldPath}");
            }

            using FileStream fs = File.OpenRead(worldPath);
            using GZipStream gzip = new(fs, CompressionMode.Decompress);
            using MemoryStream ms = new();
            gzip.CopyTo(ms);
            ms.Position = 0;

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new Vector2JsonConverter() }
            };

            WorldData? worldData = JsonSerializer.Deserialize<WorldData>(ms.ToArray(), options)
                ?? throw new InvalidDataException("Failed to deserialize world data");

            // Create a new instance to ensure we're not modifying the template
            return JsonSerializer.Deserialize<WorldData>(
                JsonSerializer.Serialize(worldData, options), options)!;
        }

        private static async Task<string?> ShowPlayerNameDialogAsync(ConsoleWindowManager manager, GameState state)
        {
            StringBuilder playerName = new();
            bool isValid = false;

            Region dialog = new()
            {
                Name = state.Localization.GetString("NewPlayer_Title"),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<ColoredText> lines =
                    [
                        "",
                        state.Localization.GetString("NewPlayer_EnterName"),
                        "",
                        $"> {state.Localization.GetString("NewPlayer_Name")}: [{playerName}]",
                        "",
                        state.Localization.GetString("NewPlayer_Controls"),
                        "",
                        !isValid ? state.Localization.GetString("NewPlayer_InvalidName") : ""
                    ];
                    manager.RenderWrappedText(region, lines);
                }
            };

            void UpdateLayout()
            {
                manager.UpdateRegion("NewPlayer", r =>
                {
                    r.X = Console.WindowWidth / 4;
                    r.Y = Console.WindowHeight / 3;
                    r.Width = Console.WindowWidth / 2;
                    r.Height = Console.WindowHeight / 3;
                });
            }

            manager.AddRegion("NewPlayer", dialog);
            UpdateLayout();
            manager.QueueRender();

            while (true)
            {
                state.Input.Update();

                if (state.Input.IsKeyPressed(ConsoleKey.Tab))
                {
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Enter))
                {
                    if (playerName.Length == 0)
                    {
                        isValid = false;
                        manager.QueueRender();
                        continue;
                    }
                    return playerName.ToString().Trim();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                {
                    return null;
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Backspace) && playerName.Length > 0)
                {
                    playerName.Length--;
                    isValid = true;
                    manager.QueueRender();
                }
                else
                {
                    foreach (char c in state.Input.GetPressedChars())
                    {
                        if (playerName.Length < 30)
                        {
                            playerName.Append(c);
                            isValid = true;
                            manager.QueueRender();
                        }
                    }
                }

                if (manager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(16);
            }
        }

        /// <summary>
        /// Shows the load game menu.
        /// </summary>
        /// <param name="manager">The console window manager to use.</param>
        /// <param name="state">The current game state.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task<string?> ShowNewSaveDialogAsync(ConsoleWindowManager manager, GameState state)
        {
            StringBuilder saveName = new();
            bool isValid = false;

            Region dialog = new()
            {
                Name = state.Localization.GetString("NewSave_Title"),
                BorderColor = ConsoleColor.Blue,
                TitleColor = ConsoleColor.Cyan,
                RenderContent = (region) =>
                {
                    List<ColoredText> lines =
                    [
                        "",
                        state.Localization.GetString("NewSave_EnterName"),
                        "",
                        $"> {state.Localization.GetString("NewSave_Name")}: [{saveName}]",
                        "",
                        state.Localization.GetString("NewSave_Controls"),
                        "",
                        !isValid ? state.Localization.GetString("NewSave_InvalidName") : ""
                    ];
                    manager.RenderWrappedText(region, lines);
                }
            };

            void UpdateLayout()
            {
                manager.UpdateRegion("NewSave", r =>
                {
                    r.X = Console.WindowWidth / 4;
                    r.Y = Console.WindowHeight / 3;
                    r.Width = Console.WindowWidth / 2;
                    r.Height = Console.WindowHeight / 3;
                });
            }

            manager.AddRegion("NewSave", dialog);
            UpdateLayout();
            manager.QueueRender();

            while (true)
            {
                state.Input.Update();

                if (state.Input.IsKeyPressed(ConsoleKey.Tab))
                {
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Enter))
                {
                    if (saveName.Length == 0)
                    {
                        isValid = false;
                        manager.QueueRender();
                        continue;
                    }
                    return saveName.ToString().Trim();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                {
                    return null;
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Backspace) && saveName.Length > 0)
                {
                    saveName.Length--;
                    isValid = true;
                    manager.QueueRender();
                }
                else
                {
                    foreach (char c in state.Input.GetPressedChars())
                    {
                        if (saveName.Length < 100)
                        {
                            saveName.Append(c);
                            isValid = true;
                            manager.QueueRender();
                        }
                    }
                }

                if (manager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(16);
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

            // Get all types in the executing assembly that implement ICommand
            IEnumerable<Type> commandTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract &&
                            typeof(ICommand).IsAssignableFrom(t) &&
                            t.Namespace?.StartsWith("RPG.Commands") == true);

            foreach (Type commandType in commandTypes)
            {
                try
                {
                    // Create instance based on constructor parameters
                    ICommand? command = null;
                    ConstructorInfo[] constructors = commandType.GetConstructors();
                    if (constructors.Length == 0)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = constructors[0].GetParameters();

                    if (parameters.Length == 0)
                    {
                        command = (ICommand?)Activator.CreateInstance(commandType);
                    }
                    else
                    {
                        // Map parameter types to available instances
                        object?[] args = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            args[i] = parameters[i].ParameterType switch
                            {
                                Type t when t == typeof(GameState) => state,
                                Type t when t == typeof(CommandHandler) => commandHandler,
                                _ => null
                            };
                        }

                        command = Activator.CreateInstance(commandType, args) as ICommand;
                    }

                    command?.Let(c =>
                    {
                        commandHandler.RegisterCommand(c);
                        Logger.Debug($"Registered command: {c.Name}");
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to register command {commandType.Name}");
                }
            }
        }
        private static async Task ShowOptionsMenuAsync()
        {
            using ConsoleWindowManager manager = new();
            GameState state = new(manager);
            int currentOption = 1;
            GameSettings settings = GameSettings.Instance;
            const int MAX_OPTIONS = 6;

            // FPS options (will be converted to refresh rate ms)
            int[] fpsOptions = [30, 60, 120];
            int currentFpsIndex = Array.FindIndex(fpsOptions, fps => 1000 / fps == settings.Display.RefreshRateMs);
            if (currentFpsIndex == -1) currentFpsIndex = 1; // Default to 60 FPS if current setting isn't in list

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
                $"   {state.Localization.GetString("Settings_Visual")}",
                "   ===============",
                "",
                currentOption == 1 ?
                    $" > {state.Localization.GetString("Settings_Colors_Toggle", settings.Display.UseColors ? state.Localization.GetString("Settings_Disable") : state.Localization.GetString("Settings_Enable"))}" :
                    $"   {state.Localization.GetString("Settings_Colors_Label", settings.Display.UseColors ? state.Localization.GetString("Settings_On") : state.Localization.GetString("Settings_Off"))}",
                "",
                currentOption == 2 ?
                    $" > {state.Localization.GetString("Settings_BorderStyle_Label", GetBorderStyleDisplay(settings.Display, state))} [←/→]" :
                    $"   {state.Localization.GetString("Settings_BorderStyle_Label", GetBorderStyleDisplay(settings.Display, state))}",
                "",
                "",
                $"   {state.Localization.GetString("Settings_Cursor")}",
                "   ===============",
                "",
                currentOption == 3 ?
                    $" > {state.Localization.GetString("Settings_CursorBlink_Toggle", settings.Display.EnableCursorBlink ? state.Localization.GetString("Settings_Disable") : state.Localization.GetString("Settings_Enable"))}" :
                    $"   {state.Localization.GetString("Settings_CursorBlink_Label", settings.Display.EnableCursorBlink ? state.Localization.GetString("Settings_On") : state.Localization.GetString("Settings_Off"))}",
                "",
                currentOption == 4 ?
                    $" > {state.Localization.GetString("Settings_BlinkSpeed_Label", GetBlinkSpeedDisplay(settings.Display.CursorBlinkRateMs, state))} [←/→]" :
                    $"   {state.Localization.GetString("Settings_BlinkSpeed_Label", GetBlinkSpeedDisplay(settings.Display.CursorBlinkRateMs, state))}",
                "",
                "",
                $"   {state.Localization.GetString("Settings_Performance")}",
                "   ===========",
                "",
                currentOption == 5 ?
                    $" > {state.Localization.GetString("Settings_FrameRate_Label", fpsOptions[currentFpsIndex])} [←/→]" :
                    $"   {state.Localization.GetString("Settings_FrameRate_Label", fpsOptions[currentFpsIndex])}",
                "",
                "",
                currentOption == 6 ?
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
                state.Input.Update();

                if (state.Input.IsKeyPressed(ConsoleKey.UpArrow))
                {
                    currentOption = currentOption == 1 ? MAX_OPTIONS : currentOption - 1;
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.DownArrow))
                {
                    currentOption = currentOption == MAX_OPTIONS ? 1 : currentOption + 1;
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Enter) || state.Input.IsKeyPressed(ConsoleKey.Spacebar))
                {
                    bool settingsChanged = true;
                    switch (currentOption)
                    {
                        case 1: // Colours toggle
                            settings.Display.UseColors = !settings.Display.UseColors;
                            manager.UpdateDisplaySettings(settings.Display);
                            break;
                        case 3: // Cursor Blink toggle
                            settings.Display.EnableCursorBlink = !settings.Display.EnableCursorBlink;
                            manager.UpdateDisplaySettings(settings.Display);
                            break;
                        case 6: // Back
                            settings.Save();
                            return;
                    }
                    if (settingsChanged)
                    {
                        settings.Save();
                        manager.QueueRender();
                    }
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.LeftArrow) || state.Input.IsKeyPressed(ConsoleKey.RightArrow))
                {
                    bool settingsChanged = true;
                    switch (currentOption)
                    {
                        case 2: // Border Style cycling
                            CycleBorderStyle(settings.Display, state.Input.IsKeyPressed(ConsoleKey.RightArrow));
                            manager.UpdateDisplaySettings(settings.Display);
                            break;

                        case 4: // Blink Speed
                            settings.Display.CursorBlinkRateMs = state.Input.IsKeyPressed(ConsoleKey.LeftArrow)
                            ? Math.Max(200, settings.Display.CursorBlinkRateMs - 150)
                            : Math.Min(1000, settings.Display.CursorBlinkRateMs + 150);
                            manager.UpdateDisplaySettings(settings.Display);
                            break;

                        case 5: // Frame Rate
                            currentFpsIndex = state.Input.IsKeyPressed(ConsoleKey.LeftArrow)
                            ? (currentFpsIndex - 1 + fpsOptions.Length) % fpsOptions.Length
                            : (currentFpsIndex + 1) % fpsOptions.Length;
                            settings.Display.RefreshRateMs = 1000 / fpsOptions[currentFpsIndex];
                            manager.UpdateDisplaySettings(settings.Display);
                            break;
                    }
                    if (settingsChanged)
                    {
                        settings.Save();
                        manager.QueueRender();
                    }
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                {
                    settings.Save();
                    return;
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
                        SaveInfo save = pageSaves[i];
                        int index = pageStart + i;
                        string saveType = save.Type == SaveType.Autosave ? "[Auto]" : "[Manual]";
                        string saveDate = save.LastModified.ToString("yyyy-MM-dd HH:mm");

                        menuItems.Add(index == selectedIndex ?
                            new ColoredText($"> [{save.DisplayName}] {saveType} {saveDate}", ConsoleColor.Cyan) :
                            new ColoredText($"  {save.DisplayName} {saveType} {saveDate}", ConsoleColor.Gray));

                        menuItems.Add(new ColoredText($"     {save.PlayerName} - Level {save.PlayerLevel}", ConsoleColor.DarkCyan));
                        menuItems.Add(new ColoredText($"     Location: {save.Location}", ConsoleColor.DarkCyan));
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
                state.Input.Update();

                if (state.Input.IsKeyPressed(ConsoleKey.UpArrow))
                {
                    selectedIndex = (selectedIndex - 1 + saves.Count) % saves.Count;
                    currentPage = selectedIndex / itemsPerPage;
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.DownArrow))
                {
                    selectedIndex = (selectedIndex + 1) % saves.Count;
                    currentPage = selectedIndex / itemsPerPage;
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.LeftArrow))
                {
                    if (totalPages > 1)
                    {
                        currentPage = (currentPage - 1 + totalPages) % totalPages;
                        selectedIndex = currentPage * itemsPerPage;
                        manager.QueueRender();
                    }
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.RightArrow))
                {
                    if (totalPages > 1)
                    {
                        currentPage = (currentPage + 1) % totalPages;
                        selectedIndex = currentPage * itemsPerPage;
                        manager.QueueRender();
                    }
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Delete))
                {
                    string slotToDelete = saves[selectedIndex].Id;
                    if (await ConfirmDeleteAsync(slotToDelete, manager, state))
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
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Enter))
                {
                    string slot = saves[selectedIndex].Id;
                    GameState? loadedState = await Task.Run(() => SaveManager.LoadGame(slot, manager));

                    if (loadedState == null)
                    {
                        Logger.Instance.Error("Failed to load save file {Slot}", slot);
                        continue;
                    }

                    await manager.DisposeAsync();
                    await StartGameAsync(slot);
                    return;
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                {
                    return;
                }

                if (manager.CheckResize())
                {
                    UpdateLayout();
                }

                await Task.Delay(16);
            }
        }
        private static async Task<bool> ConfirmDeleteAsync(string slot, ConsoleWindowManager manager, GameState state)
        {
            bool selected = false;

            Region dialog = new()
            {
                // get from resource file
                Name = state.Localization.GetString("DeleteSave_Title"),
                BorderColor = ConsoleColor.Red,
                TitleColor = ConsoleColor.Red,
                RenderContent = (region) =>
                {
                    List<ColoredText> lines =
                    [
                        "",
                        state.Localization.GetString("DeleteSave_Confirm", slot),
                        "",
                        state.Localization.GetString("DeleteSave_Warning"),
                        "",
                        selected ? state.Localization.GetString("DeleteSave_Options_YesSelected") :
                                   state.Localization.GetString("DeleteSave_Options_NoSelected"),
                        ""
                    ];
                    manager.RenderWrappedText(region, lines);
                }
            };

            void UpdateConfirmLayout()
            {
                manager.UpdateRegion("ConfirmDialog", r =>
                {
                    r.X = Console.WindowWidth / 4;
                    r.Y = Console.WindowHeight / 3;
                    r.Width = Console.WindowWidth / 2;
                    r.Height = Console.WindowHeight / 3;
                });
            }

            manager.AddRegion("ConfirmDialog", dialog);
            UpdateConfirmLayout();
            manager.QueueRender();

            while (true)
            {
                state.Input.Update();

                if (state.Input.IsKeyPressed(ConsoleKey.LeftArrow) || state.Input.IsKeyPressed(ConsoleKey.RightArrow))
                {
                    selected = !selected;
                    manager.QueueRender();
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Enter))
                {
                    manager.RemoveRegion("ConfirmDialog");
                    manager.QueueRender();
                    return selected;
                }
                else if (state.Input.IsKeyPressed(ConsoleKey.Escape))
                {
                    manager.RemoveRegion("ConfirmDialog");
                    manager.QueueRender();
                    return false;
                }

                if (manager.CheckResize())
                {
                    UpdateConfirmLayout();
                }

                await Task.Delay(16);
            }
        }

        private static void CycleBorderStyle(ConsoleDisplayConfig config, bool forward)
        {
            // Cycle order: Unicode Curved -> Unicode Classic -> ASCII -> Unicode Curved
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

        #endregion
    }
}