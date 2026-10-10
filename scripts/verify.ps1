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
    npm run test:e2e
    if ($LASTEXITCODE -ne 0) { throw 'End-to-end tests failed.' }
    dotnet publish backend/Bancada.Api -c Release -o .local/publish --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Combined artifact publish failed.' }
    New-Item -ItemType Directory -Force -Path .local/publish/wwwroot | Out-Null
    Copy-Item -Path 'frontend/dist/bancada-web/browser/*' -Destination '.local/publish/wwwroot' -Recurse -Force
    npm run test:demo
    if ($LASTEXITCODE -ne 0) { throw 'Private demo browser verification failed.' }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Diff whitespace check failed.' }
} finally { Pop-Location }
