using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SharpIDE.Application.Features.Analysis;

public readonly record struct SharpIdeDiagnostic(LinePositionSpan Span, Diagnostic Diagnostic, string FilePath);
