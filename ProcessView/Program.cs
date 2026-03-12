using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessView;
using ProcessView.Data;
using ProcessView.Interop;

var config = AppConfig.FromEnvironmentAndArgs(args);

Console.WriteLine($"ProcessView starting. DB: {config.DatabasePath}, interval: {config.IntervalMilliseconds} ms");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    Console.WriteLine("Cancellation requested. Stopping...");
    eventArgs.Cancel = true;
    cts.Cancel();
};

var repository = new SQLiteProcessRepository(config.DatabasePath);
await repository.InitializeSchemaAsync().ConfigureAwait(false);

await RunLoopAsync(repository, config.IntervalMilliseconds, cts.Token).ConfigureAwait(false);

static async Task RunLoopAsync(SQLiteProcessRepository repository, int intervalMs, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var started = DateTimeOffset.UtcNow;
        long snapshotId = repository.CreateSnapshotId();

        try
        {
            var processes = Toolhelp32.TakeSnapshot();
            await repository.InsertSnapshotAsync(snapshotId, processes).ConfigureAwait(false);

            var finished = DateTimeOffset.UtcNow;
            var duration = finished - started;
            Console.WriteLine(
                $"Snapshot {snapshotId}: {processes.Count} processes recorded in {duration.TotalMilliseconds:F1} ms");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during snapshot {snapshotId}: {ex}");
        }

        try
        {
            await Task.Delay(intervalMs, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Graceful exit
            break;
        }
    }

    Console.WriteLine("ProcessView stopped.");
}

