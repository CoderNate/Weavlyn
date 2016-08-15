# Weavlyn
You want to do some fancy metaprogramming so that you don't have to dirty your source code with lots of boilerplate.  Microsoft's Roslyn makes it easy to parse your original source and automatically generate a new file with boilerplate added in.  And you can do some MSBuild trickery to swap out your original source files and replace them with generated ones right before compile time.

Ah, but what about debugging?  It's a real pain to be editing your original source code and then have to track down the generated version of it in order to set breakpoints.

That's where the #line directive comes to the rescue.  Write \<ProjectRoot\>/Program.cs like this:
```csharp
class MyClass {
	void DoNothing() {}
}
```
And generate \<ProjectRoot\>/Generated/Program.cs like this:
```csharp
#line 1 "..\Program.cs"
class MyClass {
	void DoNothing() {
#line hidden
	System.Console.WriteLine("Executing DoNothing");
#line 2 "..\Program.cs"
	}
}
```
That's all it takes to be able to step through your original source code while debugging.  This is how the generated source code for an ASP cshtml file works.

Importing a pretty simple targets file in your csproj takes care of calling the executable that generates source code as well as swapping out original source files and replacing them with generated code.  Installing Weavlyn will put something like this into your csproj file:
```xml
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  ...
  <PropertyGroup>
    <RewriterName>ConsoleLoggingRewriter.csx</RewriterName>
  </PropertyGroup>
  <Import Project="$(ProjectDir)\..\WeavlynRewriting\Weavlyn.targets" />
</Project>
```

By storing a hash of the source file in a comment at the top of the generated file, you can speed up compilation by avoiding the cost of re-generating code for source files with unchanged hashes at every build.

## Installing Weavlyn
From the root of your solution, run the following in Powershell (make sure your powershell ExecutionPolicy allows for executing un-signed scripts):
```powershell
Invoke-WebRequest "https://raw.githubusercontent.com/CoderNate/Weavlyn/master/Weavlyn.ps1" -OutFile Weavlyn.ps1
& .\Weavlyn.ps1 -OPERATION install -ProjectFile .\ProjectToProcess\ProjectToProcess.csproj -RewriterName "ExampleRewriter.csx"
```
And now just modify ExampleRewriter.csx to have it make whatever source code modifications you want.  See ConsoleLoggingRewriter.csx in the Github repository for an example that inserts a Console.WriteLine statement in every function.