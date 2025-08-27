namespace SharpIDE.Godot;

public static class GodotGlobalEvents
{
    public static event Func<BottomPanelType?, Task> BottomPanelTabSelected = _ => Task.CompletedTask;
    public static void InvokeBottomPanelTabSelected(BottomPanelType? type) => BottomPanelTabSelected.Invoke(type);
}

public enum BottomPanelType
{
    Run,
    Build,
    Problems
}