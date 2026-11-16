# VenVia Desk 0.4 alpha.1 — Windows read-only preflight
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Windows required' }
$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
[pscustomobject]@{ OS=$os.Caption; Version=$os.Version; Architecture=$os.OSArchitecture; RAMBytes=$cs.TotalPhysicalMemory } | Format-List
Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | Select-Object DeviceID,FreeSpace,Size | Format-Table
foreach ($name in 'php','mariadb','mysql','nginx','httpd') {
  $cmd = Get-Command $name -ErrorAction SilentlyContinue
  if ($cmd) { Write-Output "$name : present" } else { Write-Output "$name : not found in PATH" }
}
Get-Service | Where-Object { $_.Name -match 'maria|mysql|nginx|apache|w3svc' } | Select-Object Name,Status | Format-Table
try { Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object { $_.LocalPort -in 80,443,3306 } | Select-Object LocalAddress,LocalPort,OwningProcess | Format-Table }
catch { Write-Warning 'Port inspection unavailable; manual review required.' }
Write-Output 'Preflight inventory only; no installation or compatibility approval performed.'
