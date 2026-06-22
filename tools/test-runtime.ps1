$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet_home"
$env:APPDATA = Join-Path $root ".appdata"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $root ".nuget_packages"

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $env:APPDATA "NuGet") | Out-Null
New-Item -ItemType Directory -Force -Path $env:NUGET_PACKAGES | Out-Null

$nugetConfig = Join-Path $root "NuGet.Config"
Copy-Item -LiteralPath $nugetConfig -Destination (Join-Path $env:APPDATA "NuGet\NuGet.Config") -Force

$runtimeProject = Get-ChildItem -Path $root -Recurse -Filter "WenMingBlocks.Runtime.Authority.csproj" | Select-Object -First 1
$testProject = Get-ChildItem -Path $root -Recurse -Filter "WenMingBlocks.Runtime.Tests.csproj" | Select-Object -First 1

if ($null -eq $runtimeProject) {
    throw "Runtime project was not found."
}

if ($null -eq $testProject) {
    throw "Runtime test project was not found."
}

dotnet build $runtimeProject.FullName --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { throw "Runtime build failed." }
dotnet run --project $testProject.FullName --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { throw "Runtime tests failed." }
