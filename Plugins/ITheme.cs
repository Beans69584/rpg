// Plugins/ITheme.cs
namespace RPG.Plugins
{
    public interface ITheme
    {
        string Name { get; }
        Dictionary<string, char> BorderCharacters { get; }
        Dictionary<string, ConsoleColor> Colors { get; }
        Dictionary<string, string> Styles { get; }
    }
}