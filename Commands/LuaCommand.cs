using NLua;

namespace RPG.Commands
{
    public class LuaCommand : BaseCommand
    {
        private readonly string _name;
        private readonly string _description;
        private readonly LuaFunction _executeFunction;
        private readonly string[] _aliases;
        private readonly string _usage;
        private readonly string _category;

        public LuaCommand(string name, string description, LuaFunction executeFunction, 
            string[] aliases, string usage, string category)
        {
            _name = name;
            _description = description;
            _executeFunction = executeFunction;
            _aliases = aliases;
            _usage = usage;
            _category = category;
        }

        public override string Name => _name;
        public override string Description => _description;
        public string[] Aliases => _aliases;
        public string Usage => _usage;
        public string Category => _category;

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
