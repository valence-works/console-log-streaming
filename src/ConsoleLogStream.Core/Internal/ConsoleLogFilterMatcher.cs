using ConsoleLogStream.Core.Models;

namespace ConsoleLogStream.Core.Internal;

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

        return true;
    }

    private static bool ContainsQuery(ConsoleLogLine line, string query)
    {
        return line.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || line.Source.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || line.Source.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (line.Source.ServiceName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
