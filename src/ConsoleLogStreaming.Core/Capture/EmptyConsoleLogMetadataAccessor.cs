namespace ConsoleLogStreaming.Core.Capture;

/// <summary>
/// Default metadata accessor that supplies no line metadata.
/// </summary>
public sealed class EmptyConsoleLogMetadataAccessor : IConsoleLogMetadataAccessor
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static readonly EmptyConsoleLogMetadataAccessor Instance = new();

    private EmptyConsoleLogMetadataAccessor()
    {
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetMetadata() => Empty;

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
