using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Stringpolator
{
    public static class InterpolatedStringGenerator
    {
        public static InterpolatedStringExpressionSyntax InterpolatedString(IEnumerable<InterpolatedStringContentSyntax> parts)
        {
            return InterpolatedStringExpression(Token(InterpolatedStringStartToken))
                .WithContents(List(parts))
                .WithStringEndToken(Token(InterpolatedStringEndToken));
        }

        public static InterpolatedStringContentSyntax TextPart(string text)
        {
            return InterpolatedStringText(Token(TriviaList(), InterpolatedStringTextToken, text, text, TriviaList()));
        }

        public static InterpolatedStringContentSyntax ExpressionPart(ExpressionSyntax syntax)
        {
            return Interpolation(syntax);
        }
    }
}
