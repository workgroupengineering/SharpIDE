using Godot;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.Problems;

namespace SharpIDE.Godot.Features.SolutionExplorer;

public partial class SolutionExplorerPanel
{
    private void CopySelectedNodeToSlnExplorerClipboard()
    {
        var selected = _tree.GetSelected();
        if (selected is null) return;
        var genericMetadata = selected.GetMetadata(0).As<RefCounted?>();
        if (genericMetadata is RefCountedContainer<SharpIdeFile> fileContainer)
        {
            _itemOnClipboard = (fileContainer.Item, ClipboardOperation.Copy);
        }
    }
    
    private void CutSelectedNodeToSlnExplorerClipboard()
    {
        var selected = _tree.GetSelected();
        if (selected is null) return;
        var genericMetadata = selected.GetMetadata(0).As<RefCounted?>();
        if (genericMetadata is RefCountedContainer<SharpIdeFile> fileContainer)
        {
            _itemOnClipboard = (fileContainer.Item, ClipboardOperation.Cut);
        }
    }
    
    private void ClearSlnExplorerClipboard()
    {
        _itemOnClipboard = null;
    }

    private void CopyNodeFromClipboardToSelectedNode()
    {
        var selected = _tree.GetSelected();
        if (selected is null || _itemOnClipboard is null) return;
        var genericMetadata = selected.GetMetadata(0).As<RefCounted?>();
        IFolderOrProject? folderOrProject = genericMetadata switch
        {
            RefCountedContainer<SharpIdeFolder> f => f.Item,
            RefCountedContainer<SharpIdeProjectModel> p => p.Item,
            _ => null
        };
        if (folderOrProject is null) return;
			
        var (fileToPaste, operation) = _itemOnClipboard.Value;
        _itemOnClipboard = null;
        _ = Task.GodotRun(async () =>
        {
            if (operation is ClipboardOperation.Copy)
            {
                await _ideFileOperationsService.CopyFile(folderOrProject, fileToPaste.Path, fileToPaste.Name);
            }
        });
    }
}