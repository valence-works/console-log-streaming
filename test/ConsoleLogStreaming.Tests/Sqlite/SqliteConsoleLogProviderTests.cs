using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Persistence.Sqlite;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStreaming.Tests.Sqlite;

public sealed class SqliteConsoleLogProviderTests
{
    [Fact]
    public async Task PersistsRedactedLinesAcrossProviderRecreation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():n}.db");
        var connectionString = $"Data Source={path}";

        await using (var services = CreateServices(connectionString))
        {
            var provider = services.GetRequiredService<IConsoleLogProvider>();
            var sqlite = services.GetRequiredService<SqliteConsoleLogProvider>();
            var source = services.GetRequiredService<IConsoleLogSourceRegistry>().Current;

            await provider.PublishAsync(new ConsoleLogLine
            {
                Source = source,
                Stream = ConsoleStream.Stdout,
                Text = "database password=secret",
                Sequence = 1
            });
            await sqlite.FlushAsync();
        }

        await using (var services = CreateServices(connectionString))
        {
            var provider = services.GetRequiredService<IConsoleLogProvider>();
            var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

            Assert.Contains(result.Items, x => x.Text == "database password=[redacted]");
        }
    }

    [Fact]
    public async Task RetentionKeepsConfiguredMaximumRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():n}.db");
        await using var services = CreateServices($"Data Source={path}", options => options.MaxRows = 1);
        var provider = services.GetRequiredService<IConsoleLogProvider>();
        var sqlite = services.GetRequiredService<SqliteConsoleLogProvider>();
        var source = services.GetRequiredService<IConsoleLogSourceRegistry>().Current;

        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "one", Sequence = 1 });
        await provider.PublishAsync(new ConsoleLogLine { Source = source, Stream = ConsoleStream.Stdout, Text = "two", Sequence = 2 });
        await sqlite.FlushAsync();
        await sqlite.CleanupAsync();

        var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Single(result.Items);
        Assert.Equal("two", result.Items[0].Text);
    }

    private static ServiceProvider CreateServices(string connectionString, Action<SqliteConsoleLogOptions>? configure = null)
    {
        return new ServiceCollection()
            .AddConsoleLogStreaming(options => options.SourceId = "sqlite-test")
            .AddConsoleLogStreamingSqlite(options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            })
            .BuildServiceProvider();
    }
}
