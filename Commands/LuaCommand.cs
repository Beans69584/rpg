using System;
using NLua;

namespace RPG.Commands
{
    /// <summary>
    /// Represents a lua command that can be executed by the player.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="LuaCommand"/> class.
    /// </remarks>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A brief description of the command.</param>
    /// <param name="executeFunction">The function to execute when the command is run.</param>
    /// <param name="aliases">A list of aliases for the command.</param>
    /// <param name="usage">A brief description of how to use the command.</param>
    /// <param name="category">The category of the command.</param>
    public class LuaCommand(string name, string description, LuaFunction executeFunction,
        string[] aliases, string usage, string category) : BaseCommand
    {
        private readonly string _name = name;
        private readonly string _description = description;
        private readonly LuaFunction _executeFunction = executeFunction;
        private readonly string[] _aliases = aliases;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public override string Name => _name;
        /// <summary>
        /// Gets a brief description of the command.
        /// </summary>
        public override string Description => _description;
        /// <summary>
        /// Gets a list of aliases for the command.
        /// </summary>
        public override string[] Aliases => _aliases;
        /// <summary>
        /// Gets a brief description of how to use the command.
        /// </summary>
        public string Usage { get; } = usage;
        /// <summary>
        /// Gets the category of the command.
        /// </summary>
        public string Category { get; } = category;

        /// <summary>
        /// Executes the command with the specified arguments and game state.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="state">The current game state.</param>
        public override void Execute(string args, GameState state)
        {
            try
            {
                _executeFunction.Call(args, state);
            }
            catch (Exception ex)
            {
                state.GameLog.Add($"Error executing command '{_name}': {ex.Message}");
            }
        }
    }
}
