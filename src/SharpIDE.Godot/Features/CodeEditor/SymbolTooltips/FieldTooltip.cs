using Godot;
using Microsoft.CodeAnalysis;

namespace SharpIDE.Godot.Features.CodeEditor;

public static partial class SymbolInfoComponents
{
    public static Control GetFieldSymbolInfo(IFieldSymbol symbol)
    {
        var label = new RichTextLabel();
        label.FitContent = true;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.PushColor(CachedColors.White);
        label.PushFont(MonospaceFont);
        label.AddAttributes(symbol);
        label.AddAccessibilityModifier(symbol);
        label.AddText(" ");
        label.AddStaticModifier(symbol);
        label.AddReadonlyModifier(symbol);
        label.AddVirtualModifier(symbol);
        label.AddAbstractModifier(symbol);
        label.AddOverrideModifier(symbol);
        label.AddFieldTypeName(symbol);
        label.AddFieldName(symbol);
        label.AddText(";");
        label.AddContainingNamespaceAndClass(symbol);
        
        label.Pop();
        label.Pop();
        return label;
    }
    
    private static void AddStaticModifier(this RichTextLabel label, IFieldSymbol symbol)
    {
        if (symbol.IsStatic)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText("static");
            label.Pop();
            label.AddText(" ");
        }
    }
    
    private static void AddReadonlyModifier(this RichTextLabel label, IFieldSymbol fieldSymbol)
    {
        if (fieldSymbol.IsReadOnly)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText("readonly");
            label.Pop();
            label.AddText(" ");
        }
    }

    private static void AddFieldTypeName(this RichTextLabel label, IFieldSymbol fieldSymbol)
    {
        label.PushColor(GetSymbolColourByType(fieldSymbol.Type));
        label.AddText(fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        label.Pop();
        label.AddText(" ");
    }
    
    private static void AddFieldName(this RichTextLabel label, IFieldSymbol fieldSymbol)
    {
        label.PushColor(CachedColors.White);
        label.AddText(fieldSymbol.Name);
        label.Pop();
    }
}