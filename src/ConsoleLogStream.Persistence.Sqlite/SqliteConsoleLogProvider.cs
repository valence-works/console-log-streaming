using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using ConsoleLogStream.Core;
using ConsoleLogStream.Core.Internal;
using ConsoleLogStream.Core.Models;
using ConsoleLogStream.Core.Providers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ConsoleLogStream.Persistence.Sqlite;

/// <summary>
/// SQLite-backed console log provider.
/// </summary>
public sealed class SqliteConsoleLogProvider : IConsoleLogProvider, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SqliteConsoleLogOptions _options;
    private readonly IConsoleLogRedactor _redactor;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly InMemoryConsoleLogProvider _liveProvider;
    private readonly Channel<ConsoleLogLine> _writeQueue;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private long _pendingWrites;
    private long _droppedWrites;
    private int _disposed;

    /// <summary>
    /// Initializes a new SQLite provider.
    /// </summary>
    public SqliteConsoleLogProvider(
        IOptions<SqliteConsoleLogOptions> options,
        IConsoleLogRedactor redactor,
        IConsoleLogSourceRegistry sourceRegistry,
        InMemoryConsoleLogProvider liveProvider)
    {
        _options = options.Value;
        _redactor = redactor;
        _sourceRegistry = sourceRegistry;
        _liveProvider = liveProvider;
        _writeQueue = Channel.CreateBounded<ConsoleLogLine>(new BoundedChannelOptions(Math.Max(1, _options.WriteQueueCapacity))
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

        if (_options.InitializeSchemaOnStart)
            EnsureSchema();

        _worker = Task.Run(ProcessQueueAsync);
    }

    /// <summary>
    /// Number of dropped writes caused by a full write queue.
    /// </summary>
    public long DroppedWriteCount => Interlocked.Read(ref _droppedWrites);

    /// <inheritdoc />
    public async ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var receivedAt = DateTimeOffset.UtcNow;
        var redacted = _redactor.Redact(line with { ReceivedAt = receivedAt });
        redacted = redacted with { Source = _sourceRegistry.MarkSeen(redacted.Source, receivedAt) };

        await _liveProvider.PublishAsync(redacted, cancellationToken).ConfigureAwait(false);

        Interlocked.Increment(ref _pendingWrites);
        if (_writeQueue.Writer.TryWrite(redacted))
            return;

        Interlocked.Decrement(ref _pendingWrites);
        Interlocked.Increment(ref _droppedWrites);
    }

    /// <inheritdoc />
    public async ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(filter.Limit ?? 500, 1, 500);
        var items = new List<ConsoleLogLine>();

        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(filter.SourceId))
        {
            conditions.Add("source_id = $sourceId");
            command.Parameters.AddWithValue("$sourceId", filter.SourceId);
        }

        if (filter.Stream is not null)
        {
            conditions.Add("stream = $stream");
            command.Parameters.AddWithValue("$stream", filter.Stream.ToString());
        }

        if (filter.From is not null)
        {
            conditions.Add("received_at >= $from");
            command.Parameters.AddWithValue("$from", ToStorage(filter.From.Value));
        }

        if (filter.To is not null)
        {
            conditions.Add("received_at <= $to");
            command.Parameters.AddWithValue("$to", ToStorage(filter.To.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            conditions.Add("(text LIKE $query OR source_id LIKE $query OR source_display_name LIKE $query OR service_name LIKE $query)");
            command.Parameters.AddWithValue("$query", $"%{filter.Query}%");
        }

        var where = conditions.Count == 0 ? "" : $"WHERE {string.Join(" AND ", conditions)}";
        command.CommandText = $"""
            SELECT id, timestamp, received_at, sequence, stream, text, source_json, truncated
            FROM console_log_lines
            {where}
            ORDER BY received_at DESC, sequence DESC
            LIMIT $take
            """;
        command.Parameters.AddWithValue("$take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            items.Add(ReadLine(reader));

        items.Reverse();
        return new RecentConsoleLogsResult
        {
            Items = items,
            Sources = _sourceRegistry.List().ToArray(),
            Dropped = DroppedWriteCount == 0
                ? []
                :
                [
                    new ConsoleLogDroppedSummary
                    {
                        Reason = "sqlite-write-queue-overflow",
                        Count = DroppedWriteCount
                    }
                ]
        };
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        return _liveProvider.SubscribeAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return _liveProvider.ListSourcesAsync(cancellationToken);
    }

    /// <summary>
    /// Waits until the write queue is drained.
    /// </summary>
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        while (Volatile.Read(ref _pendingWrites) > 0)
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs configured retention cleanup.
    /// </summary>
    public async ValueTask CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();

        if (_options.MaxAge is not null)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM console_log_lines WHERE received_at < $cutoff";
            command.Parameters.AddWithValue("$cutoff", ToStorage(DateTimeOffset.UtcNow.Subtract(_options.MaxAge.Value)));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_options.MaxRows is not null)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM console_log_lines
                WHERE rowid NOT IN (
                    SELECT rowid FROM console_log_lines
                    ORDER BY received_at DESC, sequence DESC
                    LIMIT $maxRows
                )
                """;
            command.Parameters.AddWithValue("$maxRows", _options.MaxRows.Value);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _writeQueue.Writer.TryComplete();
        await FlushAsync().ConfigureAwait(false);

        try
        {
            await _worker.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _stop.Dispose();
    }

    private async Task ProcessQueueAsync()
    {
        var batch = new List<ConsoleLogLine>(_options.BatchSize);

        await foreach (var line in _writeQueue.Reader.ReadAllAsync(_stop.Token).ConfigureAwait(false))
        {
            batch.Add(line);
            while (batch.Count < _options.BatchSize && _writeQueue.Reader.TryRead(out var next))
                batch.Add(next);

            await WriteBatchAsync(batch, _stop.Token).ConfigureAwait(false);
            Interlocked.Add(ref _pendingWrites, -batch.Count);
            batch.Clear();
        }
    }

    private async Task WriteBatchAsync(IReadOnlyCollection<ConsoleLogLine> lines, CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
            return;

        await using var connection = OpenConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (var line in lines)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO console_log_lines (
                    id, timestamp, received_at, sequence, stream, text, source_id,
                    source_display_name, service_name, source_json, truncated
                )
                VALUES (
                    $id, $timestamp, $receivedAt, $sequence, $stream, $text, $sourceId,
                    $sourceDisplayName, $serviceName, $sourceJson, $truncated
                )
                """;
            command.Parameters.AddWithValue("$id", line.Id);
            command.Parameters.AddWithValue("$timestamp", ToStorage(line.Timestamp));
            command.Parameters.AddWithValue("$receivedAt", ToStorage(line.ReceivedAt));
            command.Parameters.AddWithValue("$sequence", line.Sequence);
            command.Parameters.AddWithValue("$stream", line.Stream.ToString());
            command.Parameters.AddWithValue("$text", line.Text);
            command.Parameters.AddWithValue("$sourceId", line.Source.Id);
            command.Parameters.AddWithValue("$sourceDisplayName", line.Source.DisplayName);
            command.Parameters.AddWithValue("$serviceName", (object?)line.Source.ServiceName ?? DBNull.Value);
            command.Parameters.AddWithValue("$sourceJson", JsonSerializer.Serialize(line.Source, JsonOptions));
            command.Parameters.AddWithValue("$truncated", line.Truncated ? 1 : 0);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS console_log_lines (
                id TEXT PRIMARY KEY,
                timestamp TEXT NOT NULL,
                received_at TEXT NOT NULL,
                sequence INTEGER NOT NULL,
                stream TEXT NOT NULL,
                text TEXT NOT NULL,
                source_id TEXT NOT NULL,
                source_display_name TEXT NOT NULL,
                service_name TEXT NULL,
                source_json TEXT NOT NULL,
                truncated INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_console_log_lines_received_at ON console_log_lines(received_at);
            CREATE INDEX IF NOT EXISTS ix_console_log_lines_source_received_at ON console_log_lines(source_id, received_at);
            CREATE INDEX IF NOT EXISTS ix_console_log_lines_stream_received_at ON console_log_lines(stream, received_at);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_options.ConnectionString);
        connection.Open();
        return connection;
    }

    private static ConsoleLogLine ReadLine(SqliteDataReader reader)
    {
        var source = JsonSerializer.Deserialize<ConsoleLogSource>(reader.GetString(6), JsonOptions) ?? new ConsoleLogSource();
        return new ConsoleLogLine
        {
            Id = reader.GetString(0),
            Timestamp = DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.AssumeUniversal),
            ReceivedAt = DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.AssumeUniversal),
            Sequence = reader.GetInt64(3),
            Stream = Enum.Parse<ConsoleStream>(reader.GetString(4)),
            Text = reader.GetString(5),
            Source = source,
            Truncated = reader.GetInt32(7) == 1
        };
    }

    private static string ToStorage(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString("O");
    }
}
