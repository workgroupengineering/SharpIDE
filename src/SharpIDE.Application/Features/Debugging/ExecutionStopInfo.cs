namespace SharpIDE.Application.Features.Debugging;

public class ExecutionStopInfo
{
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required int ThreadId { get; init; }
}
