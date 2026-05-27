param(
    [string]$ApiAppPoolName = "CafeOrders.API",
    [string]$WebUiAppPoolName = "CafeOrders.WebUI",
    [string]$ApiSiteName = "CafeOrders.API",
    [string]$WebUiSiteName = "CafeOrders.WebUI",
    [string]$WebUiUrl = "http://192.168.2.11:5002/",
    [string]$LogPath = "C:\Scripts\CafeOrders.WatchDog.log",
    [string]$BrowserPath = "",
    [bool]$LaunchThroughExplorerShell = $true,
    [int]$HealthTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"
$ChromeTitlePatterns = @("LAN Cafe Ops", "CafeOrders")
$TargetUrlToken = ([Uri]$WebUiUrl).Authority

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )

    $logDirectory = Split-Path -Parent $LogPath
    if (-not [string]::IsNullOrWhiteSpace($logDirectory)) {
        New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    }

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $LogPath -Value "[$timestamp][$Level] $Message"
}

function Import-IisModule {
    try {
        Import-Module WebAdministration -ErrorAction Stop
        return $true
    }
    catch {
        Write-Log "WebAdministration module could not be loaded. $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Ensure-AppPoolStarted {
    param([string]$Name)

    $path = "IIS:\AppPools\$Name"
    if (-not (Test-Path $path)) {
        Write-Log "AppPool not found: $Name" "ERROR"
        return $false
    }

    $state = (Get-WebAppPoolState -Name $Name).Value
    if ($state -ne "Started") {
        Write-Log "Starting AppPool: $Name. Current state: $state"
        Start-WebAppPool -Name $Name
        Start-Sleep -Seconds 2
        $state = (Get-WebAppPoolState -Name $Name).Value
    }

    if ($state -eq "Started") {
        Write-Log "AppPool OK: $Name"
        return $true
    }

    Write-Log "AppPool failed to start: $Name. Current state: $state" "ERROR"
    return $false
}

function Ensure-SiteStarted {
    param([string]$Name)

    $site = Get-Website -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $site) {
        Write-Log "IIS Site not found: $Name" "ERROR"
        return $false
    }

    if ($site.State -ne "Started") {
        Write-Log "Starting IIS Site: $Name. Current state: $($site.State)"
        Start-Website -Name $Name
        Start-Sleep -Seconds 2
        $site = Get-Website -Name $Name -ErrorAction SilentlyContinue
    }

    if ($null -ne $site -and $site.State -eq "Started") {
        Write-Log "IIS Site OK: $Name"
        return $true
    }

    Write-Log "IIS Site failed to start: $Name. Current state: $($site.State)" "ERROR"
    return $false
}

function Test-WebUiHealth {
    try {
        $response = Invoke-WebRequest -Uri $WebUiUrl -UseBasicParsing -TimeoutSec $HealthTimeoutSeconds -MaximumRedirection 5
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
            Write-Log "WebUI health OK: $WebUiUrl HTTP $($response.StatusCode)"
            return $true
        }

        Write-Log "WebUI health failed: $WebUiUrl HTTP $($response.StatusCode)" "ERROR"
        return $false
    }
    catch {
        Write-Log "WebUI health failed: $WebUiUrl. $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Get-ChromePath {
    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LocalAppData\Google\Chrome\Application\chrome.exe"
    )

    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    return "chrome.exe"
}

function Test-ChromeCommandLineHasCafeOrders {
    try {
        $chromeProcesses = Get-CimInstance Win32_Process -Filter "name = 'chrome.exe'" -ErrorAction Stop
        foreach ($process in $chromeProcesses) {
            if ($process.CommandLine -and $process.CommandLine.Contains($TargetUrlToken)) {
                Write-Log "CafeOrders page detected by Chrome command line. PID: $($process.ProcessId)"
                return $true
            }
        }
    }
    catch {
        Write-Log "Chrome command line check failed. $($_.Exception.Message)" "WARN"
    }

    return $false
}

function Test-ChromeWindowTitleHasCafeOrders {
    $chromeProcesses = Get-Process chrome -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 }
    foreach ($process in $chromeProcesses) {
        foreach ($pattern in $ChromeTitlePatterns) {
            if ($process.MainWindowTitle -and $process.MainWindowTitle.Contains($pattern)) {
                Write-Log "CafeOrders page detected by Chrome window title. PID: $($process.Id)"
                return $true
            }
        }
    }

    return $false
}

function Test-ChromeTabTitleHasCafeOrders {
    try {
        Add-Type -AssemblyName UIAutomationClient
        Add-Type -AssemblyName UIAutomationTypes
    }
    catch {
        Write-Log "UIAutomation assemblies could not be loaded. $($_.Exception.Message)" "WARN"
        return $false
    }

    $chromeProcesses = Get-Process chrome -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 }
    foreach ($process in $chromeProcesses) {
        try {
            $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
            if ($null -eq $root) {
                continue
            }

            $condition = New-Object -TypeName System.Windows.Automation.PropertyCondition -ArgumentList (
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::TabItem)
            $tabs = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
            foreach ($tab in $tabs) {
                $tabName = $tab.Current.Name
                foreach ($pattern in $ChromeTitlePatterns) {
                    if ($tabName -and $tabName.Contains($pattern)) {
                        Write-Log "CafeOrders page detected by Chrome tab title. PID: $($process.Id). Tab: $tabName"
                        return $true
                    }
                }
            }
        }
        catch {
            Write-Log "Chrome tab inspection failed for PID $($process.Id). $($_.Exception.Message)" "WARN"
        }
    }

    return $false
}

function Test-CafeOrdersAlreadyOpen {
    if (Test-ChromeCommandLineHasCafeOrders) {
        return $true
    }

    if (Test-ChromeWindowTitleHasCafeOrders) {
        return $true
    }

    if (Test-ChromeTabTitleHasCafeOrders) {
        return $true
    }

    return $false
}

function Open-CafeOrders {
    if ($LaunchThroughExplorerShell) {
        Write-Log "Opening CafeOrders through Windows shell: $WebUiUrl"
        Start-Process -FilePath "explorer.exe" -ArgumentList $WebUiUrl
        return
    }

    $resolvedBrowserPath = if ([string]::IsNullOrWhiteSpace($BrowserPath)) { Get-ChromePath } else { $BrowserPath }
    Write-Log "Opening CafeOrders in browser: $resolvedBrowserPath $WebUiUrl"
    Start-Process -FilePath $resolvedBrowserPath -ArgumentList $WebUiUrl
}

Write-Log "CafeOrders WatchDog started."

if (-not (Import-IisModule)) {
    exit 1
}

$apiPoolOk = Ensure-AppPoolStarted -Name $ApiAppPoolName
$webPoolOk = Ensure-AppPoolStarted -Name $WebUiAppPoolName
$apiSiteOk = Ensure-SiteStarted -Name $ApiSiteName
$webSiteOk = Ensure-SiteStarted -Name $WebUiSiteName

if (-not ($apiPoolOk -and $webPoolOk -and $apiSiteOk -and $webSiteOk)) {
    Write-Log "IIS recovery did not complete. Browser launch skipped." "ERROR"
    exit 2
}

if (-not (Test-WebUiHealth)) {
    Write-Log "WebUI is not healthy. Browser launch skipped." "ERROR"
    exit 3
}

if (Test-CafeOrdersAlreadyOpen) {
    Write-Log "CafeOrders is already open. Browser launch skipped."
    exit 0
}

Open-CafeOrders
Write-Log "CafeOrders WatchDog completed."
