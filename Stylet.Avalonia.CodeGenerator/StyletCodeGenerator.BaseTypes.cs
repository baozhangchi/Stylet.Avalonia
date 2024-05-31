using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stylet.Avalonia.CodeGenerator;

public partial class StyletCodeGenerator
{
    private static void RegisterBaseTypesGenerator(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.CompilationProvider,
            (sourceProductionContext, compilation) =>
            {
                foreach (var syntaxTree in compilation.SyntaxTrees)
                foreach (var classDeclarationSyntax in syntaxTree.GetCompilationUnitRoot().DescendantNodes()
                             .OfType<ClassDeclarationSyntax>())
                    if (classDeclarationSyntax.Identifier.Text == "App")
                    {
                        var semanticModel = compilation.GetSemanticModel(syntaxTree);
                        var declaredSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                        if (declaredSymbol != null)
                        {
                            var baseNamespace = declaredSymbol.ContainingNamespace.ToString()!;
                            GenerateIoc(sourceProductionContext, baseNamespace);
                            GenerateStyletApplication(sourceProductionContext, baseNamespace);
                            GenerateViewModelBase(sourceProductionContext, baseNamespace);
                            return;
                        }
                    }
            });
    }

    private static void GenerateViewModelBase(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration("ViewModelBase")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Conductor<ViewModelBase>")));
        sourceProductionContext.AddSource("ViewModelBase.g.cs",
            GenerateFullCode($"{baseNamespace}.ViewModels", classDeclarationSyntax, "Stylet", "Avalonia.Controls"));
    }

    private static void GenerateStyletApplication(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration("StyletApplication")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AbstractKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList(
                SyntaxFactory.SeparatedList(new List<AttributeSyntax>
                {
                    SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName("DoNotNotify"))
                })))
            .AddTypeParameterListParameters(
                SyntaxFactory.TypeParameter("TRootViewModel"))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName("StyletApplication"))).AddMembers(
                SyntaxFactory
                    .MethodDeclaration(
                        SyntaxFactory.ParseTypeName("Control?"),
                        "DisplayRootView")
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                    .WithBody(GenerateMethodBody(
                        "if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)",
                        "return IoC.Get<IViewManager>().CreateAndBindViewForModelIfNecessary(IoC.Get<TRootViewModel>()!) as TopLevel;",
                        "throw new NotSupportedException();")),
                GenerateMethod("OnStart", "void", MethodModifier.Protected | MethodModifier.Override, "base.OnStart();",
                    "Ioc.GetInstance = GetInstance;", "Ioc.GetAllInstance = GetInstances;")
            );
        sourceProductionContext.AddSource("StyletApplication.g.cs",
            GenerateFullCode(baseNamespace, classDeclarationSyntax, "Avalonia.Controls.ApplicationLifetimes",
                "PropertyChanged",
                "Stylet.Avalonia", "Stylet",
                "Avalonia.Controls", "System"));
    }

    private static void GenerateIoc(SourceProductionContext sourceProductionContext, string baseNamespace)
    {
        var iocClassDeclaration = SyntaxFactory.ClassDeclaration("Ioc")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)).AddMembers(
                SyntaxFactory.ParseMemberDeclaration(
                    "public static Func<Type, string?, object> GetInstance {get; set;}= (type,key)=>throw new NotImplementedException();")
                !,
                SyntaxFactory.ParseMemberDeclaration(
                    "public static Func<Type, global::System.Collections.Generic.IEnumerable<object>> GetAllInstance {get; set;}= (type)=>throw new NotImplementedException();")
                !,
                SyntaxFactory.ParseMemberDeclaration(
                    "public static T Get<T>(string? key=null){return (T)GetInstance(typeof(T),key);}")
                !,
                SyntaxFactory.ParseMemberDeclaration(
                    "public static global::System.Collections.Generic.IEnumerable<T> GetAll<T>(){return GetAllInstance(typeof(T)).Cast<T>();}")
                !);
        sourceProductionContext.AddSource("Ioc.g.cs",
            GenerateFullCode(baseNamespace, iocClassDeclaration, "System", "System.Linq"));
    }
}