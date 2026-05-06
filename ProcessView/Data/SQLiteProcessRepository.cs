using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ProcessView.Domain;

namespace ProcessView.Data;

public sealed class SQLiteProcessRepository
{
    private readonly string _connectionString;

    public SQLiteProcessRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path must be provided.", nameof(databasePath));
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
    }

    public async Task InitializeSchemaAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = """
CREATE TABLE IF NOT EXISTS ProcessSnapshot (
    SnapshotId      INTEGER NOT NULL,
    ProcessId       INTEGER NOT NULL,
    ParentProcessId INTEGER NOT NULL,
    Name            TEXT    NOT NULL,
    ThreadCount      INTEGER NOT NULL,
    UsageCount       INTEGER NOT NULL,
    ModuleId         INTEGER NOT NULL,
    DefaultHeapId    INTEGER NOT NULL,
    PriorityClassBase INTEGER NOT NULL,
    Flags            INTEGER NOT NULL,
    PRIMARY KEY (SnapshotId, ProcessId)
);

CREATE INDEX IF NOT EXISTS IX_ProcessSnapshot_Snapshot_Parent
    ON ProcessSnapshot (SnapshotId, ParentProcessId);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        // If the DB already existed, CREATE TABLE IF NOT EXISTS won't add new columns.
        // Ensure required columns are present (add-only) with a safe default for existing rows.
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "ThreadCount", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "UsageCount", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "ModuleId", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "DefaultHeapId", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "PriorityClassBase", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
        await EnsureColumnAsync(connection, table: "ProcessSnapshot", column: "Flags", definition: "INTEGER NOT NULL DEFAULT 0").ConfigureAwait(false);
    }

    public long CreateSnapshotId()
    {
        // Используем Unix‑таймстамп в миллисекундах как SnapshotId
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public async Task InsertSnapshotAsync(long snapshotId, IReadOnlyCollection<ProcessInfo> processes)
    {
        if (processes.Count == 0)
        {
            return;
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        const string insertSql = """
INSERT INTO ProcessSnapshot (
    SnapshotId,
    ProcessId,
    ParentProcessId,
    Name,
    ThreadCount,
    UsageCount,
    ModuleId,
    DefaultHeapId,
    PriorityClassBase,
    Flags
)
VALUES (
    $snapshotId,
    $pid,
    $ppid,
    $name,
    $threadCount,
    $usageCount,
    $moduleId,
    $defaultHeapId,
    $priorityClassBase,
    $flags
);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = insertSql;

        var snapshotParam = command.CreateParameter();
        snapshotParam.ParameterName = "$snapshotId";
        snapshotParam.Value = snapshotId;
        command.Parameters.Add(snapshotParam);

        var pidParam = command.CreateParameter();
        pidParam.ParameterName = "$pid";
        command.Parameters.Add(pidParam);

        var ppidParam = command.CreateParameter();
        ppidParam.ParameterName = "$ppid";
        command.Parameters.Add(ppidParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "$name";
        command.Parameters.Add(nameParam);

        var threadCountParam = command.CreateParameter();
        threadCountParam.ParameterName = "$threadCount";
        command.Parameters.Add(threadCountParam);

        var usageCountParam = command.CreateParameter();
        usageCountParam.ParameterName = "$usageCount";
        command.Parameters.Add(usageCountParam);

        var moduleIdParam = command.CreateParameter();
        moduleIdParam.ParameterName = "$moduleId";
        command.Parameters.Add(moduleIdParam);

        var defaultHeapIdParam = command.CreateParameter();
        defaultHeapIdParam.ParameterName = "$defaultHeapId";
        command.Parameters.Add(defaultHeapIdParam);

        var priorityClassBaseParam = command.CreateParameter();
        priorityClassBaseParam.ParameterName = "$priorityClassBase";
        command.Parameters.Add(priorityClassBaseParam);

        var flagsParam = command.CreateParameter();
        flagsParam.ParameterName = "$flags";
        command.Parameters.Add(flagsParam);

        foreach (var process in processes)
        {
            pidParam.Value = process.ProcessId;
            ppidParam.Value = process.ParentProcessId;
            nameParam.Value = process.Name;
            threadCountParam.Value = process.ThreadCount;
            usageCountParam.Value = process.UsageCount;
            moduleIdParam.Value = process.ModuleId;
            defaultHeapIdParam.Value = process.DefaultHeapId;
            priorityClassBaseParam.Value = process.PriorityClassBase;
            flagsParam.Value = process.Flags;

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        await transaction.CommitAsync().ConfigureAwait(false);
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, string definition)
    {
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({table});";

        await using var reader = await pragma.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var existingName = reader.GetString(1);
            if (string.Equals(existingName, column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        await alter.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}

