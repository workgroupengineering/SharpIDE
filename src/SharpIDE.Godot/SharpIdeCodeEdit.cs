using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery;
using Task = System.Threading.Tasks.Task;

namespace SharpIDE.Godot;

public partial class SharpIdeCodeEdit : CodeEdit
{
	[Signal]
	public delegate void CodeFixesRequestedEventHandler();

	private int _currentLine;
	private int _selectionStartCol;
	private int _selectionEndCol;
	
	private SharpIdeFile _currentFile = null!;
	
	private CustomHighlighter _syntaxHighlighter = new();
	private PopupMenu _popupMenu = null!;

	private ImmutableArray<(FileLinePositionSpan fileSpan, Diagnostic diagnostic)> _diagnostics = [];
	private ImmutableArray<CodeAction> _currentCodeActionsInPopup = [];
	
	public override void _Ready()
	{
		_popupMenu = GetNode<PopupMenu>("CodeFixesMenu");
		_popupMenu.IdPressed += OnCodeFixSelected;
		this.CodeCompletionRequested += OnCodeCompletionRequested;
		this.CodeFixesRequested += OnCodeFixesRequested;
		this.CaretChanged += () =>
		{
			_selectionStartCol = GetSelectionFromColumn();
			_selectionEndCol = GetSelectionToColumn();
			_currentLine = GetCaretLine();
			GD.Print($"Selection changed to line {_currentLine}, start {_selectionStartCol}, end {_selectionEndCol}");
		};
		TextChanged += OnTextChanged;
		this.SyntaxHighlighter = _syntaxHighlighter;
	}

	private void OnTextChanged()
	{
		// update the MSBuildWorkspace
		RoslynAnalysis.UpdateDocument(_currentFile, Text);
		_ = Task.Run(async () =>
		{
			try
			{
				var syntaxHighlighting = await RoslynAnalysis.GetDocumentSyntaxHighlighting(_currentFile);
				var diagnostics = await RoslynAnalysis.GetDocumentDiagnostics(_currentFile);
				Callable.From(() =>
				{
					SetSyntaxHighlightingModel(syntaxHighlighting);
					SetDiagnosticsModel(diagnostics);
				}).CallDeferred();
			}
			catch (Exception ex)
			{
				GD.PrintErr($"Error Calling OnTextChanged: {ex.Message}");
			}
		});
	}

	private void OnCodeFixSelected(long id)
	{
		GD.Print($"Code fix selected: {id}");
		var codeAction = _currentCodeActionsInPopup[(int)id];
		if (codeAction is null) return;
		var currentCaretPosition = GetCaretPosition();
		var vScroll = GetVScroll();
		_ = Task.Run(async () =>
		{
			try
			{
				await RoslynAnalysis.ApplyCodeActionAsync(codeAction);
				var fileContents = await File.ReadAllTextAsync(_currentFile.Path);
				var syntaxHighlighting = await RoslynAnalysis.GetDocumentSyntaxHighlighting(_currentFile);
				var diagnostics = await RoslynAnalysis.GetDocumentDiagnostics(_currentFile);
				Callable.From(() =>
				{
					SetText(fileContents);
					SetSyntaxHighlightingModel(syntaxHighlighting);
					SetDiagnosticsModel(diagnostics);
					SetCaretLine(currentCaretPosition.line);
					SetCaretColumn(currentCaretPosition.col);
					SetVScroll(vScroll);
				}).CallDeferred();
			}
			catch (Exception ex)
			{
				GD.PrintErr($"Error applying code fix: {ex.Message}");
			}
		});
	}

	public async Task SetSharpIdeFile(SharpIdeFile file)
	{
		_currentFile = file;
		var fileContents = await File.ReadAllTextAsync(_currentFile.Path);
		SetText(fileContents);
		var syntaxHighlighting = await RoslynAnalysis.GetDocumentSyntaxHighlighting(_currentFile);
		SetSyntaxHighlightingModel(syntaxHighlighting);
		var diagnostics = await RoslynAnalysis.GetDocumentDiagnostics(_currentFile);
		SetDiagnosticsModel(diagnostics);
	}
	
