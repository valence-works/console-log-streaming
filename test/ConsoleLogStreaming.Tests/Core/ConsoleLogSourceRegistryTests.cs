using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Core.Sources;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Tests.Core;

public sealed class ConsoleLogSourceRegistryTests
{
    [Fact]
    public void MarkSeen_AddsNewSourceAndRaisesSourceChanged()
    {
        var registry = CreateRegistry();
        ConsoleLogSource? changed = null;
        registry.SourceChanged += source => changed = source;

        var timestamp = DateTimeOffset.UtcNow;
        var source = registry.MarkSeen(new ConsoleLogSource { Id = "remote", DisplayName = "remote" }, timestamp);

        Assert.Equal("remote", source.Id);
        Assert.Equal(ConsoleLogSourceHealth.Connected, source.Health);
        Assert.Same(source, changed);
        Assert.Contains(registry.List(), x => x.Id == "remote");
    }

    [Fact]
    public void List_MarksInactiveSourcesStale()
    {
        var registry = CreateRegistry(new ConsoleLogOptions { SourceHeartbeatTimeout = TimeSpan.FromSeconds(1) });
        registry.MarkSeen(new ConsoleLogSource { Id = "remote", DisplayName = "remote" }, DateTimeOffset.UtcNow.AddSeconds(-2));

        Assert.Contains(registry.List(), x => x.Id == "remote" && x.Health == ConsoleLogSourceHealth.Stale);
    }

    [Fact]
    public void MarkSeen_RaisesSourceChangedWhenSourceRecoversFromStale()
    {
        var registry = CreateRegistry(new ConsoleLogOptions { SourceHeartbeatTimeout = TimeSpan.FromSeconds(1) });
        registry.MarkSeen(new ConsoleLogSource { Id = "remote", DisplayName = "remote" }, DateTimeOffset.UtcNow.AddSeconds(-2));
        _ = registry.List();
        var changes = new List<ConsoleLogSource>();
        registry.SourceChanged += changes.Add;

        registry.MarkSeen(new ConsoleLogSource { Id = "remote", DisplayName = "remote" }, DateTimeOffset.UtcNow);

        Assert.Contains(changes, x => x.Id == "remote" && x.Health == ConsoleLogSourceHealth.Connected);
    }

    private static ConsoleLogSourceRegistry CreateRegistry(ConsoleLogOptions? options = null) =>
        new(Options.Create(options ?? new ConsoleLogOptions()));
}
