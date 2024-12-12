using System;
using System.Collections.Generic;

namespace RPG.Core
{
    /// <summary>
    /// Gets input from the console.
    /// </summary>
    public class Input
    {
        private readonly HashSet<ConsoleKey> _currentKeys = [];
        private readonly HashSet<ConsoleKey> _previousKeys = [];
        private readonly HashSet<char> _currentChars = [];
        private readonly List<ConsoleKeyInfo> _keyEvents = [];

        /// <summary>
        /// Updates the input state.
        /// </summary>
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

        /// <summary>
        /// Checks if a key was pressed this frame.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key was pressed this frame, false otherwise.</returns>
        public bool IsKeyPressed(ConsoleKey key)
        {
            return _currentKeys.Contains(key) && !_previousKeys.Contains(key);
        }

        /// <summary>
        /// Checks if a key was released this frame.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key was released this frame, false otherwise.</returns>
        public bool IsKeyHeld(ConsoleKey key)
        {
            return _currentKeys.Contains(key);
        }

        /// <summary>
        /// Checks if a key was released this frame.
        /// </summary>
        /// <returns>True if the key was released this frame, false otherwise.</returns>
        public IEnumerable<char> GetPressedChars()
        {
            return _currentChars;
        }

        /// <summary>
        /// Gets all key events that occurred this frame.
        /// </summary>
        /// <returns>A list of key events that occurred this frame.</returns>
        public IEnumerable<ConsoleKeyInfo> GetKeyEvents()
        {
            return _keyEvents;
        }
    }
}
