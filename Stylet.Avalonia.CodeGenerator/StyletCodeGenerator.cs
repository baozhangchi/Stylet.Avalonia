using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stylet.Avalonia.CodeGenerator;

[Generator(LanguageNames.CSharp)]
public class StyletCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (sourceProductionContext, compilation) =>
            {
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    foreach (var classDeclarationSyntax in syntaxTree.GetCompilationUnitRoot().DescendantNodes()
                                 .OfType<ClassDeclarationSyntax>())
                    {
                        if (classDeclarationSyntax.Identifier.Text == "App")
                        {
                            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                            var declaredSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                            if (declaredSymbol != null)
                            {
                                sourceProductionContext.AddSource("StyletApplication.g.cs",
                                    SyntaxFactory.CompilationUnit()
                                        .WithUsings(syntaxTree.GetCompilationUnitRoot().Usings)
                                        .AddMembers(
                                            SyntaxFactory.FileScopedNamespaceDeclaration(
                                                    SyntaxFactory.ParseName(
                                                        declaredSymbol.ContainingNamespace.ToString()!))
                                                .AddMembers(SyntaxFactory.ClassDeclaration("StyletApplication")
                                                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                        SyntaxFactory.Token(SyntaxKind.AbstractKeyword),
                                                        SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                                                    .AddTypeParameterListParameters(
                                                        SyntaxFactory.TypeParameter("TRootViewModel"))
                                                    .AddBaseListTypes(SyntaxFactory.SimpleBaseType(
                                                        SyntaxFactory.ParseTypeName(
                                                            $"global::{typeof(StyletApplication)}"))).AddMembers(
                                                        SyntaxFactory
                                                            .MethodDeclaration(
                                                                SyntaxFactory.ParseTypeName($"{typeof(Control)}?"),
                                                                "DisplayRootView")
                                                            .AddModifiers(
                                                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                                                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                                                            .WithBody(SyntaxFactory.Block()).AddBodyStatements(
                                                                SyntaxFactory.ParseStatement($"if (ApplicationLifetime is global::{typeof(IClassicDesktopStyleApplicationLifetime)} desktop)"),
                                                                SyntaxFactory.ParseStatement("{"),
                                                                SyntaxFactory.ParseStatement($"var rootViewModel = global::{typeof(IoC)}.Get<TRootViewModel>();"),
                                                                SyntaxFactory.ParseStatement($"return global::{typeof(IoC)}.Get<global::{typeof(IViewManager)}>().CreateAndBindViewForModelIfNecessary(rootViewModel) as global::{typeof(TopLevel)};"),
                                                                SyntaxFactory.ParseStatement("}"),
                                                                SyntaxFactory.ParseStatement($"throw new global::{typeof(NotSupportedException)}();")))))
                                        .NormalizeWhitespace().ToFullString());
                                return;
                            }
                        }
                    }
                }
            });
    }
}