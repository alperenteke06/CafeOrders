[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string]$PackageUrl = "https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip",
    [string]$PackagePath,
    [string]$ServerIp,
    [int]$ApiPort = 5001,
    [int]$WebUiPort = 5002,
    [string]$SqlInstanceName,
    [string]$SqlUser,
    [string]$SqlPassword,
    [string]$IisRootPath = "C:\inetpub\wwwroot",
    [string]$ApiSiteName = "CafeOrders.API",
    [string]$WebUiSiteName = "CafeOrders.WebUI",
    [string]$ApiAppPoolName = "CafeOrders.API",
    [string]$WebUiAppPoolName = "CafeOrders.WebUI",
    [string]$AdminAudioAgentPath = "C:\AdminAudioAgent",
    [string]$ServerNotifierPath = "C:\ServerNotifier",
    [string]$ScriptsPath = "C:\Scripts",
    [bool]$OpenFirewall = $true,
    [bool]$RegisterTask = $true,
    [bool]$TriggerTask = $true,
    [bool]$PreserveUploads = $true,
    [bool]$InstallHostingBundle = $true,
    [string]$HostingBundleUrl = "https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe"
)

$ErrorActionPreference = "Stop"

$script:InstallerConfig = @{}
$script:SetupBoundParameters = @{} + $PSBoundParameters

function Write-Step {
    param([string]$Message)
    Write-Host "[CafeOrders Setup] $Message"
}

function Write-SetupWarning {
    param([string]$Message)
    Write-Warning "[CafeOrders Setup] $Message"
}

function Import-Config {
    if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
        return
    }

    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        throw "Config file not found: $ConfigPath"
    }

    $json = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    foreach ($property in $json.PSObject.Properties) {
        $script:InstallerConfig[$property.Name] = $property.Value
    }
}

function Get-Option {
    param(
        [string]$Name,
        [object]$CurrentValue,
        [object]$Fallback
    )

    if ($script:SetupBoundParameters.ContainsKey($Name)) {
        return $CurrentValue
    }

    if ($script:InstallerConfig.ContainsKey($Name)) {
        return $script:InstallerConfig[$Name]
    }

    return $Fallback
}

