using Microsoft.Build.Construction;

namespace SharpIDE.Application.Features.SolutionDiscovery;

public static class GetNodesInSolution
{
	public static SolutionFile? ParseSolutionFileFromPath(string solutionFilePath)
	{
		var solutionFile = SolutionFile.Parse(solutionFilePath);
		return solutionFile;
	}
}
