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
        /// Shuts down the logger.
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

        // Enhanced convenience methods with context support
        public static void Debug(string message, params object[] propertyValues)
        {
            Instance.Debug(message, propertyValues);
        }

        public static void Info(string message, params object[] propertyValues)
        {
            Instance.Information(message, propertyValues);
        }

        public static void Warning(string message, params object[] propertyValues)
        {
            Instance.Warning(message, propertyValues);
        }

        public static void Error(string message, params object[] propertyValues)
        {
            Instance.Error(message, propertyValues);
        }

        public static void Error(Exception ex, string message, params object[] propertyValues)
        {
            Instance.Error(ex, message, propertyValues);
        }

        public static void Fatal(Exception ex, string message, params object[] propertyValues)
        {
            Instance.Fatal(ex, message, propertyValues);
            Instance.Fatal(ex, "Application terminating due to fatal exception");
            ForceFlush();
        }

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
