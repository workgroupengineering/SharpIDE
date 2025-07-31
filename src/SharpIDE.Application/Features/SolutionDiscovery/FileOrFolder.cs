namespace SharpIDE.Application.Features.SolutionDiscovery;

public class SharpIdeFile
{
	public required string Path { get; set; }
	public required string Name { get; set; }
}

public class SharpIdeFolder
{
	public required string Path { get; set; }
	public required string Name { get; set; }
	public required List<SharpIdeFile> Files { get; set; }
	public required List<SharpIdeFolder> Folders { get; set; }
	// public required int Depth { get; set; }

	public bool Expanded { get; set; }
}
