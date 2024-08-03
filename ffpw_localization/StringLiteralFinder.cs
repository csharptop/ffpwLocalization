using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ffpw_localization;

public class StringLiteralFinder : CSharpSyntaxWalker
{
    public List<LiteralExpressionSyntax> StringLiterals { get; } = [];

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        if (node.Right is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            StringLiterals.Add(literal);
        }

        base.VisitAssignmentExpression(node);
    }

    public override void VisitArgument(ArgumentSyntax node)
    {
        if (node.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            StringLiterals.Add(literal);
        }

        base.VisitArgument(node);
    }
}