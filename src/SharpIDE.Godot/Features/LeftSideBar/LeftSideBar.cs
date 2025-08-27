using Godot;

namespace SharpIDE.Godot.Features.LeftSideBar;

public partial class LeftSideBar : Panel
{
    private Button _slnExplorerButton = null!;
    // These are in a ButtonGroup, which handles mutual exclusivity of being toggled on
    private Button _problemsButton = null!;
    private Button _runButton = null!;
    private Button _buildButton = null!;
    
    public override void _Ready()
    {
        _slnExplorerButton = GetNode<Button>("%SlnExplorerButton");
        _problemsButton = GetNode<Button>("%ProblemsButton");
        _runButton = GetNode<Button>("%RunButton");
        _buildButton = GetNode<Button>("%BuildButton");
        
        _problemsButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Problems : null);
        _runButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Run : null);
        _buildButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Build : null);
    }
}