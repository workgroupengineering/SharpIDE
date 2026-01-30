using Godot;

namespace SharpIDE.Godot.Features.Settings;

public partial class SettingsWindow : Window
{
    private SpinBox _uiScaleSpinBox = null!;
    private LineEdit _debuggerFilePathLineEdit = null!;
    private CheckButton _debuggerUseSharpDbgCheckButton = null!;
    private OptionButton _themeOptionButton = null!;
    
    private Theme _lightTheme = ResourceLoader.Load<Theme>("uid://epmt8kq6efrs");
    private Color _lightThemeClearColor = new Color("fdfdfd");
    private Theme _darkTheme = ResourceLoader.Load<Theme>("uid://dc7l6bjhn61i5");
    private Color _darkThemeClearColor = new Color("4d4d4d");
    
    public override void _Ready()
    {
        CloseRequested += Hide;
        _uiScaleSpinBox = GetNode<SpinBox>("%UiScaleSpinBox");
        _debuggerFilePathLineEdit = GetNode<LineEdit>("%DebuggerFilePathLineEdit");
        _debuggerUseSharpDbgCheckButton = GetNode<CheckButton>("%DebuggerUseSharpDbgCheckButton");
        _themeOptionButton = GetNode<OptionButton>("%ThemeOptionButton");
        _uiScaleSpinBox.ValueChanged += OnUiScaleSpinBoxValueChanged;
        _debuggerFilePathLineEdit.TextChanged += OnDebuggerFilePathChanged;
        _debuggerUseSharpDbgCheckButton.Toggled += OnDebuggerUseSharpDbgToggled;
        _themeOptionButton.ItemSelected += OnThemeItemSelected;
        AboutToPopup += OnAboutToPopup;
    }

    private void OnAboutToPopup()
    {
        _uiScaleSpinBox.Value = Singletons.AppState.IdeSettings.UiScale;
        _debuggerFilePathLineEdit.Text = Singletons.AppState.IdeSettings.DebuggerExecutablePath;
        _debuggerUseSharpDbgCheckButton.ButtonPressed = Singletons.AppState.IdeSettings.DebuggerUseSharpDbg;
    }

    private void OnUiScaleSpinBoxValueChanged(double value)
    {
        var valueFloat = (float)value;
        Singletons.AppState.IdeSettings.UiScale = valueFloat;
        
        GetTree().GetRoot().ContentScaleFactor = valueFloat;
        PopupCenteredRatio(0.5f); // Re-size the window after scaling
    }

    private void OnDebuggerFilePathChanged(string newText)
    {
        Singletons.AppState.IdeSettings.DebuggerExecutablePath = newText;
    }
    
    private void OnDebuggerUseSharpDbgToggled(bool pressed)
    {
        Singletons.AppState.IdeSettings.DebuggerUseSharpDbg = pressed;
    }
    
    private void OnThemeItemSelected(long index)
    {
        var selectedTheme = _themeOptionButton.GetItemText((int)index);
        var rootWindow = GetTree().GetRoot();
        if (selectedTheme is "Light")
        {
            RenderingServer.Singleton.SetDefaultClearColor(_lightThemeClearColor);
            rootWindow.Theme = _lightTheme;
        }
        else if (selectedTheme is "Dark")
        {
            RenderingServer.Singleton.SetDefaultClearColor(_darkThemeClearColor);
            rootWindow.Theme = _darkTheme;
        }
    }
}