using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

/// Does not do any file system operations, only modifies the in-memory solution model
public class SharpIdeSolutionModificationService(FileChangedService fileChangedService)
{
	private readonly FileChangedService _fileChangedService = fileChangedService;

	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;

	/// The directory must already exist on disk
	public async Task<SharpIdeFolder> AddDirectory(IFolderOrProject parentFolder, string directoryName)
	{
		var addedDirectoryPath = Path.Combine(parentFolder.ChildNodeBasePath, directoryName);
		var allFiles = new ConcurrentBag<SharpIdeFile>();
		var allFolders = new ConcurrentBag<SharpIdeFolder>();
		var sharpIdeFolder = new SharpIdeFolder(new DirectoryInfo(addedDirectoryPath), parentFolder, allFiles, allFolders);
		parentFolder.Folders.Add(sharpIdeFolder);
		SolutionModel.AllFolders.AddRange((IEnumerable<SharpIdeFolder>)[sharpIdeFolder, ..allFolders]);
		SolutionModel.AllFiles.AddRange(allFiles);
		foreach (var file in allFiles)
		{
			await _fileChangedService.SharpIdeFileAdded(file, await File.ReadAllTextAsync(file.Path));
		}
		return sharpIdeFolder;
	}

	public async Task RemoveDirectory(SharpIdeFolder folder)
	{
		var parentFolderOrProject = (IFolderOrProject)folder.Parent;
		parentFolderOrProject.Folders.Remove(folder);

		// Also remove all child files and folders from SolutionModel.AllFiles and AllFolders
		var foldersToRemove = new List<SharpIdeFolder>();

		var stack = new Stack<SharpIdeFolder>();
		stack.Push(folder);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			foldersToRemove.Add(current);

			foreach (var subfolder in current.Folders)
			{
				stack.Push(subfolder);
			}
		}

		var filesToRemove = foldersToRemove.SelectMany(f => f.Files).ToList();

		SolutionModel.AllFiles.RemoveRange(filesToRemove);
		SolutionModel.AllFolders.RemoveRange(foldersToRemove);
		foreach (var file in filesToRemove)
		{
			await _fileChangedService.SharpIdeFileRemoved(file);
		}
	}

	public async Task MoveDirectory(IFolderOrProject destinationParentNode, SharpIdeFolder folderToMove)
	{
		var oldFolderPath = folderToMove.Path;
		var newFolderPath = Path.Combine(destinationParentNode.ChildNodeBasePath, folderToMove.Name);

		var parentFolderOrProject = (IFolderOrProject)folderToMove.Parent;
		parentFolderOrProject.Folders.Remove(folderToMove);
		destinationParentNode.Folders.Add(folderToMove);
		folderToMove.Parent = destinationParentNode;
		folderToMove.Path = newFolderPath;

		var stack = new Stack<SharpIdeFolder>();
		stack.Push(folderToMove);

		while (stack.Count > 0)
		{
			var current = stack.Pop();

			foreach (var subfolder in current.Folders)
			{
				subfolder.Path = Path.Combine(current.Path, subfolder.Name);
				stack.Push(subfolder);
			}

			foreach (var file in current.Files)
			{
				var oldPath = file.Path;
				file.Path = Path.Combine(current.Path, file.Name);
				await _fileChangedService.SharpIdeFileMoved(file, oldPath);
			}
		}
	}

	public async Task RenameDirectory(SharpIdeFolder folder, string renamedFolderName)
	{
		var oldFolderPath = folder.Path;

		folder.Name = renamedFolderName;
		folder.Path = Path.Combine(Path.GetDirectoryName(oldFolderPath)!, renamedFolderName);

		var stack = new Stack<SharpIdeFolder>();
		stack.Push(folder);

		while (stack.Count > 0)
		{
			var current = stack.Pop();

			foreach (var subfolder in current.Folders)
			{
				subfolder.Path = Path.Combine(current.Path, subfolder.Name);
				stack.Push(subfolder);
			}

			foreach (var file in current.Files)
			{
				var oldPath = file.Path;
				file.Path = Path.Combine(current.Path, file.Name);
				await _fileChangedService.SharpIdeFileMoved(file, oldPath);
			}
		}
	}

	public async Task<SharpIdeFile> CreateFile(IFolderOrProject parentNode, string newFilePath, string fileName, string contents)
	{
		var sharpIdeFile = new SharpIdeFile(newFilePath, fileName, parentNode, []);

		var correctInsertionPosition = parentNode.Files.list.BinarySearch(sharpIdeFile, SharpIdeFileComparer.Instance);
		if (correctInsertionPosition < 0)
		{
			correctInsertionPosition = ~correctInsertionPosition;
		}
		else
		{
			throw new InvalidOperationException("File already exists in the containing folder or project");
		}

		parentNode.Files.Insert(correctInsertionPosition, sharpIdeFile);
		SolutionModel.AllFiles.Add(sharpIdeFile);
		await _fileChangedService.SharpIdeFileAdded(sharpIdeFile, contents);
		return sharpIdeFile;
	}

	public async Task RemoveFile(SharpIdeFile file)
	{
		var parentFolderOrProject = (IFolderOrProject)file.Parent;
		parentFolderOrProject.Files.Remove(file);
		SolutionModel.AllFiles.Remove(file);
		await _fileChangedService.SharpIdeFileRemoved(file);
	}

	public async Task<SharpIdeFile> MoveFile(IFolderOrProject destinationParentNode, SharpIdeFile fileToMove)
	{
		var oldPath = fileToMove.Path;
		var newFilePath = Path.Combine(destinationParentNode.ChildNodeBasePath, fileToMove.Name);
		var parentFolderOrProject = (IFolderOrProject)fileToMove.Parent;
		parentFolderOrProject.Files.Remove(fileToMove);
		destinationParentNode.Files.Add(fileToMove);
		fileToMove.Parent = destinationParentNode;
		fileToMove.Path = newFilePath;
		await _fileChangedService.SharpIdeFileMoved(fileToMove, oldPath);
		return fileToMove;
	}

	public async Task<SharpIdeFile> RenameFile(SharpIdeFile fileToRename, string renamedFileName)
	{
		var oldPath = fileToRename.Path;
		var newFilePath = Path.Combine(Path.GetDirectoryName(oldPath)!, renamedFileName);
		fileToRename.Name = renamedFileName;
		fileToRename.Path = newFilePath;
		await _fileChangedService.SharpIdeFileRenamed(fileToRename, oldPath);
		return fileToRename;
	}
}
