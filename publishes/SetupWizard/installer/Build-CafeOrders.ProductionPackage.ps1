[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "packages\CafeOrders-Production.zip")
)

$ErrorActionPreference = "Stop"

function Copy-RequiredDirectory {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Required package source not found: $Source"
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
}

$tempRoot = Join-Path $env:TEMP ("CafeOrdersProductionPackage_" + [Guid]::NewGuid().ToString("N"))
$packageRoot = Join-Path $tempRoot "CafeOrders"

try {
    New-Item -ItemType Directory -Path (Join-Path $packageRoot "publishes") -Force | Out-Null

    foreach ($publishName in @("API", "WebUI", "DesktopApp", "AdminAudioAgent", "ServerNotifier")) {
        Copy-RequiredDirectory `
            -Source (Join-Path $RepoRoot "publishes\$publishName") `
            -Destination (Join-Path $packageRoot "publishes\$publishName")
    }

    Copy-RequiredDirectory -Source (Join-Path $RepoRoot "scripts") -Destination (Join-Path $packageRoot "scripts")
    Copy-RequiredDirectory -Source (Join-Path $RepoRoot "installer") -Destination (Join-Path $packageRoot "installer")

    New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $OutputPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    $item = Get-Item -LiteralPath $OutputPath
    Write-Host "[CafeOrders Package] Created: $OutputPath"
    Write-Host "[CafeOrders Package] Size MB: $([Math]::Round($item.Length / 1MB, 2))"
    Write-Warning "Generated package is usually larger than GitHub's normal 100 MB file limit. Prefer GitHub Releases or an external artifact location for the zip file."
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
