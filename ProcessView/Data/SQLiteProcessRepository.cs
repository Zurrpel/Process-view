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
    PRIMARY KEY (SnapshotId, ProcessId)
);

CREATE INDEX IF NOT EXISTS IX_ProcessSnapshot_Snapshot_Parent
    ON ProcessSnapshot (SnapshotId, ParentProcessId);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
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
INSERT INTO ProcessSnapshot (SnapshotId, ProcessId, ParentProcessId, Name)
VALUES ($snapshotId, $pid, $ppid, $name);
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

        foreach (var process in processes)
        {
            pidParam.Value = process.ProcessId;
            ppidParam.Value = process.ParentProcessId;
            nameParam.Value = process.Name;

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        await transaction.CommitAsync().ConfigureAwait(false);
    }
}

