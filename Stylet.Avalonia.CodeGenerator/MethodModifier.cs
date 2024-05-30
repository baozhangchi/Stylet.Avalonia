using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stylet.Avalonia.CodeGenerator;

[Flags]
public enum MethodModifier
{
    Public = 1,
    Internal = 1 << 1,
    Protected = 1 << 2,
    Private = 1 << 3,
    Partial = 1 << 4,
    Static = 1 << 5,
    Async = 1 << 6,
    Override = 1 << 7
}

public static class MethodModifierExtensions
{
    public static SyntaxToken[] ToTokens(this MethodModifier modifier)
    {
        return Enum.GetValues(typeof(MethodModifier)).Cast<MethodModifier>()
            .Where(x => modifier.HasFlag((MethodModifier)x))
            .Select(x => SyntaxFactory.Token((SyntaxKind)Enum.Parse(typeof(SyntaxKind), $"{x}Keyword"))).ToArray();
    }
}