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
        private const int DefaultFileSizeLimitBytes = 10 * 1024 * 1024; // 10MB
        private const int DefaultRetainedFileCount = 3; // Keep 3 log files

        /// <summary>
        /// Gets the singleton logger instance. Creates a new instance if one does not exist.
        /// </summary>
        /// <value>
        /// The configured Serilog logger instance.
        /// </value>
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
            try
            {
                string logDirectory = Path.Combine(PathUtilities.GetSettingsDirectory(), "Logs");
                Directory.CreateDirectory(logDirectory);

                string logPath = Path.Combine(logDirectory, "game-.log");

                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: DefaultRetainedFileCount,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}Message: {Message:lj}{NewLine}Properties: {@Properties}{NewLine}{Exception}",
                        fileSizeLimitBytes: DefaultFileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .Enrich.WithEnvironmentUserName()
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                // Create and return the fallback logger
                Serilog.Core.Logger fallbackLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("log-.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: DefaultRetainedFileCount,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}Message: {Message:lj}{NewLine}Properties: {@Properties}{NewLine}{Exception}",
                        fileSizeLimitBytes: DefaultFileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();

                fallbackLogger.Error(ex, "Failed to create full logger configuration. Falling back to basic logger.");
                return fallbackLogger;
            }
        }

        /// <summary>
        /// Shuts down the logger and releases all resources. Any subsequent logging calls will create a new logger instance.
        /// </summary>
        public static void Shutdown()
        {
            if (_instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error shutting down logger: {ex.Message}");
                }
            }
            _instance = null;
        }

        /// <summary>
        /// Logs a debug message to the configured logging destinations.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Debug(string message, params object[] propertyValues)
        {
            Instance.Debug(message, propertyValues);
        }

        /// <summary>
        /// Logs an informational message to the configured logging destinations.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Info(string message, params object[] propertyValues)
        {
            Instance.Information(message, propertyValues);
        }

        /// <summary>
        /// Logs a warning message to the configured logging destinations.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Warning(string message, params object[] propertyValues)
        {
            Instance.Warning(message, propertyValues);
        }

        /// <summary>
        /// Logs an error message to the configured logging destinations.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Error(string message, params object[] propertyValues)
        {
            Instance.Error(message, propertyValues);
        }

        /// <summary>
        /// Logs an error message and associated exception to the configured logging destinations.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Error(Exception ex, string message, params object[] propertyValues)
        {
            Instance.Error(ex, message, propertyValues);
        }

        /// <summary>
        /// Logs a fatal error message and associated exception to the configured logging destinations.
        /// This will also log an additional termination message and force a flush of the log buffer.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="propertyValues">Optional values to be included as structured properties with the log message.</param>
        public static void Fatal(Exception ex, string message, params object[] propertyValues)
        {
            Instance.Fatal(ex, message, propertyValues);
            Instance.Fatal(ex, "Application terminating due to fatal exception");
            ForceFlush();
        }

        /// <summary>
        /// Forces an immediate flush of any buffered log entries and disposes of the current logger instance.
        /// Subsequent logging calls will create a new logger instance.
        /// </summary>
        public static void ForceFlush()
        {
            if (_instance is IDisposable disposable)
            {
                disposable.Dispose();
                _instance = null;
            }
        }
    }
}
