using Microsoft.Extensions.Logging;

namespace DokiFS.Logging;

public static partial class GenericLogMessages
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in {namespaceName}.{method}")]
    public static partial void LogErrorException(this ILogger logger, string namespaceName, string method, Exception ex = null);
}
