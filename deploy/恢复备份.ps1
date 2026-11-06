param([Parameter(Mandatory=$true)][string]$BackupFile)
$ErrorActionPreference = 'Stop'
$appDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$executable = Join-Path $appDirectory 'PetShop.Web.exe'
$resolvedBackup = (Resolve-Path -LiteralPath $BackupFile).Path
$running = Get-Process -Name 'PetShop.Web' -ErrorAction SilentlyContinue
if ($running) { throw '请先关闭爪账程序，再执行恢复。可在任务管理器结束 PetShop.Web。' }
& $executable --restore $resolvedBackup
if ($LASTEXITCODE -ne 0) { throw "恢复失败，程序退出代码：$LASTEXITCODE" }
Write-Host '恢复成功。现在可以重新启动爪账。' -ForegroundColor Green
