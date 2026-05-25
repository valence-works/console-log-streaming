namespace ConsoleLogStream.Persistence.Sqlite;

/// <summary>
/// SQLite console log persistence options.
/// </summary>
public sealed class SqliteConsoleLogOptions
{
    /// <summary>
    /// SQLite connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=console-log-stream.db";

    /// <summary>
    /// Bounded write queue capacity.
    /// </summary>
    public int WriteQueueCapacity { get; set; } = 1024;

    /// <summary>
    /// Maximum batch size per write transaction.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Retention by maximum age. Null disables age retention.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Retention by maximum row count. Null disables row-count retention.
    /// </summary>
    public int? MaxRows { get; set; }

    /// <summary>
    /// Initialize schema on provider creation.
    /// </summary>
    public bool InitializeSchemaOnStart { get; set; } = true;
}
