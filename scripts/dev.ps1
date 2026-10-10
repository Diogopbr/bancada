param([switch]$Stop)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'local-env.ps1')
$statePath = Join-Path $repoPath '.local/processes.json'
if ($Stop) {
    if (Test-Path $statePath) {
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        foreach ($processId in @($state.api, $state.frontend)) {
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($process) { Stop-Process -Id $processId }
        }
        Remove-Item -LiteralPath $statePath
    }
    return
}
if (Test-Path $statePath) { throw 'Process state exists. Check running processes before starting another instance.' }
$env:ASPNETCORE_URLS = 'http://localhost:5080'
$api = Start-Process -FilePath (Join-Path $env:DOTNET_ROOT 'dotnet.exe') -ArgumentList @('run','--no-launch-profile','--project','backend/Bancada.Api') -WorkingDirectory $repoPath -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $repoPath '.local/api.log') -RedirectStandardError (Join-Path $repoPath '.local/api-error.log')
$frontend = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c','npm run start -- --port 4200 --proxy-config proxy.conf.json') -WorkingDirectory (Join-Path $repoPath 'frontend') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $repoPath '.local/frontend.log') -RedirectStandardError (Join-Path $repoPath '.local/frontend-error.log')
@{ api = $api.Id; frontend = $frontend.Id } | ConvertTo-Json | Set-Content -LiteralPath $statePath
Write-Output 'API: http://localhost:5080/health/ready; interface: http://localhost:4200'
