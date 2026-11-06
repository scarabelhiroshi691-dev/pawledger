$ErrorActionPreference = 'Stop'
$ruleName = '爪账手机端（TCP 5088）'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw '请右键此脚本，选择“使用 PowerShell 以管理员身份运行”。'
}
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5088 -Profile Private | Out-Null
}
Write-Host '已允许同一私有网络中的手机访问爪账端口 5088。' -ForegroundColor Green
Write-Host '请确保当前 Wi-Fi 网络在 Windows 中设置为“专用网络”。'
