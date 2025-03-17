using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Blvckout.BlvckAuth.API.Utilities;

public static class LoggingHelper
{
    public static void LogDebugWithObject(this ILogger logger, string? message, object? obj)
    {
        if (logger == null || message == null)
            return;
        
        if (logger.IsEnabled(LogLevel.Debug))
        {
            string jsonObject = JsonConvert.SerializeObject(
                obj,
                new JsonSerializerSettings() {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );
            logger.LogDebug(message, jsonObject);
        }
    }
}