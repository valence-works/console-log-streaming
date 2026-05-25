namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Current health of a console log source.
/// </summary>
public enum ConsoleLogSourceHealth
{
    /// <summary>
    /// The source health is unknown.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The source has recently emitted output.
    /// </summary>
    Connected = 0,

    /// <summary>
    /// The source has not emitted output within the configured stale window.
    /// </summary>
    Stale = 1,

    /// <summary>
    /// The source has disconnected.
    /// </summary>
    Disconnected = 2
}
