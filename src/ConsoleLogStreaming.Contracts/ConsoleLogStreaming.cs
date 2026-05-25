namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Identifies the managed console stream that produced a line.
/// </summary>
public enum ConsoleLogStreaming
{
    /// <summary>
    /// Standard output.
    /// </summary>
    Stdout = 0,

    /// <summary>
    /// Standard error.
    /// </summary>
    Stderr = 1
}
