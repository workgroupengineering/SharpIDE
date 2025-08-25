using SharpIDE.Application.Features.Debugging.Experimental;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Debugging;

public class Debugger
{
	public required SharpIdeProjectModel Project { get; init; }
	public required int ProcessId { get; init; }
	private DebuggingService _debuggingService = new DebuggingService();
	public async Task Attach(CancellationToken cancellationToken)
	{
		await _debuggingService.Attach(ProcessId, cancellationToken);
	}
}
