[CmdletBinding()]
param(
    [string]$AppName,
    [string]$ResourceGroup,
    [string]$Subscription,
    [switch]$BuildOnly,
    [switch]$SkipBuild,
    [switch]$SmokeOnly,
    [string]$BaseUrl,
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ClientDir = Join-Path $RepoRoot 'client'
$HostProject = Join-Path $RepoRoot 'src/Host'
$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$PublishDir = Join-Path $ArtifactsDir 'publish'
$ZipPath = Join-Path $ArtifactsDir 'deploy.zip'
$ConfigPath = Join-Path $PSScriptRoot 'deploy-config.ps1'

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Assert-LastExitCode([string]$What) {
    if ($LASTEXITCODE -ne 0) {
        throw "$What failed (exit code $LASTEXITCODE)."
    }
}

function Test-IsWindows {
    return $env:OS -eq 'Windows_NT'
}

function Resolve-Target {
    if ((-not $script:AppName -or -not $script:ResourceGroup -or -not $script:Subscription) -and (Test-Path $ConfigPath)) {
        $config = & {
            . $ConfigPath
            @{
                AppName       = (Get-Variable -Name DeployAppName -ValueOnly -ErrorAction SilentlyContinue)
                ResourceGroup = (Get-Variable -Name DeployResourceGroup -ValueOnly -ErrorAction SilentlyContinue)
                Subscription  = (Get-Variable -Name DeploySubscription -ValueOnly -ErrorAction SilentlyContinue)
            }
        }
        if (-not $script:AppName) { $script:AppName = $config.AppName }
        if (-not $script:ResourceGroup) { $script:ResourceGroup = $config.ResourceGroup }
        if (-not $script:Subscription) { $script:Subscription = $config.Subscription }
    }
    if (-not $script:AppName -or -not $script:ResourceGroup) {
        throw "Missing target: pass -AppName and -ResourceGroup, or copy deploy/deploy-config.example.ps1 to deploy/deploy-config.ps1 and fill in its values."
    }
}

function Invoke-Build {
    Write-Step 'Installing client packages (npm ci)'
    Push-Location $ClientDir
    try {
        npm ci
        Assert-LastExitCode 'npm ci'

        Write-Step 'Building client SPAs (npm run build)'
        npm run build
        Assert-LastExitCode 'npm run build'
    }
    finally {
        Pop-Location
    }

    Write-Step 'Publishing Host (dotnet publish)'
    if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
    dotnet publish $HostProject -c Release -o $PublishDir
    Assert-LastExitCode 'dotnet publish'

    Write-Step "Zipping $PublishDir"
    New-DeployZip -SourceDir $PublishDir -Destination $ZipPath
    Write-Host "Created $ZipPath"
}

function New-DeployZip([string]$SourceDir, [string]$Destination) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $Destination) { Remove-Item -Force $Destination }
    $root = (Resolve-Path $SourceDir).ProviderPath.TrimEnd('\', '/')

    $zip = [System.IO.Compression.ZipFile]::Open($Destination, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -Path $root -Recurse -File) {
            $entryName = $file.FullName.Substring($root.Length).TrimStart('\', '/') -replace '\\', '/'
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Assert-DeployZip {
    if (-not (Test-Path $ZipPath)) {
        throw "Missing deploy artifact $ZipPath - run with -BuildOnly first (or without -SkipBuild)."
    }
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $hostDll = @($zip.Entries | Where-Object { $_.FullName -eq 'Host.dll' })
    }
    finally {
        $zip.Dispose()
    }
    if ($hostDll.Count -eq 0) {
        throw "Deploy artifact $ZipPath has no Host.dll entry - rebuild it with -BuildOnly."
    }
}

function Invoke-Deploy {
    $azArgs = @(
        'webapp', 'deploy',
        '--name', $script:AppName,
        '--resource-group', $script:ResourceGroup,
        '--src-path', $ZipPath,
        '--type', 'zip',
        '--clean', 'true',
        '--track-status', 'false',
        '--only-show-errors',
        '-o', 'none'
    )
    if ($script:Subscription) { $azArgs += @('--subscription', $script:Subscription) }

    Write-Step "Deploying $ZipPath to $($script:AppName) ($($script:ResourceGroup))"
    az @azArgs
    Assert-LastExitCode 'az webapp deploy'
}

function Invoke-SmokeCheck([string]$Url, [int]$Timeout) {
    $probeUrl = $Url.TrimEnd('/') + '/'
    if (Test-IsWindows) {
        $curl = 'curl.exe'
        $nullDevice = 'NUL'
    }
    else {
        $curl = 'curl'
        $nullDevice = '/dev/null'
    }

    Write-Step "Smoke check: waiting up to $Timeout s for $probeUrl to redirect to /dashboard/login"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $attempt = 0
    while ($true) {
        $attempt++
        $remaining = $Timeout - [int][Math]::Ceiling($stopwatch.Elapsed.TotalSeconds)
        if ($remaining -le 0) {
            throw "Smoke check failed: $probeUrl did not redirect to /dashboard/login within $Timeout s."
        }
        $maxTime = [Math]::Min(20, $remaining)
        $result = & $curl -s -o $nullDevice -w '%{http_code} %{redirect_url}' --max-time $maxTime $probeUrl
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0) {
            $parts = "$result".Trim() -split ' ', 2
            $status = [int]$parts[0]
            $location = ''
            if ($parts.Count -gt 1) { $location = $parts[1].Trim() }
            $inTime = $stopwatch.Elapsed.TotalSeconds -le $Timeout
            if ($inTime -and $status -ge 300 -and $status -lt 400 -and $location.TrimEnd('/').EndsWith('/dashboard/login')) {
                Write-Host "Smoke check passed: $status -> $location" -ForegroundColor Green
                return
            }
            Write-Host "  attempt ${attempt}: HTTP $status $location"
        }
        else {
            Write-Host "  attempt ${attempt}: no answer (curl exit code $exitCode)"
        }

        $remaining = $Timeout - [int][Math]::Ceiling($stopwatch.Elapsed.TotalSeconds)
        if ($remaining -gt 0) { Start-Sleep -Seconds ([Math]::Min(5, $remaining)) }
    }
}

function Invoke-Main {
    $modeCount = @($BuildOnly, $SkipBuild, $SmokeOnly | Where-Object { $_ }).Count
    if ($modeCount -gt 1) {
        throw 'Choose at most one of -BuildOnly, -SkipBuild and -SmokeOnly.'
    }

    if ($SmokeOnly) {
        if (-not $script:BaseUrl) {
            Resolve-Target
            $script:BaseUrl = "https://$($script:AppName).azurewebsites.net"
        }
        Invoke-SmokeCheck -Url $script:BaseUrl -Timeout $TimeoutSeconds
        return
    }

    if ($BuildOnly) {
        Invoke-Build
        return
    }

    if ($SkipBuild) {
        Assert-DeployZip
        Resolve-Target
    }
    else {
        Resolve-Target
        Invoke-Build
        Assert-DeployZip
    }

    Invoke-Deploy
    if (-not $script:BaseUrl) { $script:BaseUrl = "https://$($script:AppName).azurewebsites.net" }
    Invoke-SmokeCheck -Url $script:BaseUrl -Timeout $TimeoutSeconds
    Write-Host "Deployed: $($script:BaseUrl)" -ForegroundColor Green
}

try {
    Invoke-Main
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
