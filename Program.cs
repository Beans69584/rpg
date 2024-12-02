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

    public GameState(ConsoleWindowManager manager)
    {
        WindowManager = manager;
        CommandHandler = new CommandHandler();
        GameLog.Add("Welcome to the Demo RPG!");
        GameLog.Add("Type 'help' for a list of commands.");
    }
}
class Program
{

    static async Task Main(string[] args)
    {
        while (true)
        {
            var choice = await ShowMainMenu();
            switch (choice)
            {
                case 1:
                    await StartGame();
                    break;
                case 2:
                    return;
            }
        }
    }

    private static async Task<int> ShowMainMenu()
    {
        using var manager = new ConsoleWindowManager();
        var choice = 1;

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

        var mainMenu = new Region
        {
            Name = "Demo RPG Game",
            BorderColor = ConsoleColor.Blue,
            TitleColor = ConsoleColor.Cyan,
            RenderContent = (region) =>
            {
                var menuItems = new List<string>
                {
                    "",
                    "Welcome to the Demo RPG Game!",
                    "",
                    "Use arrow keys to select an option and press Enter to confirm.",
                    "",
                    choice == 1 ? "> [Start New Game] <" : "  Start New Game  ",
                    choice == 2 ? "> [Exit Game] <" : "  Exit Game  "
                };
                manager.RenderWrappedText(region, menuItems, ConsoleColor.White);
            }
        };

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
                    case ConsoleKey.DownArrow:
                        choice = choice == 1 ? 2 : 1;
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
                manager.RenderWrappedText(region, stats, ConsoleColor.Green);
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

        while (true)
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
}