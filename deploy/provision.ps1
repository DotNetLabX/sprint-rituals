[CmdletBinding()]
param(
    [string]$AppName,
    [string]$ResourceGroup,
    [string]$Subscription,
    [switch]$Prepare,
    [switch]$Cutover,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ConfigPath = Join-Path $PSScriptRoot 'deploy-config.ps1'
$CutoverDir = Join-Path $PSScriptRoot '.cutover'
$SnapshotPath = Join-Path $CutoverDir 'old-config.txt'
$NewStartupCommand = 'dotnet Host.dll'

$RequiredSecrets = @('Google__ClientId', 'Google__ClientSecret', 'Jira__InstanceUrl', 'Jira__Email', 'Jira__ApiToken', 'Jira__ProjectKey')
$OptionalSecrets = @('Xray__ClientId', 'Xray__ClientSecret', 'Xray__BaseUrl')

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
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

function Format-AzCommand([string[]]$AzArgs) {
    $shown = foreach ($arg in $AzArgs) {
        if ($arg -match '\s') { '"' + $arg + '"' } else { $arg }
    }
    return 'az ' + ($shown -join ' ')
}

function Get-AzArgs([string]$Command, [string[]]$Extra, [bool]$Targeted) {
    $azArgs = @($Command -split ' ')
    if ($Targeted) { $azArgs += @('--name', $script:AppName, '--resource-group', $script:ResourceGroup) }
    if ($script:Subscription) { $azArgs += @('--subscription', $script:Subscription) }
    $azArgs += $Extra
    $azArgs += '--only-show-errors'
    return $azArgs
}

# Reads only ever ask for names or single values (--query), so their output never carries a secret.
function Invoke-AzRead([string]$Command, [string[]]$Extra, [switch]$NoTarget) {
    $azArgs = Get-AzArgs -Command $Command -Extra $Extra -Targeted (-not $NoTarget)
    if ($DryRun) {
        Write-Host "  [dry run] $(Format-AzCommand $azArgs)"
        return @()
    }
    $output = az @azArgs
    if ($LASTEXITCODE -ne 0) { throw "$(Format-AzCommand $azArgs) failed (exit code $LASTEXITCODE)." }
    return @($output | Where-Object { "$_".Trim() -ne '' } | ForEach-Object { "$_".Trim() })
}

function Invoke-AzWrite([string]$Command, [string[]]$Extra) {
    $azArgs = Get-AzArgs -Command $Command -Extra $Extra -Targeted $true
    if ($DryRun) {
        Write-Host "  [dry run] $(Format-AzCommand $azArgs)"
        return
    }
    Write-Host "  $(Format-AzCommand $azArgs)"
    az @azArgs
    if ($LASTEXITCODE -ne 0) { throw "$(Format-AzCommand $azArgs) failed (exit code $LASTEXITCODE)." }
}

function Get-PlanInfo {
    $planId = @(Invoke-AzRead 'webapp show' @('--query', 'appServicePlanId', '-o', 'tsv'))
    if ($DryRun) {
        Invoke-AzRead 'appservice plan show' @('--ids', '{planId}', '--query', '[name,sku.name,sku.tier]', '-o', 'tsv') -NoTarget | Out-Null
        return $null
    }
    if ($planId.Count -eq 0) { throw "Could not read the App Service plan of $($script:AppName)." }
    $plan = @(Invoke-AzRead 'appservice plan show' @('--ids', $planId[0], '--query', '[name,sku.name,sku.tier]', '-o', 'tsv') -NoTarget)
    if ($plan.Count -lt 3) { throw "Could not read the name and SKU of plan $($planId[0])." }
    return @{ Id = $planId[0]; Name = $plan[0]; SkuName = $plan[1]; SkuTier = $plan[2] }
}

function Assert-FreeTier($Plan) {
    if ($DryRun) {
        Write-Host '  [dry run] zero-cost guard not evaluated (no az call ran)'
        return
    }
    if ($Plan.SkuName -ne 'F1' -and $Plan.SkuTier -ne 'Free') {
        throw "Zero-cost guard: plan '$($Plan.Name)' is $($Plan.SkuName) ($($Plan.SkuTier)), not F1/Free. Scale it back to F1 first (docs/deployment-azure.md, Demo day); this script never changes the tier."
    }
    Write-Host "  plan '$($Plan.Name)' is $($Plan.SkuName) ($($Plan.SkuTier)) - zero-cost guard passed"
}

function Get-AppsOnPlan($Plan) {
    $rows = @(Invoke-AzRead 'webapp list' @('--query', '[].[name,appServicePlanId]', '-o', 'tsv') -NoTarget)
    if ($DryRun) { return @() }
    $names = foreach ($row in $rows) {
        $cols = $row -split "`t"
        if ($cols.Count -ge 2 -and $cols[1] -ieq $Plan.Id) { $cols[0] }
    }
    return @($names)
}

function Get-StartupCommand {
    $value = @(Invoke-AzRead 'webapp config show' @('--query', 'appCommandLine', '-o', 'tsv'))
    if ($value.Count -eq 0) { return '' }
    return $value[0]
}

function Get-DatabasePath {
    $value = @(Invoke-AzRead 'webapp config connection-string list' @('--query', "[?name=='Database'].value", '-o', 'tsv'))
    if ($value.Count -eq 0) { return '' }
    if ($value[0] -match 'Data Source\s*=\s*([^;]+)') { return $Matches[1].Trim() }
    return '(unrecognised format - check the Database connection string in the portal)'
}

function Get-ConnectionStringNames {
    return @(Invoke-AzRead 'webapp config connection-string list' @('--query', '[].name', '-o', 'tsv'))
}

function Invoke-Prepare {
    Write-Step "Prepare (read-only) for $($script:AppName) in $($script:ResourceGroup)"

    $plan = Get-PlanInfo
    Assert-FreeTier $plan
    $apps = Get-AppsOnPlan $plan
    if (-not $DryRun) {
        Write-Host "App Service plan: $($plan.Name)"
        Write-Host "Apps on this plan (a demo-day tier change moves them all): $($apps -join ', ')"
    }

    $present = @(Invoke-AzRead 'webapp config appsettings list' @('--query', '[].name', '-o', 'tsv'))
    $startup = Get-StartupCommand
    $dbPath = Get-DatabasePath
    if ($DryRun) { return }

    $missingRequired = @($RequiredSecrets | Where-Object { $present -notcontains $_ })
    $missingOptional = @($OptionalSecrets | Where-Object { $present -notcontains $_ })
    if ($missingRequired.Count -gt 0) {
        Write-Host "Missing required secrets (add them inside the cutover window): $($missingRequired -join ', ')" -ForegroundColor Yellow
    }
    else {
        Write-Host 'Required secrets: all present'
    }
    if ($missingOptional.Count -gt 0) {
        Write-Host "Missing optional secrets: $($missingOptional -join ', ')"
    }
    Write-Host "Current startup command: '$startup'"
    Write-Host "Current Database path (back this file up in the cutover): '$dbPath'"
}

function Get-SubscriptionId {
    $value = @(Invoke-AzRead 'account show' @('--query', 'id', '-o', 'tsv') -NoTarget)
    if ($DryRun) { return '' }
    if ($value.Count -eq 0) { throw 'Could not read the id of the target subscription.' }
    return $value[0]
}

function Read-Snapshot {
    $values = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($SnapshotPath)) {
        $pair = $line -split '=', 2
        if ($pair.Count -eq 2) { $values[$pair[0].Trim()] = $pair[1] }
    }
    return $values
}

