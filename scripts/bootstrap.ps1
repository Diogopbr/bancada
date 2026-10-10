param([string]$PostgresBin = 'C:\Program Files\PostgreSQL\17\bin', [int]$Port = 55417)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
$localPath = Join-Path $repoPath '.local'
New-Item -ItemType Directory -Force -Path $localPath | Out-Null
. (Join-Path $PSScriptRoot 'local-env.ps1')
if (-not (Test-Path (Join-Path $env:DOTNET_ROOT 'dotnet.exe'))) { throw 'Install the .NET 10 SDK before running bootstrap.' }
if (-not (Test-Path (Join-Path $PostgresBin 'initdb.exe'))) { throw 'PostgreSQL 17 binaries are required.' }
$settingsPath = Join-Path $localPath 'settings.json'
$dataPath = Join-Path $localPath 'postgres-data'
if (-not (Test-Path $settingsPath)) {
    $password = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    @{ connectionString = "Host=127.0.0.1;Port=$Port;Database=bancada;Username=bancada;Password=$password;Include Error Detail=false"; port = $Port; postgresBin = $PostgresBin } | ConvertTo-Json | Set-Content -LiteralPath $settingsPath
}
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($settings.port -ne $Port) { throw 'Configured port differs from requested port.' }
if (-not (Test-Path (Join-Path $dataPath 'PG_VERSION'))) {
    $password = ($settings.connectionString -split 'Password=')[1].Split(';')[0]
    $passwordFile = Join-Path $localPath 'init-password.tmp'
    try {
        $password | Set-Content -LiteralPath $passwordFile -NoNewline
        & (Join-Path $PostgresBin 'initdb.exe') -D $dataPath -U bancada --auth=scram-sha-256 --encoding=UTF8 --locale=C --pwfile=$passwordFile
        if ($LASTEXITCODE -ne 0) { throw 'initdb failed.' }
    } finally { if (Test-Path $passwordFile) { Remove-Item -LiteralPath $passwordFile } }
}
& (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataPath status *> $null
if ($LASTEXITCODE -ne 0) {
    & (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataPath -l (Join-Path $localPath 'postgres.log') -o "-p $Port -h 127.0.0.1" -w start
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL startup failed.' }
}
$env:PGPASSWORD = ($settings.connectionString -split 'Password=')[1].Split(';')[0]
try {
    $exists = & (Join-Path $PostgresBin 'psql.exe') -h 127.0.0.1 -p $Port -U bancada -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'bancada'"
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL connection failed.' }
    if ($exists -ne '1') {
        & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p $Port -U bancada bancada
        if ($LASTEXITCODE -ne 0) { throw 'Database creation failed.' }
    }
} finally { Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue }
. (Join-Path $PSScriptRoot 'local-env.ps1')
Write-Output "Local PostgreSQL ready on 127.0.0.1:$Port. Existing services preserved."
