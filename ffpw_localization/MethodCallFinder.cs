using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ffpw_localization;


class MethodCallFinder : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    public List<InvocationExpressionSyntax> MethodCalls { get; } = new();

    public MethodCallFinder(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        MethodCalls.Add(node);
        base.VisitInvocationExpression(node);
    }

    public string? GetFullyQualifiedMethodName(InvocationExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol != null)
        {
            if (methodSymbol.MethodKind == MethodKind.DelegateInvoke)
            {
                var delegateMethodSymbol = GetTargetMethodSymbol(node.Expression);
                if (delegateMethodSymbol != null)
                {
                    return delegateMethodSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                }
            }
            return methodSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        return null;
    }

    private IMethodSymbol? GetTargetMethodSymbol(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifierNameSyntax)
        {
            var symbol = _semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
            if (symbol is ILocalSymbol localSymbol)
            {
                var type = localSymbol.Type as INamedTypeSymbol;
                if (type != null && type.DelegateInvokeMethod != null)
                {
                    var syntaxReference = localSymbol.DeclaringSyntaxReferences[0];
                    var variableDeclarator = (VariableDeclaratorSyntax)syntaxReference.GetSyntax();
                    if (variableDeclarator.Initializer != null)
                    {
                        var value = variableDeclarator.Initializer.Value;
                        var methodSymbol = _semanticModel.GetSymbolInfo(value).Symbol as IMethodSymbol;
                        return methodSymbol;
                    }
                }
            }
        }
        return null;
    }
    public bool IsImplementationOfInterfaceMethod(InvocationExpressionSyntax node,INamedTypeSymbol interfaceSymbol)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol == null) return false;

        // Check if the method belongs to an interface, or a class implementing the interface
        if (IsInterfaceMethodImplementation(methodSymbol,interfaceSymbol))
        {
            return true;
        }

        // Handle the case where the method is invoked through a delegate
        if (methodSymbol.MethodKind == MethodKind.DelegateInvoke)
        {
            var delegateMethodSymbol = GetTargetMethodSymbol(node.Expression);
            if (delegateMethodSymbol != null && IsInterfaceMethodImplementation(delegateMethodSymbol,interfaceSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInterfaceMethodImplementation(IMethodSymbol methodSymbol,INamedTypeSymbol interfaceSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        return SymbolEqualityComparer.Default.Equals(containingType,interfaceSymbol) || containingType.AllInterfaces.Contains(interfaceSymbol);
    }
}