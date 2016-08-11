::The following will fail if Nuget.exe isn't in your path environment variable:
NuGet.exe Restore
if %errorlevel% neq 0 exit /b %errorlevel%

::Make sure the rewriter is built first so that the compiled EXE exists before ProjectToProcess gets built
MSBuild ExampleRewriter\ExampleRewriter.csproj
MSBuild Weavlyn.sln