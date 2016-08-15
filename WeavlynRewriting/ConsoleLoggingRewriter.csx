#r "Microsoft.CodeAnalysis.dll"
#r "Microsoft.CodeAnalysis.CSharp.dll"
#load "RewriterBase.csx"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;


public class ConsoleLoggingRewriter: RewriterBase
{
	
	public override string RewriteSourceCode(string relativePathFromGeneratedToOriginal,
			string hashCommentString, string sourceCode)
	{
		var tree = CSharpSyntaxTree.ParseText(sourceCode);
		var rewrittenSyntax = new CustomRewriter(tree, relativePathFromGeneratedToOriginal).Visit(tree.GetRoot());
		var rewrittenString = hashCommentString + Environment.NewLine +
				@"#line 1 """ + relativePathFromGeneratedToOriginal + "\"" + Environment.NewLine
				+ rewrittenSyntax.ToFullString();
		return rewrittenString;
	}

	private class CustomRewriter : CSharpSyntaxRewriter
	{
		private readonly string _relativePathToOriginalSourceFile;
		private readonly SyntaxTree _tree;
		public CustomRewriter(SyntaxTree tree, string relativePathToOriginalSourceFile)
		{
			_tree = tree;
			_relativePathToOriginalSourceFile = relativePathToOriginalSourceFile;
		}

		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			if (node.Body == null && node.ExpressionBody == null)
				return base.VisitMethodDeclaration(node);

			//Need to be very careful not to insert or remove existing NewLines or you'll be off a 
			//line or two while stepping through the original source code file.

			Func<int, bool, StatementSyntax> createNewStatement = (lineNum, addStartingNewLine) =>
			{
				var trailingLineDirective = "#line " + lineNum.ToString() + " \"" + 
						_relativePathToOriginalSourceFile + "\"";
				var parsedTrivia = SyntaxFactory.ParseLeadingTrivia(
						Environment.NewLine + trailingLineDirective + Environment.NewLine);
				var writelineMsg = "Entering " + node.Identifier.Text;
				var newStatement = SyntaxFactory.ParseStatement((addStartingNewLine ? Environment.NewLine : "")
					+ "#line hidden" 
					+ Environment.NewLine +
					"System.Console.WriteLine(\"" + writelineMsg + "\");").WithTrailingTrivia(parsedTrivia);
				return newStatement;
			};

			BlockSyntax newBody;
			if (node.Body != null)
			{
				var firstStatement = node.Body.Statements.FirstOrDefault();
				var span = firstStatement != null ? firstStatement.Span : node.Body.CloseBraceToken.Span;
                var line = _tree.GetLineSpan(span).StartLinePosition.Line;
				//If it's an empty body like {} then add an extra starting NewLine
				var addExtraStartingLine = _tree.GetLineSpan(node.Body.CloseBraceToken.Span)
						.StartLinePosition.Line == line;
                //Need to add 1 because the line directive assumes that the first line in a file is line 1, but StartLinePosition starts at 0.
				var newStatement = createNewStatement(line + 1, addExtraStartingLine);
				var newStatements = node.Body.Statements.Insert(0, newStatement);
				newBody = node.Body.WithStatements(newStatements).WithTriviaFrom(node.Body);
			}
			else if (node.ExpressionBody != null)
			{
				var expresn = node.ExpressionBody.Expression;
				var line = _tree.GetLineSpan(expresn.Span).StartLinePosition.Line;
                //Need to add 1 because the line directive assumes that the first line in a file is line 1, but StartLinePosition starts at 0.
                var newStatement = createNewStatement(line + 1, true);
				var originalStatement = SyntaxFactory.ExpressionStatement(expresn);
				newBody = SyntaxFactory.Block(newStatement, originalStatement)
					.WithTrailingTrivia(node.SemicolonToken.TrailingTrivia);
			}
			else
				throw new NotImplementedException("This shouldn't happen.");

			//Make sure there's no semicolon if it was an expression body because we turned it into
			//a regular body statement.
			return base.VisitMethodDeclaration(node.WithExpressionBody(null)
				.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)).WithBody(newBody));
		}
	}

}

var projectDir = Args[0];
var generatedSubfolderName = Args[1];
new ConsoleLoggingRewriter().ProcessProjectFiles(projectDir, generatedSubfolderName);