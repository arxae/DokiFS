using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DokiFS.Logging;

public static class DokiFSLogger
{
    static ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// Sets the logger factory to be used by DokiFS.
    /// </summary>
    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
        => DokiFSLogger.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    /// <summary>
    /// Creates a logger for the specified category name.
    /// </summary>
    public static ILogger CreateLogger(string categoryName)
        => loggerFactory.CreateLogger(categoryName);

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    public static ILogger<T> CreateLogger<T>()
        => loggerFactory.CreateLogger<T>();
}
