using System;
using System.IO;

namespace ProcessView;

public sealed class AppConfig
{
    public string DatabasePath { get; init; }
    public int IntervalMilliseconds { get; init; }

    private AppConfig(string databasePath, int intervalMilliseconds)
    {
        DatabasePath = databasePath;
        IntervalMilliseconds = intervalMilliseconds;
    }

    public static AppConfig FromEnvironmentAndArgs(string[] args)
    {
        string baseDirectory = AppContext.BaseDirectory;
        string defaultDbPath = Path.Combine(baseDirectory, "process-snapshots.db");

        string dbPath = defaultDbPath;
        int intervalMs = 1000;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--db" when i + 1 < args.Length:
                    dbPath = args[++i];
                    break;
                case "--interval-ms" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed):
                    intervalMs = parsed > 0 ? parsed : intervalMs;
                    i++;
                    break;
            }
        }

        string? envInterval = Environment.GetEnvironmentVariable("PROCESS_VIEW_INTERVAL_MS");
        if (!string.IsNullOrWhiteSpace(envInterval) && int.TryParse(envInterval, out var envParsed) && envParsed > 0)
        {
            intervalMs = envParsed;
        }

        return new AppConfig(dbPath, intervalMs);
    }
}

