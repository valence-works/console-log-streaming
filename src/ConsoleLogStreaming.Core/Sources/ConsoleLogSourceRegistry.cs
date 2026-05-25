using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Sources;

/// <summary>
/// In-memory source registry for the local process.
/// </summary>
public sealed class ConsoleLogSourceRegistry : IConsoleLogSourceRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ConsoleLogSource> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConsoleLogOptions _options;

    /// <summary>
    /// Initializes a new instance of the source registry.
    /// </summary>
    public ConsoleLogSourceRegistry(IOptions<ConsoleLogOptions> options)
    {
        var value = options.Value;
        _options = value;
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
    public event Action<ConsoleLogSource>? SourceChanged;

    /// <inheritdoc />
    public ConsoleLogSource Current { get; }

    /// <inheritdoc />
    public ConsoleLogSource MarkSeen(ConsoleLogSource source, DateTimeOffset timestamp)
    {
        ConsoleLogSource? changed = null;
        ConsoleLogSource updated;

        lock (_gate)
        {
            _sources.TryGetValue(source.Id, out var existing);
            var current = existing != null ? ApplyCurrentHealth(existing, DateTimeOffset.UtcNow) : null;
            updated = source with { LastSeen = timestamp, Health = ConsoleLogSourceHealth.Connected };
            _sources[updated.Id] = updated;

            if (current == null || current.Health != updated.Health)
                changed = updated;
        }

        if (changed != null)
            SourceChanged?.Invoke(changed);

        return updated;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ConsoleLogSource> List()
    {
        var changed = new List<ConsoleLogSource>();
        ConsoleLogSource[] sources;
        var now = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            foreach (var (sourceId, source) in _sources)
            {
                var updated = ApplyCurrentHealth(source, now);
                if (updated.Health == source.Health)
                    continue;

                _sources[sourceId] = updated;
                changed.Add(updated);
            }

            sources = _sources.Values
                .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        foreach (var source in changed)
            SourceChanged?.Invoke(source);

        return sources;
    }

    private ConsoleLogSource ApplyCurrentHealth(ConsoleLogSource source, DateTimeOffset now)
    {
        if (source.LastSeen == null)
            return source;

        var staleBefore = now.Subtract(_options.SourceHeartbeatTimeout);
        return source.LastSeen < staleBefore ? source with { Health = ConsoleLogSourceHealth.Stale } : source;
    }
}