function Assert-SnapshotTarget([string]$SubscriptionId) {
    $snapshot = Read-Snapshot
    $expected = [ordered]@{ App = $script:AppName; ResourceGroup = $script:ResourceGroup }
    if ($DryRun) {
        Write-Host '  [dry run] snapshot subscription not compared (no az call ran)'
    }
    else {
        $expected['Subscription'] = $SubscriptionId
    }
    foreach ($key in $expected.Keys) {
        $recorded = $snapshot[$key]
        if (-not $recorded -or $recorded -ine $expected[$key]) {
            throw "Snapshot $SnapshotPath belongs to another target ($key '$recorded', this run '$($expected[$key])') - refusing before any change. Move it aside only if that other cutover is finished."
        }
    }
}

function Save-Snapshot {
    $subscriptionId = Get-SubscriptionId
    if (Test-Path $SnapshotPath) {
        Assert-SnapshotTarget $subscriptionId
        Write-Host "  snapshot $SnapshotPath exists for this target - kept untouched (resumed cutover)"
        return
    }
    $startup = Get-StartupCommand
    $dbPath = Get-DatabasePath
    if ($DryRun) {
        Write-Host "  [dry run] would record the startup command and Database path in $SnapshotPath"
        return
    }
    if ($startup -eq $NewStartupCommand) {
        throw "The startup command is already '$NewStartupCommand' but no snapshot exists at $SnapshotPath - refusing, the old configuration cannot be recorded any more."
    }
    $lines = @(
        "App=$($script:AppName)",
        "ResourceGroup=$($script:ResourceGroup)",
        "Subscription=$subscriptionId",
        "RecordedAtUtc=$((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))",
        "StartupCommand=$startup",
        "DatabasePath=$dbPath"
    )
    if (-not (Test-Path $CutoverDir)) { New-Item -ItemType Directory -Path $CutoverDir | Out-Null }
    [System.IO.File]::WriteAllLines($SnapshotPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "  recorded the old startup command and Database path in $SnapshotPath"
}

function Invoke-Cutover {
    Write-Step "Cutover for $($script:AppName) in $($script:ResourceGroup)"

    $plan = Get-PlanInfo
    Assert-FreeTier $plan

    Write-Step 'Snapshot of the old configuration (write-once)'
    Save-Snapshot

    Write-Step 'Applying the new configuration'
    $origin = "https://$($script:AppName).azurewebsites.net"
    Invoke-AzWrite 'webapp config appsettings set -o none' @('--settings', 'ASPNETCORE_FORWARDEDHEADERS_ENABLED=true', 'SCM_DO_BUILD_DURING_DEPLOYMENT=false', "Auth__AllowedReturnOrigins__0=$origin")
    Invoke-AzWrite 'webapp log config -o none' @('--docker-container-logging', 'filesystem')
    Invoke-AzWrite 'webapp config set -o none' @('--http20-enabled', 'true', '--startup-file', $NewStartupCommand)
    Invoke-AzWrite 'webapp update -o none' @('--https-only', 'true')
    Invoke-AzWrite 'webapp config connection-string delete -o none' @('--setting-names', 'Database')

    Write-Step 'Post-condition: no Database connection string'
    $names = Get-ConnectionStringNames
    if (-not $DryRun -and ($names -contains 'Database')) {
        throw "The Database connection string is still present - re-run -Cutover."
    }
    if ($DryRun) {
        Write-Host 'Dry run complete - nothing was changed. The old database file itself is never touched.' -ForegroundColor Green
        return
    }
    Write-Host 'Cutover configuration applied. The old database file itself is not touched.' -ForegroundColor Green
}

function Invoke-Main {
    if ($Prepare.IsPresent -eq $Cutover.IsPresent) {
        throw 'Choose exactly one of -Prepare and -Cutover.'
    }
    Resolve-Target
    if ($DryRun) { Write-Host 'Dry run: printing every az command, running none.' -ForegroundColor Yellow }
    if ($Prepare) { Invoke-Prepare } else { Invoke-Cutover }
}

try {
    Invoke-Main
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
