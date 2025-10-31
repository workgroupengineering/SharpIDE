using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public class IdeFileExternalChangeHandler
{
	private readonly ILogger<IdeFileExternalChangeHandler> _logger;
	private readonly FileChangedService _fileChangedService;
	private readonly SharpIdeSolutionModificationService _sharpIdeSolutionModificationService;
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;
	public IdeFileExternalChangeHandler(FileChangedService fileChangedService, SharpIdeSolutionModificationService sharpIdeSolutionModificationService, ILogger<IdeFileExternalChangeHandler> logger)
	{
		_logger = logger;
		_fileChangedService = fileChangedService;
		_sharpIdeSolutionModificationService = sharpIdeSolutionModificationService;
		GlobalEvents.Instance.FileSystemWatcherInternal.FileChanged.Subscribe(OnFileChanged);
		GlobalEvents.Instance.FileSystemWatcherInternal.FileCreated.Subscribe(OnFileCreated);
		GlobalEvents.Instance.FileSystemWatcherInternal.FileDeleted.Subscribe(OnFileDeleted);
		GlobalEvents.Instance.FileSystemWatcherInternal.FileRenamed.Subscribe(OnFileRenamed);
		GlobalEvents.Instance.FileSystemWatcherInternal.DirectoryCreated.Subscribe(OnFolderCreated);
		GlobalEvents.Instance.FileSystemWatcherInternal.DirectoryDeleted.Subscribe(OnFolderDeleted);
		GlobalEvents.Instance.FileSystemWatcherInternal.DirectoryRenamed.Subscribe(OnFolderRenamed);
	}

	private async Task OnFileRenamed(string oldFilePath, string newFilePath)
	{
		var sharpIdeFile = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == oldFilePath);
		if (sharpIdeFile is null) return;
		await _sharpIdeSolutionModificationService.RenameFile(sharpIdeFile, Path.GetFileName(newFilePath));
	}

	private async Task OnFileDeleted(string filePath)
	{
		var sharpIdeFile = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (sharpIdeFile is null) return;
		await _sharpIdeSolutionModificationService.RemoveFile(sharpIdeFile);
	}

	// TODO: Test - this most likely only will ever be called on linux - windows and macos(?) does delete + create on rename of folders
	private async Task OnFolderRenamed(string oldFolderPath, string newFolderPath)
	{
		var sharpIdeFolder = SolutionModel.AllFolders.SingleOrDefault(f => f.Path == oldFolderPath);
		if (sharpIdeFolder is null)
		{
			return;
		}
		var isMoveRatherThanRename = Path.GetDirectoryName(oldFolderPath) != Path.GetDirectoryName(newFolderPath);
		if (isMoveRatherThanRename)
		{
			await _sharpIdeSolutionModificationService.MoveDirectory(sharpIdeFolder, sharpIdeFolder);
		}
		else
		{
			var newFolderName = Path.GetFileName(newFolderPath);
			await _sharpIdeSolutionModificationService.RenameDirectory(sharpIdeFolder, newFolderName);
		}
	}

	private async Task OnFolderDeleted(string folderPath)
	{
		var sharpIdeFolder = SolutionModel.AllFolders.SingleOrDefault(f => f.Path == folderPath);
		if (sharpIdeFolder is null)
		{
			return;
		}
		await _sharpIdeSolutionModificationService.RemoveDirectory(sharpIdeFolder);
	}

	private async Task OnFolderCreated(string folderPath)
	{
		var sharpIdeFolder = SolutionModel.AllFolders.SingleOrDefault(f => f.Path == folderPath);
		if (sharpIdeFolder is not null)
		{
			return;
		}
		var containingFolderPath = Path.GetDirectoryName(folderPath)!;
		var containingFolderOrProject = (IFolderOrProject?)SolutionModel.AllFolders.SingleOrDefault(f => f.ChildNodeBasePath == containingFolderPath) ?? SolutionModel.AllProjects.SingleOrDefault(s => s.ChildNodeBasePath == containingFolderPath);
		if (containingFolderOrProject is null)
		{
			_logger.LogError("Containing Folder or Project of folder '{FolderPath}' does not exist", folderPath);
			return;
		}
		var folderName = Path.GetFileName(folderPath);
		await _sharpIdeSolutionModificationService.AddDirectory(containingFolderOrProject, folderName);
	}

	private async Task OnFileCreated(string filePath)
	{
		var sharpIdeFile = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (sharpIdeFile is not null)
		{
			// It was likely already created via a parent folder creation
			return;
		}
		var createdFileDirectory = Path.GetDirectoryName(filePath)!;

		var containingFolderOrProject = (IFolderOrProject?)SolutionModel.AllFolders.SingleOrDefault(f => f.ChildNodeBasePath == createdFileDirectory) ?? SolutionModel.AllProjects.SingleOrDefault(s => s.ChildNodeBasePath == createdFileDirectory);
		if (containingFolderOrProject is null)
		{
			_logger.LogError("Containing Folder or Project of file '{FilePath}' does not exist", filePath);
			return;
		}

		await _sharpIdeSolutionModificationService.CreateFile(containingFolderOrProject, filePath, Path.GetFileName(filePath), await File.ReadAllTextAsync(filePath));
	}

	private async Task OnFileChanged(string filePath)
	{
		var sharpIdeFile = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (sharpIdeFile is null) return;
		if (sharpIdeFile.SuppressDiskChangeEvents is true) return;
		if (sharpIdeFile.LastIdeWriteTime is not null)
		{
			var now = DateTimeOffset.Now;
			if (now - sharpIdeFile.LastIdeWriteTime.Value < TimeSpan.FromMilliseconds(300))
			{
				_logger.LogTrace("File change ignored - recently modified by the IDE: '{FilePath}'", filePath);
				return;
			}
		}
		_logger.LogInformation("IdeFileExternalChangeHandler: Changed - '{FilePath}'", filePath);
		var file = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (file is not null)
		{
			await _fileChangedService.SharpIdeFileChanged(file, await File.ReadAllTextAsync(file.Path), FileChangeType.ExternalChange);
		}
	}
}
