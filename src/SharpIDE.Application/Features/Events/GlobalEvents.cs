using SharpIDE.Application.Features.Debugging;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Events;

public class GlobalEvents
{
	public static GlobalEvents Instance { get; set; } = null!;
	public event Func<Task> ProjectsRunningChanged = () => Task.CompletedTask;
	public void InvokeProjectsRunningChanged() => ProjectsRunningChanged?.InvokeParallelFireAndForget();

	public event Func<Task> StartedRunningProject = () => Task.CompletedTask;
	public void InvokeStartedRunningProject() => StartedRunningProject?.InvokeParallelFireAndForget();

	public event Func<SharpIdeProjectModel, Task> ProjectStartedDebugging = _ => Task.CompletedTask;
	public void InvokeProjectStartedDebugging(SharpIdeProjectModel project) => ProjectStartedDebugging?.InvokeParallelFireAndForget(project);

	public event Func<SharpIdeProjectModel, Task> ProjectStoppedDebugging = _ => Task.CompletedTask;
	public void InvokeProjectStoppedDebugging(SharpIdeProjectModel project) => ProjectStoppedDebugging?.InvokeParallelFireAndForget(project);

	public event Func<SharpIdeProjectModel, Task> ProjectStartedRunning = _ => Task.CompletedTask;
	public void InvokeProjectStartedRunning(SharpIdeProjectModel project) => ProjectStartedRunning?.InvokeParallelFireAndForget(project);

	public event Func<SharpIdeProjectModel, Task> ProjectStoppedRunning = _ => Task.CompletedTask;
	public void InvokeProjectStoppedRunning(SharpIdeProjectModel project) => ProjectStoppedRunning?.InvokeParallelFireAndForget(project);

	public event Func<ExecutionStopInfo, Task> DebuggerExecutionStopped = _ => Task.CompletedTask;
	public void InvokeDebuggerExecutionStopped(ExecutionStopInfo executionStopInfo) => DebuggerExecutionStopped?.InvokeParallelFireAndForget(executionStopInfo);
}

public static class AsyncEventExtensions
{
	public static void InvokeParallelFireAndForget(this MulticastDelegate @event) => FireAndForget(() => @event.InvokeParallelAsync());
	public static void InvokeParallelFireAndForget<T>(this MulticastDelegate @event, T arg) => FireAndForget(() => @event.InvokeParallelAsync(arg));
	public static void InvokeParallelFireAndForget<T, U>(this MulticastDelegate @event, T arg, U arg2) => FireAndForget(() => @event.InvokeParallelAsync(arg, arg2));

	private static async void FireAndForget(Func<Task> action)
	{
		try
		{
			await action().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An exception occurred in an event handler: {ex}");
		}
	}

	public static Task InvokeParallelAsync(this MulticastDelegate @event)
	{
		return InvokeDelegatesAsync(@event.GetInvocationList(), del => ((Func<Task>)del)());
	}

	public static Task InvokeParallelAsync<T>(this MulticastDelegate @event, T arg)
	{
		return InvokeDelegatesAsync(@event.GetInvocationList(), del => ((Func<T, Task>)del)(arg));
	}
	public static Task InvokeParallelAsync<T, U>(this MulticastDelegate @event, T arg, U arg2)
	{
		return InvokeDelegatesAsync(@event.GetInvocationList(), del => ((Func<T, U, Task>)del)(arg, arg2));
	}

	private static async Task InvokeDelegatesAsync(IEnumerable<Delegate> invocationList, Func<Delegate, Task> delegateExecutorDelegate)
	{
		var tasks = invocationList.Select(async del =>
		{
			try
			{
				await delegateExecutorDelegate(del).ConfigureAwait(false);
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		});

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);
		var exceptions = results.Where(r => r is not null).Select(r => r!).ToList();
		if (exceptions.Count != 0)
		{
			throw new AggregateException(exceptions);
		}
	}
}
