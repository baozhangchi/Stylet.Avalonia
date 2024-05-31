using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stylet.Avalonia.CodeGenerator;

public partial class StyletCodeGenerator
{
    private static void RegisterViewModelsGenerator(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.AdditionalTextsProvider
                .Where(x => Path.GetExtension(x.Path).Equals(".axaml", StringComparison.OrdinalIgnoreCase)).Collect(),
            (sourceProductionContext, additionalTexts) =>
            {
                XNamespace xNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
                foreach (var additionalText in additionalTexts)
                {
                    if (Path.GetFileNameWithoutExtension(additionalText.Path) == "App") continue;

                    var doc = XDocument.Load(additionalText.Path);
                    var viewClass = doc.Root?.Attribute(xNamespace + "Class")?.Value;
                    if (!string.IsNullOrWhiteSpace(viewClass))
                    {
                        var viewModelClass = viewClass.Replace("View", "ViewModel");
                        var viewModelName = viewModelClass.Split('.').Last();
                        var viewModelNs = viewModelClass.Substring(0, viewModelClass.Length - viewModelName.Length - 1);
                        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(viewModelName)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                            .AddBaseListTypes(
                                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ViewModelBase")))
                            .AddMembers(SyntaxFactory.ParseMemberDeclaration(
                                "private readonly IWindowManager _windowManager=Ioc.Get<IWindowManager>();")!);
                        sourceProductionContext.AddSource($"{viewModelName}.g.cs",
                            GenerateFullCode(viewModelNs, classDeclarationSyntax, "Stylet"));
                    }
                }
            });
    }
}