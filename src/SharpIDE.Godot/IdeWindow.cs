using Ardalis.GuardClauses;
using Godot;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using SharpIDE.Godot.Features.SlnPicker;

namespace SharpIDE.Godot;

/// <summary>
/// Used to hold either the main IDE scene or the solution picker scene
/// </summary>
public partial class IdeWindow : Control
{
    private readonly PackedScene _solutionPickerScene = ResourceLoader.Load<PackedScene>("res://Features/SlnPicker/SlnPicker.tscn");
    private readonly PackedScene _ideRootScene = ResourceLoader.Load<PackedScene>("res://IdeRoot.tscn");

    private IdeRoot? _ideRoot;
    private SlnPicker? _slnPicker;

    public override void _Ready()
    {
        MSBuildLocator.RegisterDefaults();
        GodotServiceDefaults.AddServiceDefaults();
        //GetWindow().SetMinSize(new Vector2I(1152, 648));
        
        PickSolution();
    }

    public void PickSolution()
    {
        if (_slnPicker is not null) throw new InvalidOperationException("Solution picker is already active");
        _slnPicker = _solutionPickerScene.Instantiate<SlnPicker>();
        AddChild(_slnPicker);
        _ = Task.GodotRun(async () =>
        {
            var slnPathTask = _slnPicker.GetSelectedSolutionPath();
            var ideRoot = _ideRootScene.Instantiate<IdeRoot>();
            ideRoot.IdeWindow = this;
            var slnPath = await slnPathTask;
            if (slnPath is null)
            {
                ideRoot.QueueFree();
                _slnPicker.QueueFree();
                _slnPicker = null;
                return;
            }
            ideRoot.SetSlnFilePath(slnPath);
            
            await this.InvokeAsync(() =>
            {
                RemoveChild(_slnPicker);
                _slnPicker.QueueFree();
                _slnPicker = null;
                if (_ideRoot is not null)
                {
                    RemoveChild(_ideRoot);
                    _ideRoot.QueueFree();
                }
                _ideRoot = ideRoot;
                AddChild(ideRoot);
            });
        });
    }
}