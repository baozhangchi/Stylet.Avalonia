﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stylet.Avalonia.CodeGenerator;

[Generator(LanguageNames.CSharp)]
public partial class StyletCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterExtensionsGenerator(context);
        RegisterBaseTypes(context);
        context.RegisterSourceOutput(context.CompilationProvider, (sourceProductionContext, compilation) =>
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                foreach (var declarationSyntax in syntaxTree.GetCompilationUnitRoot().DescendantNodes()
                             .OfType<ClassDeclarationSyntax>())
                {
                    var declaredSymbol = semanticModel.GetDeclaredSymbol(declarationSyntax);
                    if (declaredSymbol != null &&
                        declaredSymbol.AllInterfaces.Any(x => x.Name == "INotifyPropertyChanged") &&
                        !declaredSymbol.InheritsFrom("Stylet.PropertyChangedBase"))
                    {
                        var className = declaredSymbol.Name;
                        var ns = declaredSymbol.ContainingNamespace.ToDisplayString();
                        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                            .AddAttributeLists(SyntaxFactory.AttributeList(
                                SyntaxFactory.SeparatedList(new List<AttributeSyntax>
                                {
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.ParseName("DoNotNotify"))
                                })));
                        sourceProductionContext.AddSource($"{className}.g.cs",
                            GenerateFullCode(ns, classDeclarationSyntax, "PropertyChanged"));
                    }
                }
            }
        });
    }

    private static BlockSyntax GenerateMethodBody(params string[] statements)
    {
        return SyntaxFactory.Block(statements.Select(x => SyntaxFactory.ParseStatement(x)));
    }

    private static string GenerateFullCode(string @namespace, ClassDeclarationSyntax classDeclarationSyntax,
        params string[] usings)
    {
        var compilationUnitSyntax = SyntaxFactory.CompilationUnit()
            .AddUsings(usings.Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray())
            .AddMembers(SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
                .AddMembers(classDeclarationSyntax));
        return
            $"// <auto-generated />\r\n#nullable enable\r\n\r\n{compilationUnitSyntax.NormalizeWhitespace().ToFullString()}";
    }
}