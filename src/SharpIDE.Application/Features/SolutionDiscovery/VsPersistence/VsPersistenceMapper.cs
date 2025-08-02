using System.Diagnostics;
using Ardalis.GuardClauses;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

public static class VsPersistenceMapper
{
	public static async Task<SharpIdeSolutionModel> GetSolutionModel(string solutionFilePath, CancellationToken cancellationToken = default)
	{
		var timer = Stopwatch.StartNew();
		// This intermediate model is pretty much useless, but I have left it around as we grab the project nodes with it, which we might use later.
		var intermediateModel = await GetIntermediateModel(solutionFilePath, cancellationToken);

		var solutionName = Path.GetFileName(solutionFilePath);
		var solutionModel = new SharpIdeSolutionModel
		{
			Name = solutionName,
			FilePath = solutionFilePath,
			Projects = intermediateModel.Projects.Select(GetSharpIdeProjectModel).ToList(),
			Folders = intermediateModel.SolutionFolders.Select(s => new SharpIdeSolutionFolder
			{
				Name = s.Model.Name,
				Folders = s.Folders.Select(GetSharpIdeSolutionFolder).ToList(),
				Projects = s.Projects.Select(GetSharpIdeProjectModel).ToList()
			}).ToList(),
		};
		timer.Stop();
		Console.WriteLine($"Solution model fully created in {timer.ElapsedMilliseconds} ms");

		return solutionModel;
	}
	private static SharpIdeProjectModel GetSharpIdeProjectModel(IntermediateProjectModel projectModel) => new SharpIdeProjectModel
	{
		Name = projectModel.Model.ActualDisplayName,
		FilePath = projectModel.Model.FilePath,
		Files = TreeMapperV2.GetFiles(projectModel.FullFilePath),
		Folders = TreeMapperV2.GetSubFolders(projectModel.FullFilePath)

	};

	private static SharpIdeSolutionFolder GetSharpIdeSolutionFolder(IntermediateSlnFolderModel folderModel) => new SharpIdeSolutionFolder()
		{
			Name = folderModel.Model.Name,
			Folders = folderModel.Folders.Select(GetSharpIdeSolutionFolder).ToList(),
			Projects = folderModel.Projects.Select(GetSharpIdeProjectModel).ToList()
		};

	private static async Task<IntermediateSolutionModel> GetIntermediateModel(string solutionFilePath,
		CancellationToken cancellationToken = default)
	{
		var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath);
		Guard.Against.Null(serializer, nameof(serializer));
		var vsSolution = await serializer.OpenAsync(solutionFilePath, cancellationToken);

		var rootFolders = vsSolution.SolutionFolders
			.Where(f => f.Parent is null)
			.Select(f => BuildFolderTree(f, solutionFilePath, vsSolution.SolutionFolders, vsSolution.SolutionProjects))
			.ToList();

		var solutionModel = new IntermediateSolutionModel
		{
			Name = Path.GetFileName(solutionFilePath),
			FilePath = solutionFilePath,
			Projects = vsSolution.SolutionProjects.Where(p => p.Parent is null).Select(s => new IntermediateProjectModel
			{
				Model = s,
				Id = s.Id,
				FullFilePath = new DirectoryInfo(Path.Join(Path.GetDirectoryName(solutionFilePath), s.FilePath)).FullName
			}).ToList(),
			SolutionFolders = rootFolders
		};
		return solutionModel;
	}

	private static IntermediateSlnFolderModel BuildFolderTree(SolutionFolderModel folder, string solutionFilePath,
		IReadOnlyList<SolutionFolderModel> allSolutionFolders, IReadOnlyList<SolutionProjectModel> allSolutionProjects)
	{
		var childFolders = allSolutionFolders
			.Where(f => f.Parent == folder)
			.Select(f => BuildFolderTree(f, solutionFilePath, allSolutionFolders, allSolutionProjects))
			.ToList();

		var projectsInFolder = allSolutionProjects
			.Where(p => p.Parent == folder)
			.Select(s => new IntermediateProjectModel
			{
				Model = s,
				Id = s.Id,
				FullFilePath = new DirectoryInfo(Path.Join(Path.GetDirectoryName(solutionFilePath), s.FilePath)).FullName
			})
			.ToList();

		return new IntermediateSlnFolderModel
		{
			Model = folder,
			Folders = childFolders,
			Projects = projectsInFolder
		};
	}
}
