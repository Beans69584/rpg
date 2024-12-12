using System;
using System.IO;
using System.Reflection;

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
        public static string GetApplicationFolder()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix or PlatformID.MacOSX => "demorpg",
                PlatformID.Win32NT or PlatformID.Win32Windows => "DemoRPG",
                _ => throw new PlatformNotSupportedException($"Platform {Environment.OSVersion.Platform} is not supported"),
            };
        }

        /// <summary>
        /// Gets the directory containing the executing assembly.
        /// </summary>
        public static string GetAssemblyDirectory()
        {
            return AppContext.BaseDirectory ??
                throw new InvalidOperationException("Unable to determine the assembly directory.");
        }

        /// <summary>
        /// Gets the directory containing prebuilt worlds.
        /// </summary>
        public static string GetWorldsDirectory()
        {
            return Path.Combine(GetAssemblyDirectory(), "worlds");
        }

        /// <summary>
        /// Gets the path to the prebuilt world file.
        /// </summary>
        public static string GetPrebuiltWorldPath(string worldName)
        {
            return Path.Combine(GetWorldsDirectory(), $"{worldName}.rpgw");
        }

        /// <summary>
        /// Gets the base directory for user data.
        /// </summary>
        public static string GetBaseDirectory()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix or PlatformID.MacOSX => Path.Combine(
                    Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")),
                _ => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };
        }

        /// <summary>
        /// Gets the settings directory for the application.
        /// </summary>
        public static string GetSettingsDirectory()
        {
            return Path.Combine(GetBaseDirectory(), GetApplicationFolder());
        }

        /// <summary>
        /// Gets the saves directory for the application.
        /// </summary>
        public static string GetSavesDirectory()
        {
            return Path.Combine(GetSettingsDirectory(), "saves");
        }

        /// <summary>
        /// Gets the backups directory for saves.
        /// </summary>
        public static string GetBackupsDirectory()
        {
            return Path.Combine(GetSavesDirectory(), "backups");
        }

        /// <summary>
        /// Gets the autosaves directory.
        /// </summary>
        public static string GetAutosavesDirectory()
        {
            return Path.Combine(GetSavesDirectory(), "autosaves");
        }

    }
}
