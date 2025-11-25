using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Build;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

[assembly: CaptureConsole]

namespace SharpIDE.Application.UnitTests.Features.Analysis;
public class RoslynAnalysisTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public RoslynAnalysisTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
		SharpIdeMsbuildLocator.Register();
	}


	[Fact]
    public async Task GetProjectDiagnostics_NoSolutionChanges_IsSubsequentlyCheaper()
    {
	    // Arrange
	    var serviceCollection = new ServiceCollection();
	    serviceCollection.AddApplication();

	    var services = serviceCollection.BuildServiceProvider();
	    var logger = services.GetRequiredService<ILogger<RoslynAnalysis>>();
	    var buildService = services.GetRequiredService<BuildService>();

	    var roslynAnalysis = new RoslynAnalysis(logger, buildService);

	    var solutionModel = await VsPersistenceMapper.GetSolutionModel(@"C:\Users\Matthew\Documents\Git\SharpIDE\SharpIDE.sln", TestContext.Current.CancellationToken);
	    var sharpIdeApplicationProject = solutionModel.AllProjects.Single(p => p.Name == "SharpIDE.Application");

		roslynAnalysis._solutionLoadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	    await roslynAnalysis.Analyse(solutionModel, TestContext.Current.CancellationToken);

	    // Act
	    foreach (var i in Enumerable.Range(1, 3))
	    {
		    var timer = Stopwatch.StartNew();
		    await roslynAnalysis.GetProjectDiagnostics(sharpIdeApplicationProject, TestContext.Current.CancellationToken);
		    timer.Stop();
		    _testOutputHelper.WriteLine($"Diagnostics: {timer.ElapsedMilliseconds.ToString()}ms");
	    }
    }
}
