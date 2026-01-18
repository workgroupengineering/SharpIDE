using GDExtensionBindgen;
using Godot;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.Debug_.Tab.SubTabs;

namespace SharpIDE.Godot.Features.Debug_.Tab;

public partial class DebugPanelTab : Control
{
    private Terminal _terminal = null!;
    private ThreadsVariablesSubTab _threadsVariablesSubTab = null!;
    private Task _writeTask = Task.CompletedTask;
    
    public SharpIdeProjectModel Project { get; set; } = null!;
    public int TabBarTab { get; set; }

    public override void _EnterTree()
    {
        _threadsVariablesSubTab = GetNode<ThreadsVariablesSubTab>("%ThreadsVariablesSubTab");
        _threadsVariablesSubTab.Project = Project;
    }

    public override void _Ready()
    {
        var terminalControl = GetNode<Control>("%Terminal");
        _terminal = new Terminal(terminalControl);
    }
    
    public void StartWritingFromProjectOutput()
    {
        if (_writeTask.IsCompleted is not true)
        {
            GD.PrintErr("Attempted to start writing from project output, but a write task is already running.");
            return;
        }
        _writeTask = Task.GodotRun(async () =>
        {
            await foreach (var array in Project.RunningOutputChannel!.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await this.InvokeAsync(() => _terminal.Write(array));
            }
        });
    }
    
    public void ClearTerminal()
    {
        _terminal.Clear();
    }
}