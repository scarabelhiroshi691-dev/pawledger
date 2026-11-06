$ErrorActionPreference = 'Stop'
$appDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$executable = (Join-Path $appDirectory 'PetShop.Web.exe').ToLowerInvariant()
$targets = Get-Process -Name 'PetShop.Web' -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.ToLowerInvariant() -eq $executable }
if (-not $targets) { Write-Host '爪账当前没有运行。'; return }
$targets | Stop-Process
Write-Host '爪账已安全关闭。' -ForegroundColor Green
