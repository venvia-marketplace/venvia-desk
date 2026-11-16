# Build/QA helper only; not a privileged installer.
param(
    [Parameter(Mandatory=$true)][string]$ArchiveDirectory
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows-x64.candidates.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.schema -ne 1 -or $manifest.platform -ne 'windows-x64') { throw 'Unsupported manifest' }
$root = Get-Item -LiteralPath $ArchiveDirectory
if (!$root.PSIsContainer -or ($root.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Invalid archive directory' }
$names = @{
    php = 'php-8.5.12-1-nts-Win32-vs17-x64.zip'
    mariadb = 'mariadb-11.4.13-winx64.zip'
    nginx = 'nginx-1.30.6.zip'
}
$results = foreach ($pkg in $manifest.packages) {
    $name = $names[$pkg.id]
    if (!$name) { throw 'Unexpected package ID' }
    $path = Join-Path $root.FullName $name
    $state = 'missing'
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $item = Get-Item -LiteralPath $path
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse point archive rejected' }
        if ($pkg.sha256 -notmatch '^[a-fA-F0-9]{64}$') { $state = 'publisher-verification-pending' }
        elseif ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ieq $pkg.sha256) { $state = 'hash-match' }
        else { $state = 'hash-mismatch' }
    }
    [pscustomobject]@{ id=$pkg.id; version=$pkg.version; state=$state }
}
$results | ConvertTo-Json -Depth 3
# No approval trust root or signature verifier has been provisioned yet.
# A matching hash alone never authorizes execution.
exit 1
