namespace SharpIDE.Application.Features.SolutionDiscovery;

public class SharpIdeFileComparer : IComparer<SharpIdeFile>
{
	public static readonly SharpIdeFileComparer Instance = new SharpIdeFileComparer();
	public int Compare(SharpIdeFile? x, SharpIdeFile? y)
	{
		if (ReferenceEquals(x, y)) return 0;
		if (x is null) return -1;
		if (y is null) return 1;

		int result = string.Compare(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);

		return result;
	}
}

public class SharpIdeFolderComparer : IComparer<SharpIdeFolder>
{
	public static readonly SharpIdeFolderComparer Instance = new SharpIdeFolderComparer();
	public int Compare(SharpIdeFolder? x, SharpIdeFolder? y)
	{
		if (ReferenceEquals(x, y)) return 0;
		if (x is null) return -1;
		if (y is null) return 1;

		int result = string.Compare(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);

		return result;
	}
}
