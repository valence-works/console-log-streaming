namespace ConsoleLogStream.Core.Models;

/// <summary>
/// Identifies the managed console stream that produced a line.
/// </summary>
public enum ConsoleStream
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
