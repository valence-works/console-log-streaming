using System.Globalization;
using CoreConsoleLogDroppedSummary = ConsoleLogStreaming.Core.Models.ConsoleLogDroppedSummary;
using CoreConsoleLogFilter = ConsoleLogStreaming.Core.Models.ConsoleLogFilter;
using CoreConsoleLogLine = ConsoleLogStreaming.Core.Models.ConsoleLogLine;
using CoreConsoleLogSource = ConsoleLogStreaming.Core.Models.ConsoleLogSource;
using CoreConsoleLogSourceHealth = ConsoleLogStreaming.Core.Models.ConsoleLogSourceHealth;
using CoreConsoleLogStreaming = ConsoleLogStreaming.Core.Models.ConsoleStream;
using CoreConsoleLogStreamingItem = ConsoleLogStreaming.Core.Models.ConsoleLogStreamingItem;
using CoreRecentConsoleLogsResult = ConsoleLogStreaming.Core.Models.RecentConsoleLogsResult;

namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Default mapper between core provider models and transport DTO models.
/// </summary>
public class ConsoleLogStreamingApiMapper : IConsoleLogStreamingApiMapper
{
    /// <inheritdoc />
    public virtual CoreConsoleLogFilter ToCore(ConsoleLogFilter filter) => new()
    {
        SourceId = filter.SourceId,
        Stream = filter.Stream != null ? ToCore(filter.Stream.Value) : null,
        Query = filter.Query,
        Metadata = filter.Metadata,
        From = filter.From,
        To = filter.To,
        Limit = filter.Limit
    };

    /// <inheritdoc />
    public virtual ConsoleLogLine ToApi(CoreConsoleLogLine line) => new()
    {
        Id = line.Id,
        Timestamp = line.Timestamp,
        ReceivedAt = line.ReceivedAt,
        Sequence = line.Sequence,
        Stream = ToApi(line.Stream),
        Text = line.Text,
        Source = ToApi(line.Source),
        Metadata = CopyMetadata(line.Metadata),
        Truncated = line.Truncated,
        Dropped = line.Dropped != null ? ToApi(line.Dropped) : null
    };

    /// <inheritdoc />
    public virtual ConsoleLogSource ToApi(CoreConsoleLogSource source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        ServiceName = source.ServiceName,
        ProcessId = source.ProcessId,
        MachineName = source.MachineName,
        PodName = TryGetMetadata(source.Metadata, ConsoleLogStreamingApiMetadataKeys.KubernetesPodName),
        ContainerName = TryGetMetadata(source.Metadata, ConsoleLogStreamingApiMetadataKeys.ContainerName),
        Namespace = TryGetMetadata(source.Metadata, ConsoleLogStreamingApiMetadataKeys.KubernetesNamespace),
        NodeName = TryGetMetadata(source.Metadata, ConsoleLogStreamingApiMetadataKeys.KubernetesNodeName),
        StartedAt = TryGetTimestampMetadata(source.Metadata, ConsoleLogStreamingApiMetadataKeys.ProcessStartedAt),
        LastSeen = source.LastSeen,
        Health = ToApi(source.Health),
        Metadata = CopyMetadata(source.Metadata)
    };

    /// <inheritdoc />
    public virtual ConsoleLogDroppedSummary ToApi(CoreConsoleLogDroppedSummary summary) => new(
        summary.SourceId,
        summary.Stream != null ? ToApi(summary.Stream.Value) : null,
        MapReason(summary.Reason),
        summary.Count,
        summary.From,
        summary.To);

    /// <inheritdoc />
    public virtual ConsoleLogStreamingItem ToApi(CoreConsoleLogStreamingItem item) => new(
        Line: item.Line != null ? ToApi(item.Line) : null,
        DroppedLines: item.Dropped != null ? ToApi(item.Dropped) : null);

    /// <inheritdoc />
    public virtual RecentConsoleLogsResult ToApi(CoreRecentConsoleLogsResult result) => new(
        result.Items.Select(ToApi).ToList(),
        result.Dropped.Select(ToApi).ToList(),
        result.Sources.Select(ToApi).ToList());

    /// <summary>
    /// Maps a core stream to an API stream.
    /// </summary>
    protected static ConsoleLogStreaming ToApi(CoreConsoleLogStreaming stream) => stream switch
    {
        CoreConsoleLogStreaming.Stderr => ConsoleLogStreaming.Stderr,
        _ => ConsoleLogStreaming.Stdout
    };

    /// <summary>
    /// Maps an API stream to a core stream.
    /// </summary>
    protected static CoreConsoleLogStreaming ToCore(ConsoleLogStreaming stream) => stream switch
    {
        ConsoleLogStreaming.Stderr => CoreConsoleLogStreaming.Stderr,
        _ => CoreConsoleLogStreaming.Stdout
    };

    /// <summary>
    /// Maps a core source health to an API source health.
    /// </summary>
    protected static ConsoleLogSourceHealth ToApi(CoreConsoleLogSourceHealth health) => health switch
    {
        CoreConsoleLogSourceHealth.Stale => ConsoleLogSourceHealth.Stale,
        CoreConsoleLogSourceHealth.Disconnected => ConsoleLogSourceHealth.Disconnected,
        CoreConsoleLogSourceHealth.Unknown => ConsoleLogSourceHealth.Unknown,
        _ => ConsoleLogSourceHealth.Connected
    };

    /// <summary>
    /// Maps internal drop reason identifiers to API-compatible identifiers.
    /// </summary>
    protected static string MapReason(string reason) => reason switch
    {
        "recent-buffer-overflow" => "RecentBufferFull",
        "subscriber-overflow" => "SubscriberChannelFull",
        "capture-channel-overflow" => "CaptureChannelFull",
        _ => reason
    };

    /// <summary>
    /// Creates a case-insensitive metadata copy.
    /// </summary>
    protected static IReadOnlyDictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string> metadata) =>
        metadata.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

    private static string? TryGetMetadata(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static DateTimeOffset? TryGetTimestampMetadata(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp)
            ? timestamp
            : null;
    }
}
