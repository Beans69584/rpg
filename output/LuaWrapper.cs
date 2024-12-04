using NLua;

namespace RPG.Commands
{
    public class RegionWrapper
    {
        private readonly WorldRegion _region;
        private readonly WorldLoader _world;

        public RegionWrapper(WorldRegion region, WorldLoader world)
        {
            _region = region;
            _world = world;
        }

        public string Name => _world.GetString(_region.NameId);
        public string Description => _world.GetString(_region.DescriptionId);
        public WorldRegion Region => _region;
    }

    public class LocationWrapper
    {
        private readonly Location _location;
        private readonly WorldLoader _world;

        public LocationWrapper(Location location, WorldLoader world)
        {
            _location = location;
            _world = world;
        }

        public string Name => _world.GetString(_location.NameId);
        public string Description => _world.GetString(_location.DescriptionId);
        public string Type => _world.GetString(_location.TypeId);
        public Location Location => _location;
    }

    public static class LuaTableExtensions
    {
        public static LuaTable ToLuaTable(this IEnumerable<RegionWrapper> items, Lua lua)
        {
            var table = lua.DoString("return {}")[0] as LuaTable;
            if (table == null) return null;

            int index = 1;
            foreach (var item in items)
            {
                table[index++] = item;
            }

            return table;
        }

        public static LuaTable ToLuaTable(this IEnumerable<LocationWrapper> items, Lua lua)
        {
            var table = lua.DoString("return {}")[0] as LuaTable;
            if (table == null) return null;

            int index = 1;
            foreach (var item in items)
            {
                table[index++] = item;
            }

            return table;
        }
    }
}
