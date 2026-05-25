using CoreConsoleLogDroppedSummary = ConsoleLogStreaming.Core.Models.ConsoleLogDroppedSummary;
using CoreConsoleLogFilter = ConsoleLogStreaming.Core.Models.ConsoleLogFilter;
using CoreConsoleLogLine = ConsoleLogStreaming.Core.Models.ConsoleLogLine;
using CoreConsoleLogSource = ConsoleLogStreaming.Core.Models.ConsoleLogSource;
using CoreConsoleLogStreamingItem = ConsoleLogStreaming.Core.Models.ConsoleLogStreamingItem;
using CoreRecentConsoleLogsResult = ConsoleLogStreaming.Core.Models.RecentConsoleLogsResult;

namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Maps between core provider models and transport DTO models.
/// </summary>
public interface IConsoleLogStreamingApiMapper
{
    /// <summary>
    /// Maps an API filter DTO to a core filter.
    /// </summary>
    CoreConsoleLogFilter ToCore(ConsoleLogFilter filter);

    /// <summary>
    /// Maps a core line to an API DTO.
    /// </summary>
    ConsoleLogLine ToApi(CoreConsoleLogLine line);

    /// <summary>
    /// Maps a core source to an API DTO.
    /// </summary>
    ConsoleLogSource ToApi(CoreConsoleLogSource source);

    /// <summary>
    /// Maps a core dropped summary to an API DTO.
    /// </summary>
    ConsoleLogDroppedSummary ToApi(CoreConsoleLogDroppedSummary summary);

    /// <summary>
    /// Maps a core stream item to an API DTO.
    /// </summary>
    ConsoleLogStreamingItem ToApi(CoreConsoleLogStreamingItem item);

    /// <summary>
    /// Maps a core recent result to an API DTO.
    /// </summary>
    RecentConsoleLogsResult ToApi(CoreRecentConsoleLogsResult result);
}
