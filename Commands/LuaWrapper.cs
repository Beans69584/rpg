using System.Collections.Generic;
using NLua;

namespace RPG.Commands
{
    /// <summary>
    /// Wrapper for lua commands to access the world state.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="RegionWrapper"/> class.
    /// </remarks>
    /// <param name="region">The region to wrap.</param>
    /// <param name="world">The world loader to use.</param>
    public class RegionWrapper(WorldRegion region, WorldLoader world)
    {
        private readonly WorldLoader _world = world;

        /// <summary>
        /// Gets the name of the region.
        /// </summary>
        public string Name => _world.GetString(Region.NameId);
        /// <summary>
        /// Gets a brief description of the region.
        /// </summary>
        public string Description => _world.GetString(Region.DescriptionId);
        /// <summary>
        /// Gets the region.
        /// </summary>
        public WorldRegion Region { get; } = region;
    }

    /// <summary>
    /// Wrapper for lua commands to access the world state.
    /// </summary>
    /// <remarks>
    /// Initialises a new instance of the <see cref="LocationWrapper"/> class.
    /// </remarks>
    /// <param name="location">The location to wrap.</param>
    /// <param name="world">The world loader to use.</param>
    public class LocationWrapper(Location location, WorldLoader world)
    {
        private readonly WorldLoader _world = world;

        /// <summary>
        /// Gets the name of the location.
        /// </summary>
        public string Name => _world.GetString(Location.NameId);
        /// <summary>
        /// Gets a brief description of the location.
        /// </summary>
        public string Description => _world.GetString(Location.DescriptionId);
        /// <summary>
        /// Gets the type of the location.
        /// </summary>
        public string Type => _world.GetString(Location.TypeId);
        /// <summary>
        /// Gets the location.
        /// </summary>
        public Location Location { get; } = location;
    }

    /// <summary>
    /// Extension methods for lua tables.
    /// </summary>
    public static class LuaTableExtensions
    {
        /// <summary>
        /// Converts a list of region wrappers to a lua table.
        /// </summary>
        /// <param name="items">The items to convert.</param>
        /// <param name="lua">The lua instance to use.</param>
        /// <returns>The lua table.</returns>
        public static LuaTable? ToLuaTable(this IEnumerable<RegionWrapper> items, Lua lua)
        {
            if (lua.DoString("return {}")[0] is not LuaTable table) return null;

            int index = 1;
            foreach (RegionWrapper item in items)
            {
                table[index++] = item;
            }

            return table;
        }

        /// <summary>
        /// Converts a list of location wrappers to a lua table.
        /// </summary>
        /// <param name="items">The items to convert.</param>
        /// <param name="lua">The lua instance to use.</param>
        /// <returns>The lua table.</returns>
        public static LuaTable? ToLuaTable(this IEnumerable<LocationWrapper> items, Lua lua)
        {
            if (lua.DoString("return {}")[0] is not LuaTable table) return null;

            int index = 1;
            foreach (LocationWrapper item in items)
            {
                table[index++] = item;
            }

            return table;
        }
    }
}
