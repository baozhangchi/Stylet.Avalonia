using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stylet.Avalonia.CodeGenerator;

public partial class StyletCodeGenerator
{
    private static void RegisterExtensionsGenerator(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider),
            (sourceProductionContext, combine) =>
            {
                var analyzerConfigOptions = combine.Left;
                var compilation = combine.Right;
                var baseNamespace = compilation.AssemblyName!;
                if (analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.rootnamespace", out var ns))
                    baseNamespace = ns;

                GenerateVisualExtensions(sourceProductionContext, baseNamespace);
                GenerateEnumExtensions(sourceProductionContext, baseNamespace);
            });
    }

    private static void GenerateEnumExtensions(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var className = "EnumExtensions";
        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).AddMembers(SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("string"),
                    SyntaxFactory.Identifier("GetDescription")).WithBody(SyntaxFactory.Block())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)).AddParameterListParameters(SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier("@enum"))
                    .WithType(SyntaxFactory.ParseTypeName("System.Enum"))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ThisKeyword))).AddBodyStatements(
                    SyntaxFactory.ParseStatement("var type = @enum.GetType();"),
                    SyntaxFactory.ParseStatement("var fieldInfo = type.GetField(@enum.ToString());"),
                    SyntaxFactory.ParseStatement(
                        "if (fieldInfo != null){var attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();if (attribute != null) return attribute.Description;}"),
                    SyntaxFactory.ParseStatement("return @enum.ToString();")));


        sourceProductionContext.AddSource("EnumExtensions.g.cs",
            GenerateFullCode(baseNamespace, classDeclarationSyntax, "System.ComponentModel",
                "System.Reflection"));
    }

    private static void GenerateVisualExtensions(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var className = "VisualExtensions";

        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddMembers(GenerateMethod("GetVisualParentUtil", "T?", MethodModifier.Public | MethodModifier.Static,
                    new Dictionary<string, string> { { "T", "Visual" } },
                    new Dictionary<string, (string Type, bool fromThis)> { { "element", ("Visual", true) } },
                    "var parent = element.GetVisualParent() ?? element.Parent as Visual;",
                    "return parent is T result ? result : parent != null ? parent.GetVisualParentUtil<T>() : null;"),
                GenerateMethod("GetItemIndexAtPoint", "int", MethodModifier.Public | MethodModifier.Static,
                    new Dictionary<string, (string Type, bool fromThis)>
                        { { "control", ("ItemsControl", true) }, { "point", ("Point", false) } },
                    "for (var i = 0; i < control.Items.Count; i++){",
                    "var container = control.ContainerFromIndex(i)!;",
                    "if (container.Bounds.Contains(point)) return i;}", "return -1;"));

        sourceProductionContext.AddSource($"{className}.g.cs",
            GenerateFullCode(baseNamespace, classDeclarationSyntax, "Avalonia", "Avalonia.Controls",
                "Avalonia.VisualTree"));
    }
}