using Godot;
using Microsoft.Build.Locator;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.BottomPanel;
using SharpIDE.Godot.Features.CustomControls;
using SharpIDE.Godot.Features.Run;
using SharpIDE.Godot.Features.SolutionExplorer;

namespace SharpIDE.Godot;

public partial class IdeRoot : Control
{
	private Button _openSlnButton = null!;
	private Button _buildSlnButton = null!;
	private FileDialog _fileDialog = null!;
	private SharpIdeCodeEdit _sharpIdeCodeEdit = null!;
	private SolutionExplorerPanel _solutionExplorerPanel = null!;
	private InvertedVSplitContainer _invertedVSplitContainer = null!;
	private RunPanel _runPanel = null!;
	private Button _runMenuButton = null!;
	private Popup _runMenuPopup = null!;
	private BottomPanelManager _bottomPanelManager = null!;
	
	private readonly PackedScene _runMenuItemScene = ResourceLoader.Load<PackedScene>("res://Features/Run/RunMenuItem.tscn");
	public override void _Ready()
	{
		MSBuildLocator.RegisterDefaults();
		
		_openSlnButton = GetNode<Button>("%OpenSlnButton");
		_buildSlnButton = GetNode<Button>("%BuildSlnButton");
		_runMenuPopup = GetNode<Popup>("%RunMenuPopup");
		_runMenuButton = GetNode<Button>("%RunMenuButton");
		_sharpIdeCodeEdit = GetNode<SharpIdeCodeEdit>("%CodeEditorPanel/SharpIdeCodeEdit");
		_fileDialog = GetNode<FileDialog>("%OpenSolutionDialog");
		_solutionExplorerPanel = GetNode<SolutionExplorerPanel>("%SolutionExplorerPanel");
		_runPanel = GetNode<RunPanel>("%RunPanel");
		_invertedVSplitContainer = GetNode<InvertedVSplitContainer>("%InvertedVSplitContainer");
		_bottomPanelManager = GetNode<BottomPanelManager>("%BottomPanel");
		
		_runMenuButton.Pressed += OnRunMenuButtonPressed;
		GodotGlobalEvents.FileSelected += OnSolutionExplorerPanelOnFileSelected;
		_fileDialog.FileSelected += OnSlnFileSelected;
		_openSlnButton.Pressed += () => _fileDialog.Visible = true;
		_buildSlnButton.Pressed += OnBuildSlnButtonPressed;
		GodotGlobalEvents.BottomPanelVisibilityChangeRequested += async show => await this.InvokeAsync(() => _invertedVSplitContainer.InvertedSetCollapsed(!show));
		OnSlnFileSelected(@"C:\Users\Matthew\Documents\Git\BlazorCodeBreaker\BlazorCodeBreaker.slnx");
	}

	private void OnRunMenuButtonPressed()
	{
		var popupMenuPosition = _runMenuButton.GlobalPosition;
		const int buttonHeight = 37;
		_runMenuPopup.Position = new Vector2I((int)popupMenuPosition.X, (int)popupMenuPosition.Y + buttonHeight);
		_runMenuPopup.Popup();
	}

	private async void OnBuildSlnButtonPressed()
	{
		GodotGlobalEvents.InvokeBottomPanelTabExternallySelected(BottomPanelType.Build);
		await Singletons.BuildService.MsBuildSolutionAsync(_solutionExplorerPanel.SolutionModel.FilePath);
	}

	private async Task OnSolutionExplorerPanelOnFileSelected(SharpIdeFile file)
	{
		await _sharpIdeCodeEdit.SetSharpIdeFile(file);
	}

	private void OnSlnFileSelected(string path)
	{
		_ = Task.GodotRun(async () =>
		{
			GD.Print($"Selected: {path}");
			var solutionModel = await VsPersistenceMapper.GetSolutionModel(path);
			_solutionExplorerPanel.SolutionModel = solutionModel;
			_sharpIdeCodeEdit.Solution = solutionModel;
			_bottomPanelManager.Solution = solutionModel;
			Callable.From(_solutionExplorerPanel.RepopulateTree).CallDeferred();
			RoslynAnalysis.StartSolutionAnalysis(solutionModel);
				
			var tasks = solutionModel.AllProjects.Select(p => p.MsBuildEvaluationProjectTask).ToList();
			await Task.WhenAll(tasks).ConfigureAwait(false);
			var runnableProjects = solutionModel.AllProjects.Where(p => p.IsRunnable).ToList();
			await this.InvokeAsync(() =>
			{
				var runMenuPopupVbox = _runMenuPopup.GetNode<VBoxContainer>("MarginContainer/VBoxContainer");
				foreach (var project in runnableProjects)
				{
					var runMenuItem = _runMenuItemScene.Instantiate<RunMenuItem>();
					runMenuItem.Project = project;
					runMenuPopupVbox.AddChild(runMenuItem);
				}
				_runMenuButton.Disabled = false;
			});
				
			var infraProject = solutionModel.AllProjects.Single(s => s.Name == "WebUi");
			var diFile = infraProject.Folders.Single(s => s.Name == "Pages").Files.Single(s => s.Name == "TestPage.razor");
			await this.InvokeDeferredAsync(() => GodotGlobalEvents.InvokeFileExternallySelected(diFile));
				
			//var runnableProject = solutionModel.AllProjects.First(s => s.IsRunnable);
			//await this.InvokeAsync(() => _runPanel.NewRunStarted(runnableProject));
		});
	}
}