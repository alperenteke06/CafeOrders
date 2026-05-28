param(
    [string]$TaskName = "CafeOrders WatchDog",
    [string]$ScriptPath = "C:\Scripts\CafeOrders.WatchDog.ps1",
    [string]$HiddenRunnerPath = "C:\Scripts\Run-CafeOrders.WatchDogHidden.vbs",
    [string]$WebUiUrl = "http://192.168.1.104:5002/",
    [string]$ApiAppPoolName = "CafeOrders.API",
    [string]$WebUiAppPoolName = "CafeOrders.WebUI",
    [string]$ApiSiteName = "CafeOrders.API",
    [string]$WebUiSiteName = "CafeOrders.WebUI",
    [string]$LogPath = "C:\Scripts\CafeOrders.WatchDog.log"
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
    "`"$WebUiUrl`"",
    "`"$ApiAppPoolName`"",
    "`"$WebUiAppPoolName`"",
    "`"$ApiSiteName`"",
    "`"$WebUiSiteName`"",
    "`"$LogPath`""
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
