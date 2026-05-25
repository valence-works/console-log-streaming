using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Tests.Core;

public sealed class ConsoleLineBufferTests
{
    private readonly ConsoleLineBuffer _buffer = new(Options.Create(new ConsoleLogOptions { MaxLineLength = 5, IdleFlushTimeout = TimeSpan.FromSeconds(1) }));

    [Fact]
    public void Append_BuffersPartialWritesUntilNewline()
    {
        Assert.Empty(_buffer.Append("hel", DateTimeOffset.UtcNow));

        var lines = _buffer.Append("lo\n", DateTimeOffset.UtcNow);

        Assert.Equal(["hello"], lines.Select(x => x.Text));
    }

    [Fact]
    public void Append_TruncatesAtMaximumLengthAndDropsUntilNewline()
    {
        var lines = _buffer.Append("hello!", DateTimeOffset.UtcNow);

        Assert.Equal(["hello"], lines.Select(x => x.Text));
        Assert.True(Assert.Single(lines).Truncated);
        Assert.Null(_buffer.Flush());

        Assert.Equal(["next"], _buffer.Append("\nnext\n", DateTimeOffset.UtcNow).Select(x => x.Text));
    }

    [Fact]
    public void FlushIfIdle_CompletesBufferedLineAfterTimeout()
    {
        var now = DateTimeOffset.UtcNow;

        _buffer.Append("tail", now);

        Assert.Null(_buffer.FlushIfIdle(now.AddMilliseconds(500)));
        Assert.Equal("tail", _buffer.FlushIfIdle(now.AddSeconds(2))?.Text);
    }

    [Fact]
    public void Append_PreservesEmptyLinesBetweenWrites()
    {
        var lines = _buffer.Append("a\n\nb\n", DateTimeOffset.UtcNow);

        Assert.Equal(["a", "", "b"], lines.Select(x => x.Text));
    }

    [Fact]
    public void Append_PreservesFirstChunkMetadataAcrossPartialWrites()
    {
        _buffer.Append("hel", DateTimeOffset.UtcNow, new Dictionary<string, string> { ["scope"] = "a" });

        var line = Assert.Single(_buffer.Append("lo\n", DateTimeOffset.UtcNow, new Dictionary<string, string> { ["scope"] = "b" }));

        Assert.Equal("hello", line.Text);
        Assert.Equal("a", line.Metadata["scope"]);
    }

    [Fact]
    public void Append_DoesNotAssignMetadataAfterUnscopedPartialWrite()
    {
        _buffer.Append("hel", DateTimeOffset.UtcNow);

        var line = Assert.Single(_buffer.Append("lo\n", DateTimeOffset.UtcNow, new Dictionary<string, string> { ["scope"] = "b" }));

        Assert.Equal("hello", line.Text);
        Assert.Empty(line.Metadata);
    }
}
