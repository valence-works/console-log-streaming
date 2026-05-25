using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core.Internal;

internal static class ConsoleLogFilterMatcher
{
    public static bool IsMatch(ConsoleLogLine line, ConsoleLogFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SourceId) && !string.Equals(line.Source.Id, filter.SourceId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.Stream is not null && line.Stream != filter.Stream)
            return false;

        if (filter.From is not null && line.ReceivedAt < filter.From)
            return false;

        if (filter.To is not null && line.ReceivedAt > filter.To)
            return false;

        if (!string.IsNullOrWhiteSpace(filter.Query) && !ContainsQuery(line, filter.Query))
            return false;

        if (!MatchesMetadata(line.Metadata, filter.Metadata))
            return false;

        return true;
    }

    private static bool ContainsQuery(ConsoleLogLine line, string query)
    {
        return line.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || line.Source.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || line.Source.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (line.Source.ServiceName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (line.Source.MachineName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || line.Metadata.Any(x => x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            || line.Source.Metadata.Any(x => x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesMetadata(IReadOnlyDictionary<string, string> lineMetadata, IReadOnlyDictionary<string, string> filterMetadata)
    {
        foreach (var (key, value) in filterMetadata)
        {
            if (!lineMetadata.TryGetValue(key, out var candidate) || !string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
