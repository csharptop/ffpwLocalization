using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
namespace ffpw_localization;

public class ProjectAnalyser
{
    public ProjectAnalyser()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public HashSet<string> Literals { get; } = [];
    private readonly string _interfaceName = "Localization.Common.ILocalizer";
    public async Task Run(string path)
    {
        using var workspace = MSBuildWorkspace.Create();
        
        var projectPath = path;
        var project = await workspace.OpenProjectAsync(projectPath);
        
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                continue;

            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel == null)
                continue;
            var compilation = await document.Project.GetCompilationAsync();
            var interfaceSymbol = compilation?.GetTypeByMetadataName(_interfaceName);
            if (interfaceSymbol == null)
            {
                Console.WriteLine($"Interface {_interfaceName} not found in the project.");
                continue;
            }
            var root = await syntaxTree.GetRootAsync();
            var methodCallFinder = new MethodCallFinder(semanticModel);
            methodCallFinder.Visit(root);

            foreach (var methodCall in methodCallFinder.MethodCalls)
            {
                Console.WriteLine($"Method call found: {methodCall}");

                var fullyQualifiedMethodName = methodCallFinder.GetFullyQualifiedMethodName(methodCall);
                if (fullyQualifiedMethodName != null)
                {
                    Console.WriteLine($"Fully qualified method name: {fullyQualifiedMethodName}");
                }
                else
                {
                    Console.WriteLine("Could not determine fully qualified method name.");
                }
                if (methodCallFinder.IsImplementationOfInterfaceMethod(methodCall,interfaceSymbol))
                {
                    Console.WriteLine("This method call is an implementation of the specified interface.");
                    // Get the first argument's expression
                    var expression = methodCall.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                    if (expression != null)
                    {
                        var constantValue = semanticModel.GetConstantValue(expression);
                        if (constantValue.HasValue)
                        {
                            Console.WriteLine($"Argument value: {constantValue.Value}");
                            Literals.Add(constantValue.Value.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Argument could not be evaluated to a constant value.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("This method call is NOT an implementation of the specified interface.");
                }

            }
        }
    }
}