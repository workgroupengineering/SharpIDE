using Godot;

namespace SharpIDE.Godot.Features.SlnPicker;

public partial class SlnPicker : Control
{
    private FileDialog _fileDialog = null!;
    private Button _openSlnButton = null!;

    private readonly TaskCompletionSource<string?> _tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

    public override void _Ready()
    {
        _fileDialog = GetNode<FileDialog>("%FileDialog");
        _openSlnButton = GetNode<Button>("%OpenSlnButton");
        _openSlnButton.Pressed += () => _fileDialog.PopupCentered();
        _fileDialog.FileSelected += path => _tcs.SetResult(path);
        _fileDialog.Canceled += () => _tcs.SetResult(null);
    }
    public async Task<string?> GetSelectedSolutionPath()
    {
        return await _tcs.Task;
    }
}
