using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Core.Providers;
using ConsoleLogStreaming.Core.Redaction;
using ConsoleLogStreaming.Core.Sources;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core;

/// <summary>
/// Process-wide owner for managed console capture and provider services.
/// </summary>
public static class ConsoleLogStreamingHost
{
    private static readonly object Lock = new();
    private static Action<ConsoleLogOptions>? _pendingConfigure;
    private static Func<ConsoleLogStreamingHostContext, IConsoleLogProvider>? _providerFactory;
    private static Func<ConsoleLogStreamingHostContext, IConsoleLogMetadataAccessor>? _metadataAccessorFactory;
    private static Lazy<HostState> _state = CreateLazy();
    private static int _leases;

    /// <summary>
    /// Applies configuration before the host is initialized.
    /// </summary>
    public static bool Configure(Action<ConsoleLogOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        lock (Lock)
        {
            if (_state.IsValueCreated)
                return false;

            _pendingConfigure = (Action<ConsoleLogOptions>?)Delegate.Combine(_pendingConfigure, configure);
            return true;
        }
    }

    /// <summary>
    /// Configures the provider used by process-wide capture.
    /// </summary>
    public static bool ConfigureProvider(Func<ConsoleLogStreamingHostContext, IConsoleLogProvider> providerFactory)
    {
        ArgumentNullException.ThrowIfNull(providerFactory);

        lock (Lock)
        {
            if (_state.IsValueCreated || _providerFactory != null)
                return false;

            _providerFactory = providerFactory;
            return true;
        }
    }

    /// <summary>
    /// Configures the metadata accessor used by process-wide capture.
    /// </summary>
    public static bool ConfigureMetadataAccessor(Func<ConsoleLogStreamingHostContext, IConsoleLogMetadataAccessor> metadataAccessorFactory)
    {
        ArgumentNullException.ThrowIfNull(metadataAccessorFactory);

        lock (Lock)
        {
            if (_state.IsValueCreated || _metadataAccessorFactory != null)
                return false;

            _metadataAccessorFactory = metadataAccessorFactory;
            return true;
        }
    }

    /// <summary>
    /// Current options.
    /// </summary>
    public static IOptions<ConsoleLogOptions> Options => _state.Value.Options;

    /// <summary>
    /// Current source registry.
    /// </summary>
    public static IConsoleLogSourceRegistry SourceRegistry => _state.Value.SourceRegistry;

    /// <summary>
    /// Current redaction pipeline.
    /// </summary>
    public static IConsoleLogRedactionPipeline RedactionPipeline => _state.Value.RedactionPipeline;

    /// <summary>
    /// Current formatter.
    /// </summary>
    public static ConsoleLineFormatter Formatter => _state.Value.Formatter;

    /// <summary>
    /// Current metadata accessor.
    /// </summary>
    public static IConsoleLogMetadataAccessor MetadataAccessor => _state.Value.MetadataAccessor;

    /// <summary>
    /// Current provider.
    /// </summary>
    public static IConsoleLogProvider Provider => _state.Value.Provider;

    /// <summary>
    /// Current capture service.
    /// </summary>
    public static IConsoleLogCapture Capture => _state.Value.Capture;

    /// <summary>
    /// Initializes the host if it has not been initialized.
    /// </summary>
    public static bool EnsureInitialized()
    {
        var wasCreated = _state.IsValueCreated;
        _ = _state.Value;
        return !wasCreated;
    }

    /// <summary>
    /// Adds a process-wide host lease.
    /// </summary>
    public static void AddReference()
    {
        lock (Lock)
            _leases++;
    }

    /// <summary>
    /// Releases a process-wide host lease.
    /// </summary>
    public static ValueTask ReleaseReferenceAsync(CancellationToken cancellationToken = default)
    {
        Lazy<HostState> state;

        lock (Lock)
        {
            if (_leases <= 0)
                return ValueTask.CompletedTask;

            _leases--;
            if (_leases > 0)
                return ValueTask.CompletedTask;

            state = ResetState();
        }

        return DisposeStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Stops capture and resets the host. Intended for tests and orderly shutdown.
    /// </summary>
    public static async ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Lazy<HostState> state;

        lock (Lock)
        {
            state = ResetState();
            _leases = 0;
        }

        await DisposeStateAsync(state, cancellationToken).ConfigureAwait(false);
    }

    private static Lazy<HostState> CreateLazy() => new(BuildState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Lazy<HostState> ResetState()
    {
        var state = _state;
        _state = CreateLazy();
        _pendingConfigure = null;
        _providerFactory = null;
        _metadataAccessorFactory = null;
        return state;
    }

    private static async ValueTask DisposeStateAsync(Lazy<HostState> state, CancellationToken cancellationToken)
    {
        if (state.IsValueCreated)
            await state.Value.Capture.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static HostState BuildState()
    {
        var options = new ConsoleLogOptions();
        Func<ConsoleLogStreamingHostContext, IConsoleLogProvider>? providerFactory;
        Func<ConsoleLogStreamingHostContext, IConsoleLogMetadataAccessor>? metadataAccessorFactory;

        lock (Lock)
        {
            _pendingConfigure?.Invoke(options);
            providerFactory = _providerFactory;
            metadataAccessorFactory = _metadataAccessorFactory;
        }

        var wrappedOptions = Microsoft.Extensions.Options.Options.Create(options);
        var sourceRegistry = new ConsoleLogSourceRegistry(wrappedOptions);
        var redactors = new IConsoleLogRedactor[] { new RegexConsoleLogRedactor(wrappedOptions) };
        var redactionPipeline = new ConsoleLogRedactionPipeline(redactors);
        var formatter = new ConsoleLineFormatter(wrappedOptions);
        var context = new ConsoleLogStreamingHostContext(wrappedOptions, sourceRegistry, redactionPipeline, formatter, EmptyConsoleLogMetadataAccessor.Instance);
        var metadataAccessor = metadataAccessorFactory?.Invoke(context) ?? EmptyConsoleLogMetadataAccessor.Instance;
        context = context with { MetadataAccessor = metadataAccessor };
        var provider = providerFactory?.Invoke(context) ?? new InMemoryConsoleLogProvider(wrappedOptions, redactionPipeline, sourceRegistry);
        var capture = new ConsoleCaptureService(provider, sourceRegistry, redactionPipeline, metadataAccessor, formatter, wrappedOptions);

        capture.StartAsync().AsTask().GetAwaiter().GetResult();

        return new HostState(wrappedOptions, sourceRegistry, redactionPipeline, formatter, metadataAccessor, provider, capture);
    }

    private sealed record HostState(
        IOptions<ConsoleLogOptions> Options,
        IConsoleLogSourceRegistry SourceRegistry,
        IConsoleLogRedactionPipeline RedactionPipeline,
        ConsoleLineFormatter Formatter,
        IConsoleLogMetadataAccessor MetadataAccessor,
        IConsoleLogProvider Provider,
        IConsoleLogCapture Capture);
}

/// <summary>
/// Services available while configuring a process-wide console log stream host.
/// </summary>
public sealed record ConsoleLogStreamingHostContext(
    IOptions<ConsoleLogOptions> Options,
    IConsoleLogSourceRegistry SourceRegistry,
    IConsoleLogRedactionPipeline RedactionPipeline,
    ConsoleLineFormatter Formatter,
    IConsoleLogMetadataAccessor MetadataAccessor);
