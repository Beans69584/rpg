# Text RPG

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download)
[![Status](https://img.shields.io/badge/Status-Proof%20of%20Concept-orange)]()
[![Development](https://img.shields.io/badge/Development-Halted-red)]()

---
> **Note:** This project was developed for my college assignment. It is not intended for production use. The project is currently on hold and may not be actively maintained. Feel free to use the code for educational purposes or as a starting point for your own projects.

A command-line RPG game written in C# featuring character creation, turn-based combat, and a dialogue system. Currently in early development.

## Implemented Features

- **Character System**
  - Choice of classes: Warrior, Barbarian, Paladin, Sorcerer, Conjurer
  - Customisable attributes (Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma)
  - Basic class-specific abilities
  - Currently limited progression system

- **World Navigation**
  - Travel between regions and locations
  - Basic building exploration
  - NPC interactions
  - Currently lacking environmental descriptions and detailed world-building

- **Combat System**
  - Turn-based combat with basic actions (Attack, Defend, Use Items, Flee)
  - Simple enemy AI
  - Limited item system
  - Combat balancing needs work

- **Save System**
  - Manual saves with multiple slots
  - Autosave functionality
  - Save file backups
  - Currently missing save file compression

## Known Limitations

- Limited content (NPCs, quests, items)
- Basic dialogue system lacking branching conversations
- Minimal character progression
- Simple combat mechanics
- Limited world interaction options
- Needs more extensive error handling

## Requirements

- .NET 9.0
- Windows/Linux/macOS compatible
- Terminal with UTF-8 support recommended

## Building

To build the project:

1. Clone the repository
2. Ensure you have .NET 9.0 SDK installed
3. Run `dotnet build` in the project directory

## Running

```bash
dotnet run
```

## Project Structure

### RPG/src/ (Main Project)

- `Combat/` - Combat system implementation
- `Commands/` - Game command handlers
- `Core/` - Core game systems and state management
- `Player/` - Player character classes and attributes
- `Save/` - Save system implementation
- `UI/` - Console interface and window management
- `Utils/` - Utility classes and helpers
- `World/` - World data structures and loading
- `Resources/` - Game resources and localisation

### RPG.WorldBuilder/ (World Building Tool)

- `Data/` - World definition data
  - `worlds/` - World template files
  - `npcs/` - NPC definitions
  - `items/` - Item definitions
  - `regions/` - Region definitions
  - `dialogues/` - Dialogue tree definitions
  - `quests/` - Quest definitions

## Contributing

This is a learning project but contributions are welcome. The project currently needs:

### Priority Areas

- More comprehensive documentation
- Additional unit tests
- Code cleanup and optimisation
- Content creation tools
- Better error handling
- Expanded world content

### Getting Started

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards

- Follow existing C# code style and conventions
- Include XML documentation comments for public members
- Add unit tests for new functionality
- Update relevant documentation

## License

This project is licensed under the MIT License.

## Disclaimer

This is an early development project created primarily for learning purposes. Many features are incomplete or in a proof-of-concept state. The code may contain bugs and security issues. Use at your own risk.

The focus is currently on establishing core systems rather than content. Breaking changes may occur frequently as the project evolves.

While effort has been made to handle errors gracefully, data loss could occur. Regular backups of save files are recommended if you choose to use this software.
