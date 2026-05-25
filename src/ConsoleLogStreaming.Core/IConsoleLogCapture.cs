namespace ConsoleLogStreaming.Core;

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

    /// <summary>
    /// Flushes buffered partial lines that have exceeded the configured idle timeout.
    /// </summary>
    ValueTask FlushIdleAsync(CancellationToken cancellationToken = default);
}
