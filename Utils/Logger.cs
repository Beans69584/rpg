using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using RPG.Utils;

namespace RPG.Utils
{
    /// <summary>
    /// Provides a static instance of a logger for the application.
    /// </summary>
    public static class Logger
    {
        private static ILogger? _instance;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        public static ILogger Instance
        {
            get
            {
                _instance ??= CreateLogger();
                return _instance;
            }
        }

        private static ILogger CreateLogger()
        {
            string logDirectory = Path.Combine(PathUtilities.GetSettingsDirectory(), "Logs");
            Directory.CreateDirectory(logDirectory);

            string logPath = Path.Combine(logDirectory, "game-.log");

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 1024 * 1024,
                    rollOnFileSizeLimit: true)
                .CreateLogger();
        }

        /// <summary>
        /// Shuts down the logger.
        /// </summary>
        public static void Shutdown()
        {
            if (_instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _instance = null;
        }
    }
}
