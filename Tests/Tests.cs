using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestFileConversion()
        {

            var code = @"
class C {
    void DoSomethingElse() => System.Math.Round(1.0);

    void DoStuff()
    {
        return;
    }
    void DontDoAnything() {}
}";
            string rewritten;
            using (var codeStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(code)))
            using (var rewrittenStream = new System.IO.MemoryStream())
            {
                for (var i = 0; i < 2; i++)
                {
                    codeStream.Position = 0;
                    rewrittenStream.Position = 0;
                    var rewriter = new ExampleRewriter.Rewriter();
                    rewriter.RewriteSourceCode(@"..\Program.cs", 
                            "//Pretend this is a hash", codeStream, rewrittenStream);
                    rewrittenStream.Position = 0;
                    rewritten = System.Text.Encoding.UTF8.GetString(rewrittenStream.ToArray());
                }
            }
        }


        [Test]
        public void TestProjectConversion()
        {
            var thisAssemblyDir = System.IO.Path.GetDirectoryName(typeof(Tests).Assembly.Location);
            var projectDir = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(thisAssemblyDir, @"..\..\..\ProjectToProcess"));
            ExampleRewriter.Program.RewriteProjectFiles(projectDir, "Generated");
        }

    }
}
