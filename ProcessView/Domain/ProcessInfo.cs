namespace ProcessView.Domain;

public sealed class ProcessInfo
{
    public int ProcessId { get; init; }
    public int ParentProcessId { get; init; }
    public string Name { get; init; } = string.Empty;
}

