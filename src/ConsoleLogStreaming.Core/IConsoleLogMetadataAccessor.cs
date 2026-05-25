namespace ConsoleLogStreaming.Core;

/// <summary>
/// Supplies optional metadata for console lines at capture time.
/// </summary>
public interface IConsoleLogMetadataAccessor
{
    /// <summary>
    /// Gets metadata for the currently captured console write.
    /// </summary>
    IReadOnlyDictionary<string, string> GetMetadata();
}
