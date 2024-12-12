using System;

namespace RPG
{
    /// <summary>
    /// Provides extension methods for general use.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Executes the specified action on the object if it is not null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to perform the action on.</param>
        /// <param name="action">The action to perform.</param>
        public static void Let<T>(this T? obj, Action<T> action) where T : class
        {
            if (obj != null)
            {
                action(obj);
            }
        }
    }
}