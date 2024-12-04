# Demo RPG

A text-based RPG game engine written in C# featuring procedurally generated worlds, a Lua scripting system for commands and a console-based UI.

## Features

- Procedurally generated world with different regions, locations, NPCs and items
- Command-based interaction system with built-in and custom Lua commands
- Multi-language support with localisation for 9 languages (WIP)
- Save/load game system with multiple save slots and autosaves (WIP)
- Configurable console display with support for colours, Unicode and curved borders
- Interactive region and location maps
- Dynamic travel system with progress visualisation
- Extensible command system allowing for easy addition of new commands

## Requirements

- .NET 9.0 or higher
- NLua 1.7.3

## Building

1. Clone the repository
2. Restore NuGet packages:

   ```sh
    dotnet restore
    ```

3. Build the project:

   ```sh
    dotnet build -c Release -r {platform} # replace {platform} with your platform (e.g. win-x64, linux-x64, osx-x64)
    ```

## Running

### Run without building

```sh
dotnet run
```

### Running the compiled assembly

 ```sh
 cd bin/Release/net9.0/{platform}/publish/ # replace {platform} with your platform (e.g. win-x64, linux-x64, osx-x64)
 dotnet RPG.dll # or ./RPG
 ```

## Game Controls

- Use arrow keys to navigate menus
- Type commands to interact with the game world.
- Press ESC to return to previous menu/exit

## Basic Commands

- `help` - Displays a list of available commands
- `look` - Examine your surroundings
- `go <region>` - Travel to a connected region
- `enter <location>` - Enter a location in the current region
- `leave` - Leave the current location
- `save <slot>` - Save the game to a specific slot
- `load <slot>` - Load a saved game from a specific slot (will be removed)
- `exit` - Exit the game

## Extending the Game

### Adding Custom Commands

Create new Lua script files in the user scripts directory:

```lua
return CreateCommand({
    name = "commandname",
    description = "Command description",
    aliases = {"alias1", "alias2"},
    usage = "commandname [args]",
    category = "Category",
    execute = function(args, state)
        -- Command logic
    end
})
```

## Game Data Locations

### Settings

Game settings are stored in the application data directory:

- Windows: `%AppData%\DemoRPG\Settings`
- macOS: `~/Library/Application Support/DemoRPG/Settings`
- Linux: `~/.config/DemoRPG`

### Save Games

Save games are stored in the following locations:

- Windows: `%AppData%\DemoRPG\Saves`
- macOS: `~/Library/Application Support/DemoRPG/Saves`
- Linux: `~/.local/share/demorpg/saves`

Each save directory contains:

- Regular saves (`.save` files)
- Autosaves in `Autosaves` subfolder
- Backups in `Backups` subfolder

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.
