using ConsoleLogStream.Core.Models;
using ConsoleLogStream.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStream.Core.Sources;

/// <summary>
/// In-memory source registry for the local process.
/// </summary>
public sealed class ConsoleLogSourceRegistry : IConsoleLogSourceRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ConsoleLogSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the source registry.
    /// </summary>
    public ConsoleLogSourceRegistry(IOptions<ConsoleLogOptions> options)
    {
        var value = options.Value;
        var processId = Environment.ProcessId;
        var machineName = Environment.MachineName;
        var id = value.SourceId ?? $"{machineName}:{processId}";
        var displayName = value.SourceDisplayName ?? id;

        Current = new ConsoleLogSource
        {
            Id = id,
            DisplayName = displayName,
            ServiceName = value.ServiceName,
            ProcessId = processId,
            MachineName = machineName,
            Health = ConsoleLogSourceHealth.Connected,
            Metadata = new Dictionary<string, string>(value.SourceMetadata, StringComparer.OrdinalIgnoreCase)
        };

        _sources[Current.Id] = Current;
    }

    /// <inheritdoc />
    public ConsoleLogSource Current { get; }

    /// <inheritdoc />
    public ConsoleLogSource MarkSeen(ConsoleLogSource source, DateTimeOffset timestamp)
    {
        lock (_gate)
        {
            var updated = source with { LastSeen = timestamp, Health = ConsoleLogSourceHealth.Connected };
            _sources[updated.Id] = updated;
            return updated;
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ConsoleLogSource> List()
    {
        lock (_gate)
            return _sources.Values.ToArray();
    }
}
