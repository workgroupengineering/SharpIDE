using System.Collections.Specialized;
using Godot;
using Microsoft.CodeAnalysis;
using ObservableCollections;
using R3;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Problems;

public partial class ProblemsPanel : Control
{
    public SharpIdeSolutionModel? Solution { get; set; }
    
	private Tree _tree = null!;
    private TreeItem _rootItem = null!;
    // TODO: Use observable collections in the solution model and downwards
    private readonly ObservableHashSet<SharpIdeProjectModel> _projects = [];

    public override void _Ready()
    {
        _tree = GetNode<Tree>("ScrollContainer/Tree");
        _rootItem = _tree.CreateItem();
        _rootItem.SetText(0, "Problems");
        Observable.EveryValueChanged(this, manager => manager.Solution)
            .Where(s => s is not null)
            .Subscribe(s =>
            {
                GD.Print($"ProblemsPanel: Solution changed to {s?.Name ?? "null"}");
                _projects.Clear();
                _projects.AddRange(s!.AllProjects);
            }).AddTo(this);
        BindToTree(_projects);
    }

    private class TreeItemContainer
    {
        public TreeItem? Value { get; set; }
    }
    public void BindToTree(ObservableHashSet<SharpIdeProjectModel> list)
    {
        var view = list.CreateView(x =>
        {
            var treeItem = _tree.CreateItem(_rootItem);
            treeItem.SetText(0, x.Name);
            var projectDiagnosticsView = x.Diagnostics.CreateView(y => new TreeItemContainer());
            projectDiagnosticsView.ObserveChanged()
                .SubscribeAwait(async (e, ct) => await (e.Action switch
                {
                    NotifyCollectionChangedAction.Add => CreateDiagnosticTreeItem(_tree, treeItem, e),
                    NotifyCollectionChangedAction.Remove => FreeDiagnosticTreeItem(e),
                    _ => Task.CompletedTask
                })).AddTo(this);
            Observable.EveryValueChanged(x, s => s.Diagnostics.Count)
                .Subscribe(s => treeItem.Visible = s is not 0).AddTo(this);
            return treeItem;
        });
        view.ViewChanged += OnViewChanged;
    }

    private async Task FreeDiagnosticTreeItem(ViewChangedEvent<Diagnostic, TreeItemContainer> e)
    {
        await this.InvokeAsync(() =>
        {
            e.OldItem.View.Value?.Free();
        });
    }

    private async Task CreateDiagnosticTreeItem(Tree tree, TreeItem parent, ViewChangedEvent<Diagnostic, TreeItemContainer> e)
    {
        await this.InvokeAsync(() =>
        {
            var diagItem = tree.CreateItem(parent);
            diagItem.SetText(0, e.NewItem.Value.GetMessage());
            e.NewItem.View.Value = diagItem;
        });
    }

    private static void OnViewChanged(in SynchronizedViewChangedEventArgs<SharpIdeProjectModel, TreeItem> eventArgs)
    {
        GD.Print("View changed: " + eventArgs.Action);
        if (eventArgs.Action == NotifyCollectionChangedAction.Remove)
        {
            var treeItem = eventArgs.OldItem.View;
            Callable.From(() => treeItem.Free()).CallDeferred();
        }
    }
}