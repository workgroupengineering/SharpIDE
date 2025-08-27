using Godot;
using SharpIDE.Application.Features.SolutionDiscovery;

namespace SharpIDE.Godot.Features.SolutionExplorer;

public partial class SharpIdeFileGodotContainer : GodotObject
{
    public required SharpIdeFile File { get; init; }
}

// public partial class GodotContainer<T>(T value) : GodotObject where T : class
// {
//     public T Value { get; init; } = value;
// }