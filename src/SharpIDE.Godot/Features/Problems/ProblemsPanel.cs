using System.Collections.Specialized;
using Godot;
using Microsoft.CodeAnalysis;
using ObservableCollections;
using R3;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.Common;

namespace SharpIDE.Godot.Features.Problems;

public partial class ProblemsPanel : Control
{
    [Export]
    public Texture2D WarningIcon { get; set; } = null!;
    [Export]
    public Texture2D ErrorIcon { get; set; } = null!;
    [Export]
    public Texture2D CsprojIcon { get; set; } = null!;
    
    public SharpIdeSolutionModel? Solution { get; set; }
    
	private Tree _tree = null!;
    private TreeItem _rootItem = null!;
    // TODO: Use observable collections in the solution model and downwards
    private readonly ObservableHashSet<SharpIdeProjectModel> _projects = [];

    public override void _Ready()
    {
        _tree = GetNode<Tree>("%Tree");
        _tree.ItemActivated += TreeOnItemActivated;
        _rootItem = _tree.CreateItem();
        _rootItem.SetText(0, "Problems");
        Observable.EveryValueChanged(this, manager => manager.Solution)
            .Where(s => s is not null)
            .Subscribe(s =>
            {
                GD.Print($"ProblemsPanel: Solution changed to {s?.Name ?? "null"}");
                _projects.RemoveRange(_projects);
                _projects.AddRange(s!.AllProjects);
            }).AddTo(this);
        BindToTree(_projects);
    }


    public void BindToTree(ObservableHashSet<SharpIdeProjectModel> list)
    {
        var view = list.CreateView(y => new TreeItemContainer());
        view.ObserveChanged()
            .SubscribeAwait(async (e, ct) => await (e.Action switch
            {
                NotifyCollectionChangedAction.Add => CreateProjectTreeItem(_tree, _rootItem, e),
                NotifyCollectionChangedAction.Remove => FreeTreeItem(e.OldItem.View.Value),
                _ => Task.CompletedTask
            })).AddTo(this);
    }

    private async Task CreateProjectTreeItem(Tree tree, TreeItem parent, ViewChangedEvent<SharpIdeProjectModel, TreeItemContainer> e)
    {
        await this.InvokeAsync(() =>
        {
            var treeItem = tree.CreateItem(parent);
            treeItem.SetText(0, e.NewItem.Value.Name);
            treeItem.SetIcon(0, CsprojIcon);
            treeItem.SetMetadata(0, new RefCountedContainer<SharpIdeProjectModel>(e.NewItem.Value));
            e.NewItem.View.Value = treeItem;
            
            Observable.EveryValueChanged(e.NewItem.Value, s => s.Diagnostics.Count).Subscribe(s => treeItem.Visible = s is not 0).AddTo(this);
            
            var projectDiagnosticsView = e.NewItem.Value.Diagnostics.CreateView(y => new TreeItemContainer());
            projectDiagnosticsView.ObserveChanged()
                .SubscribeAwait(async (innerEvent, ct) => await (innerEvent.Action switch
                {
                    NotifyCollectionChangedAction.Add => CreateDiagnosticTreeItem(_tree, treeItem, innerEvent),
                    NotifyCollectionChangedAction.Remove => FreeTreeItem(innerEvent.OldItem.View.Value),
                    _ => Task.CompletedTask
                })).AddTo(this);
        });
    }

    private async Task CreateDiagnosticTreeItem(Tree tree, TreeItem parent, ViewChangedEvent<SharpIdeDiagnostic, TreeItemContainer> e)
    {
        await this.InvokeAsync(() =>
        {
            var diagItem = tree.CreateItem(parent);
            diagItem.SetText(0, e.NewItem.Value.Diagnostic.GetMessage());
            diagItem.SetMetadata(0, new RefCountedContainer<SharpIdeDiagnostic>(e.NewItem.Value));
            diagItem.SetIcon(0, e.NewItem.Value.Diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => ErrorIcon,
                DiagnosticSeverity.Warning => WarningIcon,
                _ => null
            });
            e.NewItem.View.Value = diagItem;
        });
    }
    
    private async Task FreeTreeItem(TreeItem? treeItem)
    {
        await this.InvokeAsync(() =>
        {
            treeItem?.Free();
        });
    }
    
    private void TreeOnItemActivated()
    {
        var selected = _tree.GetSelected();
        var diagnosticContainer = selected.GetMetadata(0).As<RefCountedContainer<SharpIdeDiagnostic>?>();
        if (diagnosticContainer is null) return;
        var diagnostic = diagnosticContainer.Item;
        var parentTreeItem = selected.GetParent();
        var projectContainer = parentTreeItem.GetMetadata(0).As<RefCountedContainer<SharpIdeProjectModel>?>();
        if (projectContainer is null) return;
        OpenDocumentContainingDiagnostic(diagnostic.Diagnostic);
    }
    
    private void OpenDocumentContainingDiagnostic(Diagnostic diagnostic)
    {
        var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
        var file = Solution!.AllFiles.Single(f => f.Path == fileLinePositionSpan.Path);
        var linePosition = new SharpIdeFileLinePosition(fileLinePositionSpan.StartLinePosition.Line, fileLinePositionSpan.StartLinePosition.Character);
        GodotGlobalEvents.Instance.FileExternallySelected.InvokeParallelFireAndForget(file, linePosition);
    }
}