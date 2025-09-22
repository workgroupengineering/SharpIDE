using Godot;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.CodeEditor;

public partial class CodeEditorPanel : MarginContainer
{
    [Export]
    public Texture2D CsFileTexture { get; set; } = null!;
    public SharpIdeSolutionModel Solution { get; set; } = null!;
    private PackedScene _sharpIdeCodeEditScene = GD.Load<PackedScene>("res://Features/CodeEditor/SharpIdeCodeEdit.tscn");
    private TabContainer _tabContainer = null!;
    
    public override void _Ready()
    {
        _tabContainer = GetNode<TabContainer>("TabContainer");
        _tabContainer.RemoveChild(_tabContainer.GetChild(0)); // Remove the default tab
        _tabContainer.TabClicked += OnTabClicked;
        var tabBar = _tabContainer.GetTabBar();
        tabBar.TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways;
        tabBar.TabClosePressed += OnTabClosePressed;
    }

    private void OnTabClicked(long tab)
    {
        var sharpIdeFile = _tabContainer.GetChild<SharpIdeCodeEdit>((int)tab).SharpIdeFile;
        GodotGlobalEvents.InvokeFileExternallySelected(sharpIdeFile);
    }

    private void OnTabClosePressed(long tabIndex)
    {
        var tab = _tabContainer.GetChild<Control>((int)tabIndex);
        _tabContainer.RemoveChild(tab);
        tab.QueueFree();
    }

    public async Task SetSharpIdeFile(SharpIdeFile file)
    {
        var existingTab = _tabContainer.GetChildren().OfType<SharpIdeCodeEdit>().FirstOrDefault(t => t.SharpIdeFile == file);
        if (existingTab is not null)
        {
            var existingTabIndex = existingTab.GetIndex();
            if (existingTabIndex == _tabContainer.CurrentTab) return;
            await this.InvokeAsync(() => _tabContainer.CurrentTab = existingTabIndex);
            return;
        }
        var newTab = _sharpIdeCodeEditScene.Instantiate<SharpIdeCodeEdit>();
        newTab.Solution = Solution;
        await this.InvokeAsync(() =>
        {
            _tabContainer.AddChild(newTab);
            var newTabIndex = _tabContainer.GetTabCount() - 1;
            _tabContainer.SetTabIcon(newTabIndex, CsFileTexture);
            _tabContainer.SetTabTitle(newTabIndex, file.Name);
            _tabContainer.SetTabTooltip(newTabIndex, file.Path);
            _tabContainer.CurrentTab = newTabIndex;
        });
        await newTab.SetSharpIdeFile(file);
    }
}