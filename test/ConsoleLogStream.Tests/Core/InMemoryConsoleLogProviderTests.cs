using ConsoleLogStream.Core;
using ConsoleLogStream.Core.DependencyInjection;
using ConsoleLogStream.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStream.Tests.Core;

public sealed class InMemoryConsoleLogProviderTests
{
    [Fact]
    public async Task RecentQueriesApplyFiltersAndLimits()
    {
        var services = new ServiceCollection()
            .AddConsoleLogStream(options =>
            {
                options.SourceId = "source-a";
                options.MaxRecentQuerySize = 2;
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IConsoleLogProvider>();
        var source = services.GetRequiredService<IConsoleLogSourceRegistry>().Current;

        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "alpha", Sequence = 1 });
        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stderr, Text = "beta", Sequence = 2 });
        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "alphabet", Sequence = 3 });

        var result = await provider.GetRecentAsync(new ConsoleLogFilter
        {
            Stream = ConsoleStream.Stdout,
            Query = "alpha",
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, x => Assert.Equal(ConsoleStream.Stdout, x.Stream));
    }

    [Fact]
    public async Task RecentBufferIsBoundedAndReportsDrops()
    {
        var services = new ServiceCollection()
            .AddConsoleLogStream(options =>
            {
                options.SourceId = "source-a";
                options.RecentCapacity = 1;
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IConsoleLogProvider>();
        var source = services.GetRequiredService<IConsoleLogSourceRegistry>().Current;

        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "one", Sequence = 1 });
        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "two", Sequence = 2 });

        var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Single(result.Items);
        Assert.Equal("two", result.Items[0].Text);
        Assert.Contains(result.Dropped, x => x.Reason == "recent-buffer-overflow");
    }
}
