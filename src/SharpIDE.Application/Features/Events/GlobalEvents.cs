using SharpIDE.Application.Features.Debugging;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Events;

public static class GlobalEvents
{
	public static event Func<Task> ProjectsRunningChanged = () => Task.CompletedTask;
	public static void InvokeProjectsRunningChanged() => ProjectsRunningChanged?.Invoke();

	public static event Func<Task> StartedRunningProject = () => Task.CompletedTask;
	public static void InvokeStartedRunningProject() => StartedRunningProject?.Invoke();

	public static event Func<SharpIdeProjectModel, Task> ProjectStartedRunning = _ => Task.CompletedTask;
	public static void InvokeProjectStartedRunning(SharpIdeProjectModel project) => ProjectStartedRunning?.Invoke(project);

	public static event Func<SharpIdeProjectModel, Task> ProjectStoppedRunning = _ => Task.CompletedTask;
	public static void InvokeProjectStoppedRunning(SharpIdeProjectModel project) => ProjectStoppedRunning?.Invoke(project);

	public static event Func<ExecutionStopInfo, Task> DebuggerExecutionStopped = _ => Task.CompletedTask;
	public static void InvokeDebuggerExecutionStopped(ExecutionStopInfo executionStopInfo) => DebuggerExecutionStopped?.Invoke(executionStopInfo);
}
