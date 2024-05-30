using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                {
                    baseNamespace = ns;
                }

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
            GenerateFullCode(@baseNamespace, classDeclarationSyntax, "System.ComponentModel",
                "System.Reflection"));
    }

    private static void GenerateVisualExtensions(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var className = "VisualExtensions";
        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddMembers(SyntaxFactory.MethodDeclaration(SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)), SyntaxFactory.ParseTypeName("T?"),
                            explicitInterfaceSpecifier: null!,
                            SyntaxFactory.Identifier("GetVisualParentUtil"),
                            SyntaxFactory.TypeParameterList(SyntaxFactory.Token(SyntaxKind.LessThanToken),
                                SyntaxFactory.SeparatedList(new List<TypeParameterSyntax>
                                    { SyntaxFactory.TypeParameter("T") }),
                                SyntaxFactory.Token(SyntaxKind.GreaterThanToken)),
                            SyntaxFactory.ParameterList(SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                                SyntaxFactory.SeparatedList(new List<ParameterSyntax>
                                {
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.List<AttributeListSyntax>(),
                                        SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)),
                                        SyntaxFactory.ParseTypeName("Visual"),
                                        SyntaxFactory.Identifier("element"), null)
                                }),
                                SyntaxFactory.Token(SyntaxKind.CloseParenToken)),
                            SyntaxFactory.List(
                                new[]
                                {
                                    SyntaxFactory.TypeParameterConstraintClause(
                                        SyntaxFactory.Token(SyntaxKind.WhereKeyword),
                                        SyntaxFactory.IdentifierName("T"),
                                        SyntaxFactory.Token(SyntaxKind.ColonToken),
                                        SyntaxFactory.SeparatedList(
                                            new TypeParameterConstraintSyntax[]
                                            {
                                                SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName("Visual"))
                                            }))
                                }), SyntaxFactory.Block(),
                            SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken))
                        .AddBodyStatements(SyntaxFactory.ParseStatement("var parent = element.GetVisualParent();"),
                            SyntaxFactory.ParseStatement(
                                "return parent is T result ? result : parent != null ? parent.GetVisualParentUtil<T>() : null;")),
                    GenerateMethod("GetItemIndexAtPoint", "int", MethodModifier.Public | MethodModifier.Static,
                        "for (var i = 0; i < control.Items.Count; i++){",
                        "var container = control.ContainerFromIndex(i)!;",
                        "if (container.Bounds.Contains(point)) return i;}", "return -1;").AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("control"))
                            .WithType(SyntaxFactory.ParseTypeName("ItemsControl"))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.ThisKeyword)), SyntaxFactory
                            .Parameter(SyntaxFactory.Identifier("point"))
                            .WithType(SyntaxFactory.ParseTypeName("Point"))))
            ;

        sourceProductionContext.AddSource("VisualExtensions.g.cs",
            GenerateFullCode(baseNamespace, classDeclarationSyntax, "Avalonia","Avalonia.Controls",
                "Avalonia.VisualTree"));
    }

    private static MethodDeclarationSyntax GenerateMethod(string methodName, string returnTypeName,
        MethodModifier modifier, params string[] statements)
    {
        return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName),
                SyntaxFactory.Identifier(methodName)).AddModifiers(modifier.ToTokens())
            .WithBody(GenerateMethodBody(statements));
    }
}