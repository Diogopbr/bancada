$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
$toolsPath = Join-Path (Split-Path $repoPath -Parent) '.tools'
$dotnetPath = Join-Path $toolsPath 'dotnet'
if (Test-Path (Join-Path $dotnetPath 'dotnet.exe')) {
    $env:DOTNET_ROOT = $dotnetPath
    $env:PATH = "$dotnetPath;$env:PATH"
}
$settingsPath = Join-Path $repoPath '.local/settings.json'
if (Test-Path $settingsPath) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $env:ConnectionStrings__Bancada = $settings.connectionString
    $env:BANCADA_TEST_CONNECTION = $settings.connectionString
}
