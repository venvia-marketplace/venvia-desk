# VenVia Desk Windows bootstrap — 0.4.0-alpha.1
[CmdletBinding()]
param([ValidateSet('Diagnose','Plan')][string]$Mode='Diagnose')
$ErrorActionPreference='Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Windows required' }
Write-Host 'VenVia Desk 0.4.0-alpha.1 — Windows Server Mode'
$preflight=Join-Path $PSScriptRoot 'preflight-windows.ps1'
if (-not (Test-Path -LiteralPath $preflight -PathType Leaf)) { throw 'Missing preflight script; use complete distribution.' }
& $preflight
if ($Mode -eq 'Plan') {
  Write-Host 'Proposed installation — no changes will be made:'
  @('Validate OS, resources, existing services and ports',
    'Select versioned, verified Windows runtime packages',
    'Provision isolated PHP and MariaDB with least privilege',
    'Configure web server and automatic service startup',
    'Configure LAN DNS, authorized subdomain and HTTPS certificate',
    'Install WordPress and VenVia Core; verify offline operation',
    'Validate backup and rollback before production') | ForEach-Object { Write-Host " - $_" }
}
Write-Host 'Bootstrap preview only. Runtime installation is not implemented.'
