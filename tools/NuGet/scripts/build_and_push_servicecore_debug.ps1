# PowerShell script to build DynamoCore.sln in Debug, invoke BuildPackages.bat, and push the resulting nupkg to a local NuGet repo

param(
    [string]$SolutionPath = "..\..\..\src\DynamoCore.sln",
    [string]$NugetScriptDir = "..\",
    [string]$NuspecTemplate = "template-service-core",
    [string]$DebugBinPath = "..\..\..\bin\Publish_Linux\Debug",
    [string]$LocalNugetRepo = "C:\nuget_local"
)

# 0. Ensure dotnet-t4 is installed and generate AssemblySharedInfo.cs from the .tt file
Write-Host "Setting up dotnet-t4 and generating AssemblySharedInfo.cs..."
$workspaceRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..') | Select-Object -ExpandProperty Path
$ttFolder = Join-Path $workspaceRoot 'src\AssemblySharedInfoGenerator'
Push-Location $ttFolder
if (!(Test-Path ".config")) {
    dotnet new tool-manifest
}
dotnet tool install --local dotnet-t4 --version 2.3.1
# Generate AssemblySharedInfo.cs from the .tt file
$ttFile = Join-Path $ttFolder 'AssemblySharedInfo.tt'
$outFile = Join-Path $ttFolder 'AssemblySharedInfo.cs'
dotnet tool run t4 "$ttFile" --out="$outFile"
Pop-Location

# 1. Build DynamoCore.sln in Debug for Publish_Linux platform using dotnet, overriding DotNet to net8.0-browser
Write-Host "Building DynamoCore.sln in Debug mode for Publish_Linux platform using dotnet (DotNet=net8.0-browser)..."
$solutionPath = Join-Path $PSScriptRoot '..\..\..\src\DynamoCore.sln'
dotnet build $solutionPath --configuration Debug -p:Platform=Publish_Linux -p:DotNet=net8.0-browser
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed. Exiting."
    exit 1
}

# Ensure DynamoCore.dll exists in the expected output directory for version extraction
$dllPath = Join-Path $PSScriptRoot '..\..\..\bin\Publish_Linux\Debug\DynamoCore.dll'
if (!(Test-Path $dllPath)) {
    Write-Error "DynamoCore.dll not found at $dllPath. Packaging will fail."
    exit 1
}

# 2. Invoke BuildPackages.bat with correct parameters
Write-Host "Invoking BuildPackages.bat..."
# Convert DebugBinPath to absolute path
$absoluteDebugBinPath = Resolve-Path $DebugBinPath | Select-Object -ExpandProperty Path
Push-Location $NugetScriptDir
cmd /c ".\BuildPackages.bat $NuspecTemplate $absoluteDebugBinPath"
if ($LASTEXITCODE -ne 0) {
    Write-Error "BuildPackages.bat failed. Exiting."
    Pop-Location
    exit 1
}

# 3. Find the generated nupkg and push to local NuGet repo
# Make nupkg filtering more specific to the expected package name
$expectedNupkgPattern = "DynamoVisualProgramming.ServiceCoreRuntime-debug*.nupkg"
$nupkg = Get-ChildItem -Filter $expectedNupkgPattern | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $nupkg) {
    Write-Error "No expected .nupkg file found matching pattern '$expectedNupkgPattern'. Exiting."
    Pop-Location
    exit 1
}

# Use NuGet.exe from the repo (one directory up from scripts)
$nugetExePath = Join-Path $PSScriptRoot '..\NuGet.exe'
Write-Host "Pushing $($nupkg.Name) to local NuGet repo at $LocalNugetRepo using $nugetExePath..."
& $nugetExePath push $nupkg.FullName -Source $LocalNugetRepo
if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet push failed. Exiting."
    Pop-Location
    exit 1
}

Pop-Location
Write-Host "Done."
