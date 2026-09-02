# Deploys published site content to MonsterASP.NET via Web Deploy (contentPath only).
# Shared hosting does not allow recycleApp / application pool stop-start.
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
$source = $SourcePath.Trim().TrimEnd('\', '/')

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
if ($serverClean -notmatch '^(https://[^/:]+)') {
    throw 'Could not parse host from SERVER_COMPUTER_NAME.'
}
$computerName = "$($Matches[1]):8172/msdeploy.axd?site=$site"

Write-Host "Deploying '$source' -> contentPath '$site'"
Write-Host "Credential lengths: site=$($site.Length) user=$($user.Length) pass=$($pass.Length)"

function Escape-MsDeploy([string]$value) {
    # msdeploy single-quoted provider values escape ' as ''
    return $value.Replace("'", "''")
}

# Quote provider values so passwords with commas/special chars still parse.
$dest = "contentPath='$(Escape-MsDeploy $site)',computerName='$(Escape-MsDeploy $computerName)',userName='$(Escape-MsDeploy $user)',password='$(Escape-MsDeploy $pass)',authType='Basic',includeAcls='False'"

$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = $msdeploy
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$null = $psi.ArgumentList.Add('-verb:sync')
$null = $psi.ArgumentList.Add('-allowUntrusted')
$null = $psi.ArgumentList.Add("-source:contentPath=$source")
$null = $psi.ArgumentList.Add("-dest:$dest")
$null = $psi.ArgumentList.Add('-disableLink:AppPoolExtension')
$null = $psi.ArgumentList.Add('-disableLink:ContentExtension')
$null = $psi.ArgumentList.Add('-disableLink:CertificateExtension')

$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()

if ($stdout) { Write-Host $stdout }
if ($stderr) { Write-Host $stderr }

if ($proc.ExitCode -ne 0) {
    throw "msdeploy failed with exit code $($proc.ExitCode)"
}

Write-Host 'Web Deploy completed successfully.'
