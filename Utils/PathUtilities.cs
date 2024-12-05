using System;
using System.IO;

namespace RPG.Utils
{
    /// <summary>
    /// Provides utility methods for working with file paths.
    /// </summary>
    public static class PathUtilities
    {
        /// <summary>
        /// Gets the name of the application folder.
        /// </summary>
        /// <returns>The name of the application folder.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the platform is not supported.</exception>
        public static string GetApplicationFolder()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix or PlatformID.MacOSX => ".demorpg",
                PlatformID.Win32NT or PlatformID.Win32Windows => "DemoRPG",
                _ => throw new PlatformNotSupportedException($"Platform {Environment.OSVersion.Platform} is not supported"),
            };
        }

        /// <summary>
        /// Gets the base directory for the application.
        /// </summary>
        /// <returns>The base directory for the application.</returns>
        public static string GetBaseDirectory()
        {
            if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
            {
                string? xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (!string.IsNullOrEmpty(xdgDataHome))
                {
                    return xdgDataHome;
                }

                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDir, ".local", "share");
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        /// <summary>
        /// Gets the settings directory for the application.
        /// </summary>
        /// <returns>The settings directory for the application.</returns>
        public static string GetSettingsDirectory()
        {
            return Path.Combine(GetBaseDirectory(), GetApplicationFolder());
        }
    }
}
