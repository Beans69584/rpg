using System;

namespace RPG
{
    public static class Extensions
    {
        public static void Let<T>(this T? obj, Action<T> action) where T : class
        {
            if (obj != null)
            {
                action(obj);
            }
        }
    }
}