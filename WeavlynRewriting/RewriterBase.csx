using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

public abstract class RewriterBase
{

    public void ProcessProjectFiles(string projectRootPath, string generatedSubdirectoryName,
            IEnumerable<string> sourceFiles)
    {
        var generatedRoot = Path.Combine(projectRootPath, generatedSubdirectoryName);
        foreach (var originalPath in sourceFiles)
        {
            if (!projectRootPath.EndsWith(@"\"))
                projectRootPath += @"\";
            var pathRelativeToProject = originalPath.Substring(projectRootPath.Length);
            var generatedPath = Path.Combine(generatedRoot, pathRelativeToProject);
            var generatedSubDir = Path.GetDirectoryName(generatedPath);
            if (!Directory.Exists(generatedSubDir))
                Directory.CreateDirectory(generatedSubDir);
            using (var originalStream = new FileStream(originalPath, FileMode.Open, FileAccess.Read))
            using (var generatedStream = new FileStream(generatedPath, FileMode.OpenOrCreate))
            {
                var hash = GetHash(originalStream);
                var hashCommentString = HashCommentPrefix + hash;
                var hashMatches = File.Exists(generatedPath) &&
                        DoesHashCommentMatch(hashCommentString, generatedStream);
                if (!hashMatches)
                {
                    originalStream.Position = 0;
                    var relativePathFromGeneratedToOriginal =
                        GetRelativePathFromGeneratedFileToOriginal(originalPath, generatedPath);
                    RewriteSourceCode(relativePathFromGeneratedToOriginal,
                        hashCommentString, originalStream, generatedStream);
                }
            }
        }
    }

    public void ProcessProjectFiles(string projectRootPath, string generatedSubdirectoryName)
    {
        var generatedRoot = Path.Combine(projectRootPath, generatedSubdirectoryName);
        //Ignore the obj folder because it might have some compiler-generated files.
        var objFolder = Path.Combine(projectRootPath, @"obj\");
        var ignoredFolders = new[] { generatedRoot, objFolder };
        var sourceFiles = Directory.EnumerateFiles(projectRootPath, "*.cs", SearchOption.AllDirectories)
            .Where(a => !ignoredFolders.Any(ignored => a.StartsWith(ignored))).ToArray();
        ProcessProjectFiles(projectRootPath, generatedSubdirectoryName, sourceFiles);
    }


    public virtual void RewriteSourceCode(string relativePathFromGeneratedToOriginal,
            string hashCommentString, System.IO.Stream sourceCodeStream, System.IO.Stream generatedStream)
    {
        var reader = new System.IO.StreamReader(sourceCodeStream);
        var sourceCode = reader.ReadToEnd();

        var rewritten = RewriteSourceCode(relativePathFromGeneratedToOriginal, hashCommentString, sourceCode);

        generatedStream.SetLength(0);
        var generatedStreamWriter = new System.IO.StreamWriter(generatedStream);
        generatedStreamWriter.Write(rewritten);
        generatedStreamWriter.Flush();
    }

    public abstract string RewriteSourceCode(string relativePathFromGeneratedToOriginal,
            string hashCommentString, string sourceCode);


    protected string GetRelativePathFromGeneratedFileToOriginal(string originalPath, string generatedPath)
    {
        var folderOfGeneratedFile = Path.GetDirectoryName(generatedPath);
        if (!folderOfGeneratedFile.EndsWith(@"\"))
            folderOfGeneratedFile += @"\";
        return new Uri(folderOfGeneratedFile)
            .MakeRelativeUri(new Uri(originalPath)).ToString().Replace("/", @"\");
    }


    private bool DoesHashCommentMatch(string hashCommentString, Stream generatedStream)
    {
        var existingGeneratedStreamReader = new StreamReader(generatedStream);
        var existingGeneratedFirstLine = existingGeneratedStreamReader.ReadLine();
        return existingGeneratedFirstLine != null && 
                existingGeneratedFirstLine.StartsWith(hashCommentString);
    }

    protected const string HashCommentPrefix = "//HashOfOriginal: ";

    private string GetHash(Stream s)
    {
        using (var md5 = new MD5CryptoServiceProvider())
            return GetHash(s, md5);
    }
    //private string GetHash(string filePath, HashAlgorithm hasher)
    //{
    //    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    //        return GetHash(fs, hasher);
    //}
    private string GetHash(Stream s, HashAlgorithm hasher)
    {
        var hash = hasher.ComputeHash(s);
        var hashStr = Convert.ToBase64String(hash);
        return hashStr.TrimEnd('=');
    }
}
