$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'local-env.ps1')
Push-Location $repoPath
try {
    dotnet build backend/Bancada.slnx --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
    dotnet test backend/Bancada.Tests --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Backend tests failed.' }
    npm --prefix frontend run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    npm --prefix frontend test -- --watch=false
    if ($LASTEXITCODE -ne 0) { throw 'Frontend tests failed.' }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Diff whitespace check failed.' }
} finally { Pop-Location }
