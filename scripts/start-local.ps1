param(
    [int]$ApiPort = 5077,
    [int]$WebPort = 5173,
    [string]$Ticker = "MSFT",
    [string]$Range = "1Y"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$webRoot = Join-Path $repoRoot "src\EquityLens.Web"
$logRoot = Join-Path $repoRoot ".logs"
$apiUrl = "http://127.0.0.1:$ApiPort"
$webUrl = "http://127.0.0.1:$WebPort"

New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

function Stop-Listeners {
    param([int[]]$Ports)

    foreach ($port in $Ports) {
        Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object {
                Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue
            }
    }
}

function Wait-ForHttp {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 40
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 3

            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "Timed out waiting for $Url"
}

Stop-Listeners -Ports @($ApiPort, $WebPort)

Push-Location $webRoot
try {
    if (-not (Test-Path "node_modules")) {
        npm.cmd install
    }

    npm.cmd run build
}
finally {
    Pop-Location
}

$secUserAgent = if ($env:ApiProviderOptions__SecUserAgent) {
    $env:ApiProviderOptions__SecUserAgent
}
else {
    "EquityLens Local Run contact@example.com"
}

$apiLog = Join-Path $logRoot "api.log"
$webLog = Join-Path $logRoot "web.log"

$apiCommand = @"
cd "$repoRoot"
`$env:ASPNETCORE_ENVIRONMENT = "Development"
`$env:ApiProviderOptions__SecUserAgent = "$secUserAgent"
dotnet run --project ".\src\EquityLens.Api" --urls "$apiUrl" *> "$apiLog"
"@

$webCommand = @"
cd "$webRoot"
npm.cmd run preview -- --host 127.0.0.1 --port $WebPort *> "$webLog"
"@

$apiEncodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($apiCommand))
$webEncodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($webCommand))

Start-Process -FilePath "powershell" -WindowStyle Hidden -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", $apiEncodedCommand)
Start-Process -FilePath "powershell" -WindowStyle Hidden -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", $webEncodedCommand)

Wait-ForHttp "$apiUrl/api/stocks/supported"
Wait-ForHttp $webUrl

$dashboardUrl = "$webUrl/?ticker=$Ticker&range=$Range"
Start-Process $dashboardUrl

Write-Host "API: $apiUrl"
Write-Host "Web: $dashboardUrl"
Write-Host "Logs: $logRoot"
