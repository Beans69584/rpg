using Xunit;

using RPG.Core;
using RPG.UI;
using RPG.World.Generation;

namespace RPG.Tests
{
    public class GameStateTest
    {
        private readonly ConsoleWindowManager _windowManager;
        private readonly GameState _gameState;

        public GameStateTest()
        {
            _windowManager = new ConsoleWindowManager();
            _gameState = new GameState(_windowManager);
        }

        [Fact]
        public void AddLogMessage_ShouldAddMessageToGameLog()
        {
            // Arrange
            var message = new ColoredText("Test message", ConsoleColor.Green);

            // Act
            _gameState.AddLogMessage(message);

            // Assert
            Assert.Contains(message, _gameState.GameLog);
        }

        [Fact]
        public void AddLogMessage_ShouldAddStringMessageToGameLog()
        {
            // Arrange
            string message = "Test string message";

            // Act
            _gameState.AddLogMessage(message);

            // Assert
            Assert.Contains(new ColoredText(message), _gameState.GameLog);
        }

        [Fact]
        public void LoadGame_ShouldReturnFalseIfSaveDataIsNull()
        {
            // Arrange
            string slot = "invalidSlot";

            // Act
            bool result = _gameState.LoadGame(slot);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void NavigateToLocation_ShouldUpdateCurrentLocation()
        {
            // Arrange
            var location = new Location { NameId = 1, DescriptionId = 2 };

            // Act
            _gameState.NavigateToLocation(location);

            // Assert
            Assert.Equal(location, _gameState.CurrentLocation);
        }

        [Fact]
        public void NavigateToLocation_ShouldAddLocationMessageToGameLog()
        {
            // Arrange
            var location = new Location { NameId = 1, DescriptionId = 2 };

            // Act
            _gameState.NavigateToLocation(location);

            // Assert
            Assert.Contains(new ColoredText($"You are now at: {_gameState.World?.GetString(location.NameId)}"), _gameState.GameLog);
        }
    }
}