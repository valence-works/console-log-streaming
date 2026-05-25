using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Tests.Core;

public sealed class ConsoleLineFormatterTests
{
    [Fact]
    public void Format_StripsAnsiByDefault()
    {
        var formatter = new ConsoleLineFormatter(Options.Create(new ConsoleLogOptions()));

        var result = formatter.Format("\u001b[31mred\u001b[0m");

        Assert.Equal("red", result.Text);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Format_PreservesAnsiWhenConfigured()
    {
        var formatter = new ConsoleLineFormatter(Options.Create(new ConsoleLogOptions { PreserveAnsi = true }));

        var result = formatter.Format("\u001b[31mred\u001b[0m");

        Assert.Equal("\u001b[31mred\u001b[0m", result.Text);
    }

    [Fact]
    public void Format_TruncatesOversizedLine()
    {
        var formatter = new ConsoleLineFormatter(Options.Create(new ConsoleLogOptions { MaxLineLength = 3 }));

        var result = formatter.Format("abcdef");

        Assert.Equal("abc", result.Text);
        Assert.True(result.Truncated);
    }
}
