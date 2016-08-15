##########################################################################
# This is the Weavlyn script.
# This file was downloaded from https://github.com/CoderNate/Weavlyn
##########################################################################

<#

.SYNOPSIS
A Powershell script for running and installing source code rewriters.

.DESCRIPTION
A Powershell script for running and installing source code rewriters.

.PARAMETER Operation
Install or Run.

.PARAMETER ProjectFile
The proj file of the project that install or run should operate on.

.PARAMETER RewriterName
The name of the rewriter csx script file in the WeavlynRewriting folder.

.PARAMETER GeneratedDirName
Tells the run operation what name to use for the folder that contains the generated code.

.PARAMETER ScriptArgs
Any additional parameters that should be sent to the rewriter script.

.LINK
https://github.com/CoderNate/Weavlyn

#>

[CmdletBinding()]
Param(
    [ValidateSet("Install", "Run")]
    [Parameter(Mandatory=$true)]
	[string]$Operation,
    [Parameter(Mandatory=$true)]
	[string]$ProjectFile,
    [Parameter(Mandatory=$true)]
    [string]$RewriterName,
    #[switch]$Experimental,
    [string]$GeneratedDirName,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

[Reflection.Assembly]::LoadWithPartialName("System.Security") | Out-Null
function MD5HashFile([string] $filePath)
{
    if ([string]::IsNullOrEmpty($filePath) -or !(Test-Path $filePath -PathType Leaf))
    {
        return $null
    }

    [System.IO.Stream] $file = $null;
    [System.Security.Cryptography.MD5] $md5 = $null;
    try
    {
        $md5 = [System.Security.Cryptography.MD5]::Create()
        $file = [System.IO.File]::OpenRead($filePath)
        return [System.BitConverter]::ToString($md5.ComputeHash($file))
    }
    finally
    {
        if ($file -ne $null)
        {
            $file.Dispose()
        }
    }
}

function EnsureDownloaded([string]$sourceURL, [string]$destPath) {
	if (!(Test-Path $destPath)) {
		$fileName = Split-Path $destPath -Leaf
		Write-Verbose -Message ("Downloading " + $fileName + "...")
		try {
			(New-Object System.Net.WebClient).DownloadFile($sourceURL, $destPath)
		} catch {
			Throw "Could not download " + $fileName + "."
		}
	}
}

if (($Operation -eq "run") -and ($GeneratedDirName -eq $null)) {
	throw "Must provide the 'GeneratedDirName' argument when operation is 'run'"
}

#Write-Host "Preparing to run script..."

if(!$PSScriptRoot){
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}

$TOOLS_DIR = Join-Path $PSScriptRoot "tools"
$NUGET_EXE = Join-Path $TOOLS_DIR "nuget.exe"
$NUGET_URL = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$WEAVLYNTARGETS_URL = "https://raw.githubusercontent.com/CoderNate/Weavlyn/master/WeavlynRewriting/Weavlyn.targets"
$REWRITERBASE_URL = "https://raw.githubusercontent.com/CoderNate/Weavlyn/master/WeavlynRewriting/RewriterBase.csx"
$PACKAGES_CONFIG = Join-Path $TOOLS_DIR "packages.config"
$PACKAGES_CONFIG_MD5 = Join-Path $TOOLS_DIR "packages.config.md5sum"
$WEAVLYNREWRITING_DIR = Join-Path $PSScriptRoot "WeavlynRewriting"
$WEAVLYN_TARGETS = Join-Path $WEAVLYNREWRITING_DIR "Weavlyn.targets"
$REWRITERBASE_CSX = Join-Path $WEAVLYNREWRITING_DIR "RewriterBase.csx"
$REWRITER_PATH = (Join-Path $WEAVLYNREWRITING_DIR $RewriterName)
$CSI_DIR = (Join-Path $TOOLS_DIR "Microsoft.Net.Compilers\tools")
$CSI_EXE = (Join-Path $CSI_DIR "csi.exe")

$ProjectFile = Resolve-Path $ProjectFile
if (!(Test-Path $ProjectFile)) {
	throw ("Project file '" + $ProjectFile + "' does not exist.")
}

# Make sure tools folder exists
if ((Test-Path $PSScriptRoot) -and !(Test-Path $TOOLS_DIR)) {
    Write-Verbose -Message "Creating tools directory..."
    New-Item -Path $TOOLS_DIR -Type directory | out-null
}

# Make sure that packages.config exists.
if (!(Test-Path $PACKAGES_CONFIG)) {
    Write-Verbose -Message "Writing packages.config..."
	$packagesConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
	<package id="Microsoft.Net.Compilers" version="1.3.2" />
</packages>
"@
	$packagesConfigContent | Out-File $PACKAGES_CONFIG -Encoding "UTF8"
}

# Try find NuGet.exe in path if it doesn't exist
if (!(Test-Path $NUGET_EXE)) {
    Write-Verbose -Message "Trying to find nuget.exe in PATH..."
    $existingPaths = $Env:Path -Split ';' | Where-Object { (![string]::IsNullOrEmpty($_)) -and (Test-Path $_) }
    $NUGET_EXE_IN_PATH = Get-ChildItem -Path $existingPaths -Filter "nuget.exe" | Select -First 1
    if ($NUGET_EXE_IN_PATH -ne $null -and (Test-Path $NUGET_EXE_IN_PATH.FullName)) {
        Write-Verbose -Message "Found in PATH at $($NUGET_EXE_IN_PATH.FullName)."
        $NUGET_EXE = $NUGET_EXE_IN_PATH.FullName
    }
}

# Try download NuGet.exe if it doesn't exist
EnsureDownloaded $NUGET_URL $NUGET_EXE

# Save nuget.exe path to environment to be available to child scripts
$ENV:NUGET_EXE = $NUGET_EXE

# Restore tools from NuGet?
if(-Not $SkipToolPackageRestore.IsPresent) {
    Push-Location
    Set-Location $TOOLS_DIR

    # Check for changes in packages.config and remove installed tools if true.
    [string] $md5Hash = MD5HashFile($PACKAGES_CONFIG)
    if((!(Test-Path $PACKAGES_CONFIG_MD5)) -Or
      ($md5Hash -ne (Get-Content $PACKAGES_CONFIG_MD5 ))) {
        Write-Verbose -Message "Missing or changed package.config hash..."
        Remove-Item * -Recurse -Exclude packages.config,nuget.exe
    }

    Write-Verbose -Message "Restoring tools from NuGet..."
    $NuGetOutput = Invoke-Expression "&`"$NUGET_EXE`" install -ExcludeVersion -OutputDirectory `"$TOOLS_DIR`""

    if ($LASTEXITCODE -ne 0) {
        Throw "An error occured while restoring NuGet tools."
    }
    else
    {
        $md5Hash | Out-File $PACKAGES_CONFIG_MD5 -Encoding "ASCII"
    }
    Write-Verbose -Message ($NuGetOutput | out-string)
    Pop-Location
}

# Make sure WeavlynRewriting folder exists
if (!(Test-Path $WEAVLYNREWRITING_DIR)) {
    Write-Verbose -Message "Creating WeavlynRewriting directory..."
    New-Item -Path $WEAVLYNREWRITING_DIR -Type directory | out-null
}

# Try download Weavlyn.targets if it doesn't exist
EnsureDownloaded $WEAVLYNTARGETS_URL $WEAVLYN_TARGETS

# Try download RewriterBase if it doesn't exist
EnsureDownloaded $REWRITERBASE_URL $REWRITERBASE_CSX


if ($Operation -eq "run") {
	Write-Host ("Running '" + $REWRITER_PATH + "'")
	$projectDir = Split-Path -parent $ProjectFile
	& "$CSI_EXE" /lib:"$CSI_DIR" "$REWRITER_PATH" "$projectDir" "$GeneratedDirName" $ScriptArgs
	exit $LASTEXITCODE
}
elseif ($Operation -eq "install") {
	
	if (!(Test-Path $REWRITER_PATH)) {
		$exampleScript = @"
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
"@
		$exampleScript | Out-File $REWRITER_PATH -Encoding "UTF8"
	}


	[Reflection.Assembly]::Load("System.Xml.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") | Out-Null
	$xDoc = [System.Xml.Linq.XDocument]::Load($ProjectFile)
	$ns = $xDoc.Root.GetDefaultNamespace()
	$xDoc.Root.Add((New-Object System.Xml.Linq.XElement($ns.GetName("PropertyGroup"), `
		(New-Object System.Xml.Linq.XElement($ns.GetName("RewriterName"), $RewriterName)))))
	$xDoc.Root.Add((New-Object System.Xml.Linq.XElement($ns.GetName("Import"), `
		(New-Object System.Xml.Linq.XAttribute("Project", "`$(ProjectDir)\..\WeavlynRewriting\Weavlyn.targets")))))

	$settings = New-Object System.Xml.XmlWriterSettings
	$settings.Indent = $true
	$settings.IndentChars = "  "
	$writer = [System.Xml.XmlTextWriter]::Create($ProjectFile, $settings)
	$xDoc.Save($writer)
	$writer.Close()
	
	exit $LASTEXITCODE
}
else {
	throw ("Unknown operation '" + $Operation + "'")
}