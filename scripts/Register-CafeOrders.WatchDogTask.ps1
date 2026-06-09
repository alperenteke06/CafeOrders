param(
    [string]$TaskName = "CafeOrders WatchDog",
    [string]$ScriptPath = "C:\Scripts\CafeOrders.WatchDog.ps1",
    [string]$HiddenRunnerPath = "C:\Scripts\Run-CafeOrders.WatchDogHidden.vbs",
    [string]$ApiHealthUrl = "http://192.168.2.11:5001/api/v1/settings/app",
    [string]$WebUiUrl = "http://192.168.2.11:5002/",
    [string]$ApiAppPoolName = "CafeOrders.API",
    [string]$WebUiAppPoolName = "CafeOrders.WebUI",
    [string]$ApiSiteName = "CafeOrders.API",
    [string]$WebUiSiteName = "CafeOrders.WebUI",
    [string]$LogPath = "C:\Scripts\CafeOrders.WatchDog.log",
    [string]$AdminAudioAgentPath = "C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ScriptPath)) {
    throw "WatchDog script not found: $ScriptPath"
}

if (-not (Test-Path $HiddenRunnerPath)) {
    throw "Hidden WatchDog runner not found: $HiddenRunnerPath"
}

$scriptDirectory = Split-Path -Parent $HiddenRunnerPath
$arguments = @(
    "`"$HiddenRunnerPath`"",
    "`"$ScriptPath`"",
    "`"$ApiHealthUrl`"",
    "`"$WebUiUrl`"",
    "`"$ApiAppPoolName`"",
    "`"$WebUiAppPoolName`"",
    "`"$ApiSiteName`"",
    "`"$WebUiSiteName`"",
    "`"$LogPath`"",
    "`"$AdminAudioAgentPath`""
) -join " "

$action = New-ScheduledTaskAction `
    -Execute "wscript.exe" `
    -Argument $arguments `
    -WorkingDirectory $scriptDirectory

$trigger = New-ScheduledTaskTrigger `
    -Once `
    -At (Get-Date).AddMinutes(1) `
    -RepetitionInterval (New-TimeSpan -Minutes 1) `
    -RepetitionDuration (New-TimeSpan -Days 3650)

$principal = New-ScheduledTaskPrincipal `
    -UserId ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name) `
    -LogonType Interactive `
    -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 5)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Force | Out-Null

Write-Output "Task registered: $TaskName"
