using NLua;

namespace RPG.Commands
{
    public class LuaGameAPI
    {
        private readonly GameState _state;

        public LuaGameAPI(GameState state)
        {
            _state = state;
        }

        // Game log methods
        public void Log(string message) => _state.GameLog.Add(message);
        public void LogColor(string message, string color)
        {
            // TODO: Implement colored text in game log
            _state.GameLog.Add(message);
        }
        public void ClearLog() => _state.GameLog.Clear();

        // Player state methods
        public string GetPlayerName() => _state.PlayerName;
        public void SetPlayerName(string name) => _state.PlayerName = name;
        public int GetPlayerHP() => _state.HP;
        public void SetPlayerHP(int hp) => _state.HP = Math.Clamp(hp, 0, _state.MaxHP);
        public int GetPlayerMaxHP() => _state.MaxHP;
        public void SetPlayerMaxHP(int maxHp) => _state.MaxHP = Math.Max(1, maxHp);
        public int GetPlayerLevel() => _state.Level;
        public void SetPlayerLevel(int level) => _state.Level = Math.Max(1, level);

        // Utility methods
        public void Sleep(int milliseconds) => Thread.Sleep(milliseconds);
        public string AskQuestion(string question)
        {
            Log(question);
            // TODO: Implement proper input handling
            return "";
        }
        // Combat helper methods
        public bool RollDice(int sides) => Random.Shared.Next(1, sides + 1) == sides;
        public int GetRandomNumber(int min, int max) => Random.Shared.Next(min, max + 1);
    }
}
