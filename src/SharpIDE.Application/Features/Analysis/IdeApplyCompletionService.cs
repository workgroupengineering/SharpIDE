using Microsoft.CodeAnalysis.Completion;
using SharpIDE.Application.Features.FileWatching;
using SharpIDE.Application.Features.SolutionDiscovery;

namespace SharpIDE.Application.Features.Analysis;

public class IdeApplyCompletionService(RoslynAnalysis roslynAnalysis, FileChangedService fileChangedService)
{
	private readonly RoslynAnalysis _roslynAnalysis = roslynAnalysis;
	private readonly FileChangedService _fileChangedService = fileChangedService;

	public async Task ApplyCompletion(SharpIdeFile file, CompletionItem completionItem)
	{
		var (updatedDocumentText, newLinePosition) = await _roslynAnalysis.GetCompletionApplyChanges(file, completionItem);
		await _fileChangedService.SharpIdeFileChanged(file, updatedDocumentText, FileChangeType.CompletionChange, newLinePosition);
	}
}