	public void UnderlineRange(int line, int caretStartCol, int caretEndCol, Color color, float thickness = 1.5f)
	{
		if (line < 0 || line >= GetLineCount())
			return;

		if (caretStartCol > caretEndCol) // something went wrong
			return;

		// Clamp columns to line length
		int lineLength = GetLine(line).Length;
		caretStartCol = Mathf.Clamp(caretStartCol, 0, lineLength);
		caretEndCol   = Mathf.Clamp(caretEndCol, 0, lineLength);
		
		// GetRectAtLineColumn returns the rectangle for the character before the column passed in, or the first character if the column is 0.
		var startRect = GetRectAtLineColumn(line, caretStartCol);
		var endRect = GetRectAtLineColumn(line, caretEndCol);
		//DrawLine(startRect.Position, startRect.End, color);
		//DrawLine(endRect.Position, endRect.End, color);
		
		var startPos = startRect.End;
		if (caretStartCol is 0)
		{
			startPos.X -= startRect.Size.X;
		}
		var endPos = endRect.End;
		startPos.Y -= 3;
		endPos.Y   -= 3;
		if (caretStartCol == caretEndCol)
		{
			endPos.X += 10;
		}
		DrawDashedLine(startPos, endPos, color, thickness);
		//DrawLine(startPos, endPos, color, thickness);
	}
	public override void _Draw()
	{
		//UnderlineRange(_currentLine, _selectionStartCol, _selectionEndCol, new Color(1, 0, 0));
		foreach (var (fileSpan, diagnostic) in _diagnostics)
		{
			if (diagnostic.Location.IsInSource)
			{
				var line = fileSpan.StartLinePosition.Line;
				var startCol = fileSpan.StartLinePosition.Character;
				var endCol = fileSpan.EndLinePosition.Character;
				var color = diagnostic.Severity switch
				{
					DiagnosticSeverity.Error => new Color(1, 0, 0),
					DiagnosticSeverity.Warning => new Color("ffb700"),
					_ => new Color(0, 1, 0) // Info or other
				};
				UnderlineRange(line, startCol, endCol, color);
			}
		}
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event.IsActionPressed(InputStringNames.CodeFixes))
		{
			EmitSignalCodeFixesRequested();
		}
	}

	private void SetDiagnosticsModel(ImmutableArray<(FileLinePositionSpan fileSpan, Diagnostic diagnostic)> diagnostics)
	{
		_diagnostics = diagnostics;
	}

	private void SetSyntaxHighlightingModel(IEnumerable<(FileLinePositionSpan fileSpan, ClassifiedSpan classifiedSpan)> classifiedSpans)
	{
		_syntaxHighlighter.ClassifiedSpans = classifiedSpans;
		Callable.From(() =>
		{
			_syntaxHighlighter.ClearHighlightingCache();
			//_syntaxHighlighter.UpdateCache();
			SyntaxHighlighter = null;
			SyntaxHighlighter = _syntaxHighlighter; // Reassign to trigger redraw
			GD.Print("Provided syntax highlighting");
		}).CallDeferred();
	}

	private void OnCodeFixesRequested()
	{
		var (caretLine, caretColumn) = GetCaretPosition();
		var popupMenuPosition = GetCaretDrawPos() with { X = 0 } + GetGlobalPosition();
		_popupMenu.Position = new Vector2I((int)popupMenuPosition.X, (int)popupMenuPosition.Y);
		_popupMenu.Clear();
		_popupMenu.AddItem("Getting Context Actions...", 0);
		_popupMenu.Popup();
		GD.Print($"Code fixes requested at line {caretLine}, column {caretColumn}");
		_ = Task.Run(async () =>
		{
			try
			{
				var linePos = new LinePosition(caretLine, caretColumn);
				var codeActions = await RoslynAnalysis.GetCodeFixesForDocumentAtPosition(_currentFile, linePos);
				Callable.From(() =>
				{
					_popupMenu.Clear();
					foreach (var (index, codeAction) in codeActions.Index())
					{
						_currentCodeActionsInPopup = codeActions;
						_popupMenu.AddItem(codeAction.Title, index);
						//_popupMenu.SetItemMetadata(menuItem, codeAction);
					}
					GD.Print($"Code fixes found: {codeActions.Length}, displaying menu");
				}).CallDeferred();
			}
			catch (Exception ex)
			{
				GD.Print(ex);
			}
		});
	}

	private void OnCodeCompletionRequested()
	{
		var (caretLine, caretColumn) = GetCaretPosition();
		
		GD.Print($"Code completion requested at line {caretLine}, column {caretColumn}");
		_ = Task.Run(async () =>
		{
			try
			{
				var linePos = new LinePosition(caretLine, caretColumn);
				
				var completions = await RoslynAnalysis.GetCodeCompletionsForDocumentAtPosition(_currentFile, linePos);
				Callable.From(() =>
				{
					foreach (var completionItem in completions.ItemsList)
					{
						AddCodeCompletionOption(CodeCompletionKind.Class, completionItem.DisplayText, completionItem.DisplayText);
					}
					// partially working - displays menu only when caret is what CodeEdit determines as valid
					UpdateCodeCompletionOptions(true);
					//RequestCodeCompletion(true);
					GD.Print($"Found {completions.ItemsList.Count} completions, displaying menu");
				}).CallDeferred();
			}
			catch (Exception ex)
			{
				GD.Print(ex);
			}
		});
	}
	
	private (int line, int col) GetCaretPosition()
	{
		var caretColumn = GetCaretColumn();
		var caretLine = GetCaretLine();
		return (caretLine, caretColumn);
	}
}