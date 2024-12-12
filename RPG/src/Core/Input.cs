using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public class Input
    {
        private readonly HashSet<ConsoleKey> _currentKeys = [];
        private readonly HashSet<ConsoleKey> _previousKeys = [];
        private readonly HashSet<char> _currentChars = [];
        private readonly List<ConsoleKeyInfo> _keyEvents = [];

        public void Update()
        {
            _previousKeys.Clear();
            _previousKeys.UnionWith(_currentKeys);
            _currentKeys.Clear();
            _currentChars.Clear();
            _keyEvents.Clear();

            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                _currentKeys.Add(keyInfo.Key);
                _keyEvents.Add(keyInfo);

                // Handle printable characters including shift and special chars
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _currentChars.Add(keyInfo.KeyChar);
                }
            }
        }

        public bool IsKeyPressed(ConsoleKey key)
        {
            return _currentKeys.Contains(key) && !_previousKeys.Contains(key);
        }

        public bool IsKeyHeld(ConsoleKey key)
        {
            return _currentKeys.Contains(key);
        }

        public IEnumerable<char> GetPressedChars()
        {
            return _currentChars;
        }

        public IEnumerable<ConsoleKeyInfo> GetKeyEvents()
        {
            return _keyEvents;
        }
    }
}
