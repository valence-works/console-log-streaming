using System.Text.RegularExpressions;

namespace ConsoleLogStream.Core.Capture;

internal static partial class AnsiStripper
{
    public static string Strip(string value) => AnsiRegex().Replace(value, "");

    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled)]
    private static partial Regex AnsiRegex();
}
