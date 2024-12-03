// Plugins/IMiddleware.cs
namespace RPG.Plugins
{
    // Plugins/IMiddleware.cs
    public interface IMiddleware
    {
        string Name { get; }
        bool ProcessCommand(string command, GameState state, Action next);
        void ProcessRender(Region region, Action render, Action next);
    }
}