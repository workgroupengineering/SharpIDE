using SharpIDE.Application.Features.Build;
using SharpIDE.Application.Features.Run;
using SharpIDE.Godot.Features.IdeSettings;

namespace SharpIDE.Godot;

public static class Singletons
{
    public static RunService RunService { get; } = new RunService();
    public static BuildService BuildService { get; } = new BuildService();
    public static AppState AppState { get; set; } = null!;
}