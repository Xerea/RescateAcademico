$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "RescateAcademico\RescateAcademico.csproj"
$dotnet8Exe = Join-Path $env:USERPROFILE ".dotnet8\dotnet.exe"
$dotnetExe = if (Test-Path $dotnet8Exe) { $dotnet8Exe } else { Join-Path $env:USERPROFILE ".dotnet\dotnet.exe" }

if (-not (Test-Path $dotnetExe)) {
    throw "No se encontró dotnet en $dotnetExe"
}

& $dotnetExe restore $projectPath
& $dotnetExe run --project $projectPath
