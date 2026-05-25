using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStreaming.Tests.Core;

[Collection("Console capture")]
public sealed class ConsoleCaptureTests
{
    [Fact]
    public async Task CapturesStdoutAndStderrAsDistinctRedactedLines()
    {
        await using var provider = new ServiceCollection()
            .AddConsoleLogStreaming(options =>
            {
                options.SourceId = "test-source";
                options.IdleFlushTimeout = TimeSpan.FromMilliseconds(20);
            })
            .BuildServiceProvider();

        var capture = provider.GetRequiredService<IConsoleLogCapture>();
        await capture.StartAsync();

        Console.WriteLine("stdout password=secret");
        Console.Error.WriteLine("stderr token=abc123");
        await capture.StopAsync();

        var logs = await provider.GetRequiredService<IConsoleLogProvider>().GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Contains(logs.Items, x => x.Stream == ConsoleStream.Stdout && x.Text == "stdout password=[redacted]");
        Assert.Contains(logs.Items, x => x.Stream == ConsoleStream.Stderr && x.Text == "stderr token=[redacted]");
    }

    [Fact]
    public async Task FlushesPartialLineOnStop()
    {
        await using var provider = new ServiceCollection()
            .AddConsoleLogStreaming(options => options.SourceId = "test-source")
            .BuildServiceProvider();

        var capture = provider.GetRequiredService<IConsoleLogCapture>();
        await capture.StartAsync();

        Console.Write("partial");
        await capture.StopAsync();

        var logs = await provider.GetRequiredService<IConsoleLogProvider>().GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Contains(logs.Items, x => x.Text == "partial");
    }

    [Fact]
    public async Task StripsAnsiByDefault()
    {
        await using var provider = new ServiceCollection()
            .AddConsoleLogStreaming(options =>
            {
                options.SourceId = "test-source";
            })
            .BuildServiceProvider();

        var capture = provider.GetRequiredService<IConsoleLogCapture>();
        await capture.StartAsync();

        Console.Write("\u001b[31mred\u001b[0m\n");
        await capture.StopAsync();

        var logs = await provider.GetRequiredService<IConsoleLogProvider>().GetRecentAsync(new ConsoleLogFilter { Limit = 10 });
        var line = Assert.Single(logs.Items);
        Assert.False(line.Truncated);
        Assert.Equal("red", line.Text);
    }

    [Fact]
    public async Task TruncatesOversizedLine()
    {
        await using var provider = new ServiceCollection()
            .AddConsoleLogStreaming(options =>
            {
                options.SourceId = "test-source";
                options.MaxLineLength = 5;
            })
            .BuildServiceProvider();

        var capture = provider.GetRequiredService<IConsoleLogCapture>();
        await capture.StartAsync();

        Console.Write("abcdef\n");
        await capture.StopAsync();

        var logs = await provider.GetRequiredService<IConsoleLogProvider>().GetRecentAsync(new ConsoleLogFilter { Limit = 10 });
        var line = Assert.Single(logs.Items);
        Assert.True(line.Truncated);
        Assert.Equal("abcde", line.Text);
    }
}
