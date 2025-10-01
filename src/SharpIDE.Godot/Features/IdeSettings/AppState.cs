namespace SharpIDE.Godot.Features.IdeSettings;

public class AppState
{
    public string? LastOpenSolutionFilePath { get; set; }
    public IdeSettings IdeSettings { get; set; } = new IdeSettings();
    public List<PreviouslyOpenedSln> PreviouslyOpenedSolutions { get; set; } = [];
}

public class IdeSettings
{
    public bool AutoOpenLastSolution { get; set; }
}

public class PreviouslyOpenedSln
{
    public required string Name { get; set; }
    public required string FilePath { get; set; }
}