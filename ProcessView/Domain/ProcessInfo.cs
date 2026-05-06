namespace ProcessView.Domain;

public sealed class ProcessInfo
{
    public int ProcessId { get; init; }
    public int ParentProcessId { get; init; }
    public string Name { get; init; } = string.Empty;

    public int ThreadCount { get; init; }
    public int UsageCount { get; init; }
    public int ModuleId { get; init; }
    public long DefaultHeapId { get; init; }
    public int PriorityClassBase { get; init; }
    public int Flags { get; init; }
}

