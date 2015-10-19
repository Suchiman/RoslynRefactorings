using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stringpolator
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StringpolatorCodeRefactoringProvider)), Shared]
    internal class StringpolatorCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semantic = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            var firstInvocationAround = node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault() ?? node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (firstInvocationAround == null)
            {
                return;
            }

            var info = semantic.GetSymbolInfo(firstInvocationAround.Expression).Symbol as IMethodSymbol;

            if (info?.Name == "Concat" && info.ContainingNamespace?.Name == "System")
            {
                var action = CodeAction.Create("Convert to interpolated string", c => ConvertToInterpolatedString(context.Document, root, firstInvocationAround, c));
                context.RegisterRefactoring(action);
            }
        }

        private Task<Document> ConvertToInterpolatedString(Document document, SyntaxNode root, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            string previousString = null;
            var parts = new List<InterpolatedStringContentSyntax>(invocation.ArgumentList.Arguments.Count);
            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[i];
                var literal = argument.Expression as LiteralExpressionSyntax;

                if (literal?.IsKind(SyntaxKind.StringLiteralExpression) ?? false)
                {
                    //strings needs to be collapsed, otherwise VS will insert additional whitespaces
                    previousString += literal.Token.Text.Substring(1, literal.Token.Text.Length - 2);
                }
                else
                {
                    if (previousString != null)
                    {
                        parts.Add(InterpolatedStringGenerator.TextPart(previousString));
                        previousString = null;
                    }
                    parts.Add(InterpolatedStringGenerator.ExpressionPart(argument.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia()));
                }
            }
            if (previousString != null)
            {
                parts.Add(InterpolatedStringGenerator.TextPart(previousString));
            }

            SyntaxNode interpolated = InterpolatedStringGenerator.InterpolatedString(parts);

            root = root.ReplaceNode(invocation, interpolated);

            return Task.FromResult(document.WithSyntaxRoot(root));
        }
    }
}