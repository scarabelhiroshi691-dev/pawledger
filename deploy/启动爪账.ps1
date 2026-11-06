$ErrorActionPreference = 'Stop'
$appDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$executable = Join-Path $appDirectory 'PetShop.Web.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw "找不到程序：$executable" }
$env:ASPNETCORE_URLS = 'http://0.0.0.0:5088'
$env:ASPNETCORE_ENVIRONMENT = 'Production'
try {
    if ((Invoke-WebRequest -Uri 'http://127.0.0.1:5088/health' -UseBasicParsing -TimeoutSec 1).StatusCode -eq 200) {
        Start-Process 'http://127.0.0.1:5088'
        return
    }
} catch { }
$process = Start-Process -FilePath $executable -WorkingDirectory $appDirectory -WindowStyle Hidden -PassThru
$ready = $false
for ($attempt = 0; $attempt -lt 40; $attempt++) {
    if ($process.HasExited) { throw "爪账启动失败，退出代码：$($process.ExitCode)" }
    try { if ((Invoke-WebRequest -Uri 'http://127.0.0.1:5088/health' -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $ready = $true; break } } catch { }
    Start-Sleep -Milliseconds 250
}
if (-not $ready) { throw '爪账未能在 10 秒内启动，请联系技术支持。' }
Start-Process 'http://127.0.0.1:5088'