function Require-Value {
    param(
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-IsWindowsClientOs {
    try {
        $os = Get-CimInstance -ClassName Win32_OperatingSystem -ErrorAction Stop
        return [int]$os.ProductType -eq 1
    }
    catch {
        return $false
    }
}

function Test-WindowsFeatureEnabled {
    param(
        [string]$ServerFeatureName,
        [string]$ClientFeatureName
    )

    try {
        $isClientOs = Test-IsWindowsClientOs
        $serverFeature = Get-Command Get-WindowsFeature -ErrorAction SilentlyContinue
        if (-not $isClientOs -and $serverFeature) {
            $feature = Get-WindowsFeature -Name $ServerFeatureName -ErrorAction SilentlyContinue
            return $feature -and $feature.Installed
        }

        $clientFeature = Get-Command Get-WindowsOptionalFeature -ErrorAction SilentlyContinue
        if ($clientFeature) {
            $feature = Get-WindowsOptionalFeature -Online -FeatureName $ClientFeatureName -ErrorAction SilentlyContinue
            return $feature -and $feature.State -eq "Enabled"
        }
    }
    catch {
        Write-SetupWarning "Feature check failed for $ServerFeatureName/$ClientFeatureName. $($_.Exception.Message)"
    }

    return $null
}

function Enable-WindowsFeatureIfNeeded {
    param(
        [string]$ServerFeatureName,
        [string]$ClientFeatureName,
        [string]$DisplayName,
        [bool]$Required = $true
    )

    $enabled = Test-WindowsFeatureEnabled -ServerFeatureName $ServerFeatureName -ClientFeatureName $ClientFeatureName
    if ($enabled -eq $true) {
        Write-Step "$DisplayName already enabled."
        return
    }

    try {
        Write-Step "Installing/enabling $DisplayName"
        $isClientOs = Test-IsWindowsClientOs
        $serverInstaller = Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue
        if (-not $isClientOs -and $serverInstaller) {
            Install-WindowsFeature -Name $ServerFeatureName -IncludeManagementTools | Out-Null
        }
        else {
            Enable-WindowsOptionalFeature -Online -FeatureName $ClientFeatureName -All -NoRestart | Out-Null
        }

        $enabled = Test-WindowsFeatureEnabled -ServerFeatureName $ServerFeatureName -ClientFeatureName $ClientFeatureName
        if ($enabled -eq $true) {
            Write-Step "$DisplayName enabled."
            return
        }

        $message = "$DisplayName feature state could not be verified after installation."
        if ($Required) {
            throw $message
        }

        Write-SetupWarning $message
    }
    catch {
        $message = "$DisplayName could not be installed/enabled. $($_.Exception.Message)"
        if ($Required) {
            throw $message
        }

        Write-SetupWarning $message
    }
}

function Test-HostingBundle {
    $paths = @(
        "${env:ProgramFiles}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll",
        "${env:ProgramFiles(x86)}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    )

    return $paths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

function Install-HostingBundleIfNeeded {
    if (Test-HostingBundle) {
        Write-Step ".NET Hosting Bundle / ASP.NET Core Module V2 already installed."
        return
    }

    if (-not $InstallHostingBundle) {
        Write-SetupWarning ".NET Hosting Bundle / ASP.NET Core Module V2 was not detected. Automatic installation is disabled."
        return
    }

    $downloadRoot = Join-Path $env:TEMP ("CafeOrdersHostingBundle_" + [Guid]::NewGuid().ToString("N"))
    $installerPath = Join-Path $downloadRoot "dotnet-hosting-win.exe"

    try {
        New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null
        Write-Step "Downloading .NET Hosting Bundle from $HostingBundleUrl"
        Invoke-WebRequest -Uri $HostingBundleUrl -OutFile $installerPath -UseBasicParsing

        Write-Step "Installing .NET Hosting Bundle silently"
        $process = Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru
        if ($process.ExitCode -notin @(0, 3010)) {
            throw ".NET Hosting Bundle installer exited with code $($process.ExitCode)."
        }

        if (Test-HostingBundle) {
            Write-Step ".NET Hosting Bundle installed."
            return
        }

        Write-SetupWarning ".NET Hosting Bundle installer completed but ASP.NET Core Module V2 could not be verified. A server restart may be required."
    }
    finally {
        if (Test-Path -LiteralPath $downloadRoot) {
            Remove-Item -LiteralPath $downloadRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Assert-Prerequisites {
    if (-not (Test-IsAdmin)) {
        throw "Setup must be run as Administrator. IIS, firewall and Task Scheduler operations require elevated permissions."
    }

    Enable-WindowsFeatureIfNeeded -ServerFeatureName "Web-Server" -ClientFeatureName "IIS-WebServerRole" -DisplayName "IIS Web Server"
    Enable-WindowsFeatureIfNeeded -ServerFeatureName "Web-Scripting-Tools" -ClientFeatureName "IIS-ManagementScriptingTools" -DisplayName "IIS Management Scripts and Tools"
    Enable-WindowsFeatureIfNeeded -ServerFeatureName "Web-WebSockets" -ClientFeatureName "IIS-WebSockets" -DisplayName "IIS WebSocket Protocol" -Required $false

    Install-HostingBundleIfNeeded

    try {
        Import-Module WebAdministration -ErrorAction Stop
    }
    catch {
        throw "WebAdministration module could not be loaded. IIS Management Scripts and Tools must be installed. $($_.Exception.Message)"
    }
}

function Resolve-PackageRoot {
    $downloadRoot = Join-Path $env:TEMP ("CafeOrdersSetup_" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null

    if (-not [string]::IsNullOrWhiteSpace($PackagePath)) {
        if (-not (Test-Path -LiteralPath $PackagePath)) {
            throw "Package path not found: $PackagePath"
        }

        $item = Get-Item -LiteralPath $PackagePath
        if ($item.PSIsContainer) {
            return $item.FullName
        }

        $zipPath = $item.FullName
    }
    else {
        $zipPath = Join-Path $downloadRoot "CafeOrders-Production.zip"
        Write-Step "Downloading package from $PackageUrl"
        Invoke-WebRequest -Uri $PackageUrl -OutFile $zipPath -UseBasicParsing
    }

    $extractPath = Join-Path $downloadRoot "package"
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force

    $candidate = Get-ChildItem -LiteralPath $extractPath -Directory -Recurse |
        Where-Object {
            (Test-Path -LiteralPath (Join-Path $_.FullName "publishes")) -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "scripts"))
        } |
        Select-Object -First 1

    if (-not $candidate) {
        throw "Package does not contain publishes and scripts folders."
    }

    return $candidate.FullName
}

function Assert-Package {
    param([string]$PackageRoot)

    $requiredPaths = @(
        "publishes\API",
        "publishes\WebUI",
        "publishes\DesktopApp",
        "publishes\AdminAudioAgent",
        "publishes\ServerNotifier",
        "scripts\CafeOrders.WatchDog.ps1",
        "scripts\Register-CafeOrders.WatchDogTask.ps1",
        "scripts\Run-CafeOrders.WatchDogHidden.vbs"
    )

    foreach ($relativePath in $requiredPaths) {
        $fullPath = Join-Path $PackageRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "Package is missing required path: $relativePath"
        }
    }
}

function Ensure-Directory {
    param([string]$Path)
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Clear-Directory {
    param([string]$Path)

    Ensure-Directory $Path
    Get-ChildItem -LiteralPath $Path -Force | Remove-Item -Recurse -Force
}

function Clear-WebUiDirectoryPreservingUploads {
    param([string]$Path)

    Ensure-Directory $Path
    $wwwroot = Join-Path $Path "wwwroot"
    $uploads = Join-Path $wwwroot "uploads"

    foreach ($item in Get-ChildItem -LiteralPath $Path -Force) {
        if ($item.FullName -ieq $wwwroot) {
            Ensure-Directory $wwwroot
            foreach ($webItem in Get-ChildItem -LiteralPath $wwwroot -Force) {
                if ($PreserveUploads -and $webItem.FullName -ieq $uploads) {
                    continue
                }

                Remove-Item -LiteralPath $webItem.FullName -Recurse -Force
            }

            continue
        }

        Remove-Item -LiteralPath $item.FullName -Recurse -Force
    }
}

function Copy-DirectoryContents {
    param(
        [string]$Source,
        [string]$Destination
    )

    Ensure-Directory $Destination
    Copy-Item -Path (Join-Path $Source "*") -Destination $Destination -Recurse -Force
}

function ConvertTo-PrettyJson {
    param([object]$Value)
    return $Value | ConvertTo-Json -Depth 12
}

function Write-JsonFile {
    param(
        [string]$Path,
        [object]$Value
    )

    ConvertTo-PrettyJson $Value | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Protect-ConfigFile {
    param(
        [string]$Path,
        [string]$AppPoolName
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    try {
        & icacls $Path /inheritance:r /grant:r "*S-1-5-32-544:F" "*S-1-5-18:F" "IIS AppPool\${AppPoolName}:R" | Out-Null
    }
    catch {
        Write-SetupWarning "Could not restrict ACL for $Path. $($_.Exception.Message)"
    }
}

function Write-AppSettings {
    param(
        [string]$ApiPath,
        [string]$WebUiPath,
        [string]$AgentPath,
        [string]$NotifierPath
    )

    $connectionString = "Server=$SqlInstanceName;Database=CafeOrders;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
    $apiBaseUrl = "http://$ServerIp`:$ApiPort"
    $webUiBaseUrl = "http://$ServerIp`:$WebUiPort"
    $sharedWebRootPath = "\\$ServerIp\inetpub\wwwroot\WebUI\wwwroot"

    Write-JsonFile -Path (Join-Path $ApiPath "appsettings.json") -Value ([ordered]@{
        Urls = "http://0.0.0.0:$ApiPort"
        ConnectionStrings = [ordered]@{
            CafeOrders = $connectionString
        }
        Jwt = [ordered]@{
            Issuer = "CafeOrders.API"
            Audience = "CafeOrders.DesktopApps"
            Key = "lan-cafe-super-secret-key-change-this-in-production"
            ExpiryMinutes = 720
        }
        Branding = [ordered]@{
            AppDeveloperName = "Alperen TEKE"
            AppDeveloperPhone = "0 (541) 688 88 06"
        }
        Logging = [ordered]@{
            FilePath = "CafeOrders.API.log"
            Centralized = [ordered]@{ Enabled = $true }
            LogLevel = [ordered]@{
                Default = "Information"
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        AllowedHosts = "*"
    })

    Write-JsonFile -Path (Join-Path $WebUiPath "appsettings.json") -Value ([ordered]@{
        Urls = "http://0.0.0.0:$WebUiPort"
        ConnectionStrings = [ordered]@{
            CafeOrders = $connectionString
        }
        Jwt = [ordered]@{
            Issuer = "CafeOrders.API"
            Audience = "CafeOrders.DesktopApps"
            Key = "lan-cafe-super-secret-key-change-this-in-production"
            ExpiryMinutes = 720
        }
        Branding = [ordered]@{
            AppDeveloperName = "Alperen TEKE"
            AppDeveloperPhone = "0 (541) 688 88 06"
        }
        SessionSettings = [ordered]@{
            AdminCookieDays = 3650
            SlidingExpiration = $true
            DataProtectionApplicationName = "CafeOrders.WebUI"
            DataProtectionKeysPath = "C:\ProgramData\CafeOrders\WebUI\DataProtectionKeys"
        }
        ApiBaseUrl = $apiBaseUrl
        Logging = [ordered]@{
            FilePath = "CafeOrders.WebUI.log"
            Centralized = [ordered]@{ Enabled = $true }
            LogLevel = [ordered]@{
                Default = "Information"
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        AllowedHosts = "*"
    })

    Write-JsonFile -Path (Join-Path $AgentPath "appsettings.json") -Value ([ordered]@{
        Agent = [ordered]@{
            ApiBaseUrl = "$apiBaseUrl/"
            HubUrl = "$apiBaseUrl/hubs/cafe"
            WebUiBaseUrl = "$webUiBaseUrl/"
            SharedWebRootPath = $sharedWebRootPath
            FallbackSoundPath = $null
            CacheDirectory = "cache"
            LogPath = "AdminAudioAgent.log"
            FallbackDelayMilliseconds = 0
            PollIntervalMilliseconds = 2000
            ApiStartupRetryCount = 180
            ApiStartupRetryDelayMilliseconds = 2000
            MaxPlaybackSeconds = 12
            Volume = 90
            UseSystemBeepFallback = $false
        }
    })

    Write-JsonFile -Path (Join-Path $NotifierPath "appsettings.json") -Value ([ordered]@{
        Notifier = [ordered]@{
            ApiBaseUrl = "$apiBaseUrl/"
            HubUrl = "$apiBaseUrl/hubs/cafe"
            OrdersUrl = "$webUiBaseUrl/?section=orders"
            PollIntervalSeconds = 5
            StartupRetryCount = 90
            StartupRetryDelaySeconds = 2
            LogPath = "ServerNotifier.log"
        }
    })

    Protect-ConfigFile -Path (Join-Path $ApiPath "appsettings.json") -AppPoolName $ApiAppPoolName
    Protect-ConfigFile -Path (Join-Path $WebUiPath "appsettings.json") -AppPoolName $WebUiAppPoolName
}

function Ensure-AppPool {
    param([string]$Name)

    if (-not (Test-Path "IIS:\AppPools\$Name")) {
        Write-Step "Creating AppPool $Name"
        New-WebAppPool -Name $Name | Out-Null
    }

    Set-ItemProperty "IIS:\AppPools\$Name" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$Name" -Name startMode -Value "AlwaysRunning"
}

function Ensure-Website {
    param(
        [string]$Name,
        [string]$PhysicalPath,
        [int]$Port,
        [string]$AppPoolName
    )

    if (-not (Test-Path "IIS:\Sites\$Name")) {
        Write-Step "Creating IIS site $Name on port $Port"
        New-Website -Name $Name -PhysicalPath $PhysicalPath -Port $Port -ApplicationPool $AppPoolName | Out-Null
    }
    else {
        Write-Step "Updating IIS site $Name"
        Set-ItemProperty "IIS:\Sites\$Name" -Name physicalPath -Value $PhysicalPath
        Set-ItemProperty "IIS:\Sites\$Name" -Name applicationPool -Value $AppPoolName
    }

    $binding = Get-WebBinding -Name $Name -Protocol "http" -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -eq "*:${Port}:" -or $_.bindingInformation -like "*:${Port}:*" } |
        Select-Object -First 1

    if (-not $binding) {
        New-WebBinding -Name $Name -Protocol "http" -Port $Port -IPAddress "*" | Out-Null
    }

    Start-WebAppPool -Name $AppPoolName
    Start-Website -Name $Name
}

function Ensure-FirewallRule {
    param(
        [string]$Name,
        [int]$Port
    )

    if (-not $OpenFirewall) {
        return
    }

    $existing = Get-NetFirewallRule -DisplayName $Name -ErrorAction SilentlyContinue
    if ($existing) {
        Set-NetFirewallRule -DisplayName $Name -Enabled True -Direction Inbound -Action Allow | Out-Null
        Set-NetFirewallPortFilter -AssociatedNetFirewallRule $existing -Protocol TCP -LocalPort $Port | Out-Null
        return
    }

    Write-Step "Creating firewall rule $Name for TCP $Port"
    New-NetFirewallRule -DisplayName $Name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port | Out-Null
}

function Grant-UploadPermissions {
    param([string]$WebUiPath)

    $uploadsPath = Join-Path $WebUiPath "wwwroot\uploads"
    Ensure-Directory $uploadsPath

    try {
        & icacls $uploadsPath /grant "IIS AppPool\$WebUiAppPoolName`:(OI)(CI)M" /T | Out-Null
    }
    catch {
        Write-SetupWarning "Could not grant upload folder permissions. $($_.Exception.Message)"
    }
}

function Register-WatchDogTask {
    $registerScript = Join-Path $ScriptsPath "Register-CafeOrders.WatchDogTask.ps1"
    if (-not $RegisterTask) {
        return
    }

    if (-not (Test-Path -LiteralPath $registerScript)) {
        throw "WatchDog registration script not found: $registerScript"
    }

    Write-Step "Registering WatchDog scheduled task"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $registerScript `
        -ScriptPath (Join-Path $ScriptsPath "CafeOrders.WatchDog.ps1") `
        -HiddenRunnerPath (Join-Path $ScriptsPath "Run-CafeOrders.WatchDogHidden.vbs") `
        -ApiHealthUrl "http://$ServerIp`:$ApiPort/api/v1/settings/app" `
        -WebUiUrl "http://$ServerIp`:$WebUiPort/" `
        -ApiAppPoolName $ApiAppPoolName `
        -WebUiAppPoolName $WebUiAppPoolName `
        -ApiSiteName $ApiSiteName `
        -WebUiSiteName $WebUiSiteName `
        -LogPath (Join-Path $ScriptsPath "CafeOrders.WatchDog.log") `
        -AdminAudioAgentPath (Join-Path $AdminAudioAgentPath "CafeOrders.AdminAudioAgent.exe") `
        -ServerNotifierPath (Join-Path $ServerNotifierPath "CafeOrders.ServerNotifier.exe")

    if ($TriggerTask) {
        Write-Step "Triggering WatchDog task"
        Start-ScheduledTask -TaskName "CafeOrders WatchDog"
    }
}

function Test-Health {
    param(
        [string]$Name,
        [string]$Url
    )

    for ($attempt = 1; $attempt -le 12; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            Write-Step "$Name health check OK. StatusCode=$($response.StatusCode)"
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    Write-SetupWarning "$Name health check did not respond successfully: $Url"
}

Import-Config

$PackageUrl = [string](Get-Option "PackageUrl" $PackageUrl $PackageUrl)
$PackagePath = [string](Get-Option "PackagePath" $PackagePath $PackagePath)
$ServerIp = [string](Get-Option "ServerIp" $ServerIp $ServerIp)
$ApiPort = [int](Get-Option "ApiPort" $ApiPort $ApiPort)
$WebUiPort = [int](Get-Option "WebUiPort" $WebUiPort $WebUiPort)
$SqlInstanceName = [string](Get-Option "SqlInstanceName" $SqlInstanceName $SqlInstanceName)
$SqlUser = [string](Get-Option "SqlUser" $SqlUser $SqlUser)
$SqlPassword = [string](Get-Option "SqlPassword" $SqlPassword $SqlPassword)
$IisRootPath = [string](Get-Option "IisRootPath" $IisRootPath $IisRootPath)
$ApiSiteName = [string](Get-Option "ApiSiteName" $ApiSiteName $ApiSiteName)
$WebUiSiteName = [string](Get-Option "WebUiSiteName" $WebUiSiteName $WebUiSiteName)
$ApiAppPoolName = [string](Get-Option "ApiAppPoolName" $ApiAppPoolName $ApiAppPoolName)
$WebUiAppPoolName = [string](Get-Option "WebUiAppPoolName" $WebUiAppPoolName $WebUiAppPoolName)
$AdminAudioAgentPath = [string](Get-Option "AdminAudioAgentPath" $AdminAudioAgentPath $AdminAudioAgentPath)
$ServerNotifierPath = [string](Get-Option "ServerNotifierPath" $ServerNotifierPath $ServerNotifierPath)
$ScriptsPath = [string](Get-Option "ScriptsPath" $ScriptsPath $ScriptsPath)
$OpenFirewall = [bool](Get-Option "OpenFirewall" $OpenFirewall $OpenFirewall)
$RegisterTask = [bool](Get-Option "RegisterTask" $RegisterTask $RegisterTask)
$TriggerTask = [bool](Get-Option "TriggerTask" $TriggerTask $TriggerTask)
$PreserveUploads = [bool](Get-Option "PreserveUploads" $PreserveUploads $PreserveUploads)
$InstallHostingBundle = [bool](Get-Option "InstallHostingBundle" $InstallHostingBundle $InstallHostingBundle)
$HostingBundleUrl = [string](Get-Option "HostingBundleUrl" $HostingBundleUrl $HostingBundleUrl)

Require-Value "ServerIp" $ServerIp
Require-Value "SqlInstanceName" $SqlInstanceName
Require-Value "SqlUser" $SqlUser
Require-Value "SqlPassword" $SqlPassword

Write-Step "Starting setup. ServerIp=$ServerIp, API=$ApiPort, WebUI=$WebUiPort"
Assert-Prerequisites

$packageRoot = Resolve-PackageRoot
Assert-Package $packageRoot

$apiPath = Join-Path $IisRootPath "API"
$webUiPath = Join-Path $IisRootPath "WebUI"

Write-Step "Installing API to $apiPath"
Clear-Directory $apiPath
Copy-DirectoryContents -Source (Join-Path $packageRoot "publishes\API") -Destination $apiPath

Write-Step "Installing WebUI to $webUiPath"
Clear-WebUiDirectoryPreservingUploads $webUiPath
Copy-DirectoryContents -Source (Join-Path $packageRoot "publishes\WebUI") -Destination $webUiPath
Grant-UploadPermissions -WebUiPath $webUiPath

Write-Step "Installing AdminAudioAgent to $AdminAudioAgentPath"
Ensure-Directory $AdminAudioAgentPath
Copy-DirectoryContents -Source (Join-Path $packageRoot "publishes\AdminAudioAgent") -Destination $AdminAudioAgentPath
Ensure-Directory (Join-Path $AdminAudioAgentPath "cache")

Write-Step "Installing ServerNotifier to $ServerNotifierPath"
Ensure-Directory $ServerNotifierPath
Copy-DirectoryContents -Source (Join-Path $packageRoot "publishes\ServerNotifier") -Destination $ServerNotifierPath

Write-Step "Installing scripts to $ScriptsPath"
Ensure-Directory $ScriptsPath
Copy-DirectoryContents -Source (Join-Path $packageRoot "scripts") -Destination $ScriptsPath

Write-Step "Writing environment appsettings"
Write-AppSettings -ApiPath $apiPath -WebUiPath $webUiPath -AgentPath $AdminAudioAgentPath -NotifierPath $ServerNotifierPath

Write-Step "Configuring IIS"
Ensure-AppPool -Name $ApiAppPoolName
Ensure-AppPool -Name $WebUiAppPoolName
Ensure-Website -Name $ApiSiteName -PhysicalPath $apiPath -Port $ApiPort -AppPoolName $ApiAppPoolName
Ensure-Website -Name $WebUiSiteName -PhysicalPath $webUiPath -Port $WebUiPort -AppPoolName $WebUiAppPoolName

Ensure-FirewallRule -Name "CafeOrders API $ApiPort" -Port $ApiPort
Ensure-FirewallRule -Name "CafeOrders WebUI $WebUiPort" -Port $WebUiPort

Register-WatchDogTask

Test-Health -Name "API" -Url "http://$ServerIp`:$ApiPort/api/v1/settings/app"
Test-Health -Name "WebUI" -Url "http://$ServerIp`:$WebUiPort/"

Write-Step "Setup completed."
