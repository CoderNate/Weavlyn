# Weavlyn
You want to do some fancy metaprogramming so that you don't have to dirty your source code with lots of boilerplate.  Microsoft's Roslyn makes it easy to parse your original source and automatically generate a new file with boilerplate added in.  And you can do some MSBuild trickery to swap out your original source files and replace them with generated ones right before compile time.

Ah, but what about debugging?  It's a real pain when you're switching between editing and debugging if you have to set breakpoints in the generated code and try to remember to switch back to your original code for editing.

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

Importing a pretty simple targets file in your csproj takes care of calling the executable that generates source code as well as swapping out original source files and replacing them with generated code.  See ProjectToProcess.csproj for an example of how to set the RewriterExecutablePath and import the Weavlyn.targets file:
```xml
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  ...
  <PropertyGroup>
    <RewriterExecutablePath>$(ProjectDir)\..\ExampleRewriter\bin\Debug\ExampleRewriter.exe</RewriterExecutablePath>
    <GeneratedDirName>Generated</GeneratedDirName>
  </PropertyGroup>
  <Import Project="$(ProjectDir)\..\lib\Weavlyn.targets" />
</Project>
```

By storing a hash of the source file in a comment at the top of the generated file, you can speed up compilation by avoiding the cost of re-generating code for source files with unchanged hashes at every build.

## Installing Weavlyn
It would be really cool if you could just install a Weavlyn NuGet package like you can with [Fody](https://github.com/Fody/Fody).  But one would need to pick a version of Microsoft.CodeAnalysis.* (8+ MB of Roslyn DLLs) to live side-by-side with Weavlyn.dll in the NuGet package and that just doesn't feel right.  So for now the only useful stuff in this repository is the Weavlyn.targets file and RewriterBase.cs. 