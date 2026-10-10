$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'local-env.ps1')
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $repoPath
try {
    dotnet run --no-launch-profile --project backend/Bancada.Api -- --bootstrap --seed
    if ($LASTEXITCODE -ne 0) { throw 'Schema migration and fictional seed failed.' }
} finally { Pop-Location }
