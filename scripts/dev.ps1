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
            if ($process) {
                $metadata = Get-CimInstance Win32_Process -Filter "ProcessId = $processId"
                if ($metadata.CommandLine -notmatch 'backend/Bancada.Api|npm run start -- --port 4200 --proxy-config proxy.conf.json') {
                    throw "PID $processId no longer belongs to this development session; refusing to stop it."
                }
                & taskkill.exe /PID $processId /T /F | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "Could not stop development process $processId." }
            }
        }
        Remove-Item -LiteralPath $statePath
    }
    return
}
if (Test-Path $statePath) { throw 'Process state exists. Check running processes before starting another instance.' }
$env:ASPNETCORE_URLS = 'http://localhost:5080'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$api = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('run','--no-launch-profile','--project','backend/Bancada.Api') -WorkingDirectory $repoPath -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $repoPath '.local/api.log') -RedirectStandardError (Join-Path $repoPath '.local/api-error.log')
$frontend = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c','npm run start -- --port 4200 --proxy-config proxy.conf.json') -WorkingDirectory (Join-Path $repoPath 'frontend') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $repoPath '.local/frontend.log') -RedirectStandardError (Join-Path $repoPath '.local/frontend-error.log')
@{ api = $api.Id; frontend = $frontend.Id } | ConvertTo-Json | Set-Content -LiteralPath $statePath
Write-Output 'API: http://localhost:5080/health/ready; interface: http://localhost:4200'
