namespace ConsoleLogStream.Core;

/// <summary>
/// Installs and removes managed stdout/stderr capture.
/// </summary>
public interface IConsoleLogCapture : IAsyncDisposable
{
    /// <summary>
    /// Starts capture.
    /// </summary>
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops capture and flushes buffered partial lines.
    /// </summary>
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
