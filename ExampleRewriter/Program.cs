using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleRewriter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();  //<-- Uncomment this to debug your rewriter during a build step.
            var projectDir = args[0];
            var generatedSubfolderName = args[1];
            RewriteProjectFiles(projectDir, generatedSubfolderName);
        }

        public static void RewriteProjectFiles(string projectDir, string generatedSubdirectoryName)
        {
            var rewriter = new Rewriter();
            rewriter.ProcessProjectFiles(projectDir, generatedSubdirectoryName);
        }
    }
}
