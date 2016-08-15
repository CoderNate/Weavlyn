#r "Microsoft.CodeAnalysis.dll"
#r "Microsoft.CodeAnalysis.CSharp.dll"
#load "RewriterBase.csx"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class Rewriter : RewriterBase
{
    public override string RewriteSourceCode(string relativePathFromGeneratedToOriginal,
            string hashCommentString, string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        //Do some kind of rewriting here.
        return hashCommentString + Environment.NewLine +
                @"#line 1 """ + relativePathFromGeneratedToOriginal + "\"" + Environment.NewLine
                + tree.ToString();
    }
}

var projectDir = Args[0];
var generatedSubfolderName = Args[1];
new Rewriter().ProcessProjectFiles(projectDir, generatedSubfolderName);
