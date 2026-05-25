namespace ConsoleLogStream.AspNetCore;

/// <summary>
/// ASP.NET Core adapter options.
/// </summary>
public sealed class ConsoleLogStreamAspNetCoreOptions
{
    /// <summary>
    /// Recent console logs endpoint path.
    /// </summary>
    public string RecentPath { get; set; } = "/diagnostics/console-logs/recent";

    /// <summary>
    /// Console sources endpoint path.
    /// </summary>
    public string SourcesPath { get; set; } = "/diagnostics/console-logs/sources";

    /// <summary>
    /// SignalR hub path.
    /// </summary>
    public string HubPath { get; set; } = "/hubs/console-logs";

    /// <summary>
    /// Optional ASP.NET Core authorization policy.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }
}
