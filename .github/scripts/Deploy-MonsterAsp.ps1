# Deploys published site content to MonsterASP.NET via Web Deploy (contentPath only).
# Shared hosting does not allow recycleApp / application pool stop-start.
# Matches MonsterASP docs:
# https://help.monsterasp.net/books/deploy/page/how-to-deploy-website-content-from-command-line
param(
    [Parameter(Mandatory = $true)][string]$SourcePath,
    [Parameter(Mandatory = $true)][string]$WebsiteName,
    [Parameter(Mandatory = $true)][string]$ServerComputerName,
    [Parameter(Mandatory = $true)][string]$Username,
    [Parameter(Mandatory = $true)][string]$Password
)

$ErrorActionPreference = 'Stop'

$msdeploy = Join-Path ([Environment]::GetEnvironmentVariable('ProgramFiles(x86)')) 'IIS\Microsoft Web Deploy V3\msdeploy.exe'
if (-not (Test-Path -LiteralPath $msdeploy)) {
    throw "Web Deploy not found at $msdeploy"
}

$site = $WebsiteName.Trim()
$serverRaw = $ServerComputerName.Trim()
$user = $Username.Trim()
$pass = $Password
$source = (Resolve-Path -LiteralPath $SourcePath).Path.TrimEnd('\')

if ([string]::IsNullOrWhiteSpace($site) -or
    [string]::IsNullOrWhiteSpace($serverRaw) -or
    [string]::IsNullOrWhiteSpace($user) -or
    [string]::IsNullOrWhiteSpace($pass)) {
    throw 'One or more WebDeploy values are empty (website, server, username, password).'
}

if ($serverRaw -match 'runasp\.net' -or $serverRaw -notmatch 'siteasp\.net') {
    throw @"
Invalid SERVER_COMPUTER_NAME.
Use the WebDeploy URL from MonsterASP Control Panel, e.g.:
  https://siteXXXX.siteasp.net:8172
Do NOT use the public website host (*.runasp.net).
"@
}

$serverClean = ($serverRaw -replace '\s', '')
if ($serverClean -notmatch '^https?://') {
    $serverClean = "https://$serverClean"
}
if ($serverClean -notmatch '^https://([^.]+)\.siteasp\.net') {
    throw 'Could not parse site id from SERVER_COMPUTER_NAME (expected https://siteXXXX.siteasp.net:8172).'
}

$hostSiteId = $Matches[1]
$computerName = "https://${hostSiteId}.siteasp.net:8172/msdeploy.axd?site=$site"

Write-Host "Deploying '$source' -> contentPath '$site'"
Write-Host "WebDeploy host site id: $hostSiteId"
Write-Host "Credential lengths: site=$($site.Length) user=$($user.Length) pass=$($pass.Length)"

if ($site -ne $hostSiteId) {
    throw "WEBSITE_NAME ('$site') must equal the site id in SERVER_COMPUTER_NAME ('$hostSiteId')."
}
if ($user -ne $hostSiteId) {
    throw "SERVER_USERNAME ('$user') must equal the site id ('$hostSiteId')."
}

# Exact provider shape from MonsterASP command-line docs.
function Quote-MsDeploy([string]$value) {
    return '"' + ($value -replace '"', '""') + '"'
}

$dest = "contentPath=$(Quote-MsDeploy $site),computerName=$(Quote-MsDeploy $computerName),userName=$(Quote-MsDeploy $user),password=$(Quote-MsDeploy $pass),authtype=""Basic"",includeAcls=""False"""

$allArgs = @(
    '-verb:sync',
    "-source:contentPath=$source",
    "-dest:$dest",
    '-allowUntrusted',
    '-disableLink:AppPoolExtension',
    '-disableLink:ContentExtension',
    '-disableLink:CertificateExtension'
)

$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = $msdeploy
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
foreach ($a in $allArgs) {
    $null = $psi.ArgumentList.Add($a)
}

$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()

if ($stdout) { Write-Host $stdout }
if ($stderr) { Write-Host $stderr }

if ($proc.ExitCode -ne 0) {
    throw @"
msdeploy failed with exit code $($proc.ExitCode) (ERROR_USER_UNAUTHORIZED / 401 means bad WebDeploy credentials or site mismatch).

In GitHub → Settings → Secrets, set ALL four from MonsterASP Control Panel → WebDeploy:
  WEBSITE_NAME          = siteXXXX
  SERVER_USERNAME       = siteXXXX   (same value)
  SERVER_COMPUTER_NAME  = https://siteXXXX.siteasp.net:8172
  SERVER_PASSWORD       = WebDeploy password from the panel (reset it there if unsure)
"@
}

Write-Host 'Web Deploy completed successfully.'
