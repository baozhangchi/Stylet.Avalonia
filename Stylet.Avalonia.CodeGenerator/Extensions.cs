using Microsoft.CodeAnalysis;

namespace Stylet.Avalonia.CodeGenerator;

internal static class Extensions
{
    public static void SendReport(this SourceProductionContext sourceProductionContext, string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor("MG001", nameof(StyletCodeGenerator), message, nameof(StyletCodeGenerator),
                severity, true),
            Location.None));
    }

    public static bool InheritsFrom(this INamedTypeSymbol symbol, string baseTypeName)
    {
        var baseType = symbol.BaseType;

        while (baseType != null)
        {
            if (baseType.ToDisplayString() == baseTypeName)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}