namespace SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

public class SharpIdeSolutionModel
{
	public required string Name { get; set; }
	public required string FilePath { get; set; }
	public required List<SharpIdeProjectModel> Projects { get; set; }
	public required List<SharpIdeSolutionFolder> Folders { get; set; }
}
public class SharpIdeSolutionFolder
{
	public required string Name { get; set; }
	public required List<SharpIdeSolutionFolder> Folders { get; set; }
	public required List<SharpIdeProjectModel> Projects { get; set; }
}
public class SharpIdeProjectModel
{
	public required string Name { get; set; }
	public required string FilePath { get; set; }
	public required List<SharpIdeFolder> Folders { get; set; }
	public required List<SharpIdeFile> Files { get; set; }
}
