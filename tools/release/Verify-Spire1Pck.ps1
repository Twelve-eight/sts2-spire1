[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PckPath,
    [Parameter(Mandatory=$true)][string]$AssetManifestPath,
    [Parameter(Mandatory=$true)][string]$OutputJson
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Resolve-FullPath([string]$Path) {
    return [System.IO.Path]::GetFullPath($Path)
}

function Assert-Under([string]$Path, [string]$Root, [string]$Name) {
    $p = (Resolve-FullPath $Path).TrimEnd([char[]]"\/").ToLowerInvariant()
    $r = (Resolve-FullPath $Root).TrimEnd([char[]]"\/").ToLowerInvariant()
    if ($p -ne $r -and -not $p.StartsWith($r + '\')) {
        throw "PATH-DENY [$Name]: '$Path' is outside '$Root'."
    }
}

function Read-ExactBytes([System.IO.BinaryReader]$Reader, [int]$Count) {
    $bytes = $Reader.ReadBytes($Count)
    if ($bytes.Length -ne $Count) {
        throw "PCK-TRUNCATED: expected $Count bytes, got $($bytes.Length)."
    }
    return $bytes
}

function Read-Utf8Path([System.IO.BinaryReader]$Reader) {
    $length = [int64]$Reader.ReadUInt32()
    if ($length -gt 16MB) {
        throw "PCK-DENY: path length $length exceeds the safety limit."
    }
    $bytes = Read-ExactBytes $Reader ([int]$length)
    $path = [Text.Encoding]::UTF8.GetString($bytes).TrimEnd([char]0)
    if ([string]::IsNullOrWhiteSpace($path)) {
        throw 'PCK-DENY: empty directory path.'
    }
    return $path.Replace('\','/')
}

function Add-Expected([System.Collections.Generic.HashSet[string]]$Set, [string]$Value) {
    if (-not $Set.Add($Value)) {
        throw "ASSET-MANIFEST-DUPLICATE: '$Value'."
    }
}

$pck = Resolve-FullPath $PckPath
$assetManifest = Resolve-FullPath $AssetManifestPath
$output = Resolve-FullPath $OutputJson
if (-not (Test-Path -LiteralPath $pck -PathType Leaf)) { throw "INPUT-MISSING: '$pck'." }
if (-not (Test-Path -LiteralPath $assetManifest -PathType Leaf)) { throw "INPUT-MISSING: '$assetManifest'." }
$workspaceRoot = 'G:\omp works'
Assert-Under $assetManifest $workspaceRoot 'asset-manifest'
Assert-Under $output (Join-Path $workspaceRoot '.tmp') 'output-json'

$manifest = Get-Content -LiteralPath $assetManifest -Raw | ConvertFrom-Json
$expected = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$keptRows = @($manifest.Kept)
foreach ($row in $keptRows) {
    $rel = ([string]$row.Path).Replace('\','/').TrimStart('/')
    if ($rel -match '(^|/)\.\.?(/|$)' -or [string]::IsNullOrWhiteSpace($rel)) {
        throw "ASSET-MANIFEST-DENY: unsafe source path '$rel'."
    }
    $logical = "Spire1/$rel"
    $ext = [IO.Path]::GetExtension($rel).ToLowerInvariant()
    if ($ext -eq '.png') {
        $hash = ([Security.Cryptography.MD5]::Create()).ComputeHash([Text.Encoding]::UTF8.GetBytes("res://$logical"))
        $hex = -join ($hash | ForEach-Object { $_.ToString('x2') })
        Add-Expected $expected (".godot/imported/{0}-{1}.ctex" -f ([IO.Path]::GetFileName($rel)), $hex)
        Add-Expected $expected "$logical.import"
    } elseif ($ext -eq '.json') {
        Add-Expected $expected $logical
    } else {
        throw "ASSET-MANIFEST-DENY: unsupported source extension '$rel'."
    }
}

$stream = [IO.File]::Open($pck, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
$reader = [IO.BinaryReader]::new($stream)
try {
    $magic = [Text.Encoding]::ASCII.GetString((Read-ExactBytes $reader 4))
    if ($magic -ne 'GDPC') { throw "PCK-DENY: magic '$magic' is not GDPC." }
    $format = $reader.ReadUInt32()
    $major = $reader.ReadUInt32()
    $minor = $reader.ReadUInt32()
    $patch = $reader.ReadUInt32()
    $flags = $reader.ReadUInt32()
    $fileBase = [int64]$reader.ReadUInt64()
    $directoryOffset = [int64]$reader.ReadUInt64()
    [void](Read-ExactBytes $reader (16 * 4))
    [void](Read-ExactBytes $reader 8)
    $headerEnd = $stream.Position
    if ($format -ne 3 -or $major -ne 4 -or $minor -ne 5 -or $patch -ne 1) {
        throw "PCK-DENY: expected Godot 4.5.1 format v3, got v$format engine $major.$minor.$patch."
    }
    if (($flags -band 0x2) -eq 0) { throw "PCK-DENY: PACK_REL_FILEBASE flag is missing (flags=$flags)." }
    if ($headerEnd -ne 112 -or $fileBase -ne 112) {
        throw "PCK-DENY: unexpected 112-byte header/file base (header=$headerEnd, base=$fileBase)."
    }
    $fileLength = $stream.Length
    if ($directoryOffset -lt $fileBase -or $directoryOffset -ge $fileLength) {
        throw "PCK-DENY: directory offset $directoryOffset is outside file length $fileLength."
    }
    $stream.Position = $directoryOffset
    $fileCount = [int64]$reader.ReadUInt32()
    if ($fileCount -gt 100000) { throw "PCK-DENY: unreasonable file count $fileCount." }
    $entries = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for ($i = 0; $i -lt $fileCount; $i++) {
        $path = Read-Utf8Path $reader
        if ($path.StartsWith('res://', [StringComparison]::Ordinal)) { $path = $path.Substring(6) }
        if ($path -match '(^|/)\.\.?(/|$)' -or $path.StartsWith('/') -or $path.Contains(':')) {
            throw "PCK-DENY: unsafe internal path '$path'."
        }
        if (-not $seen.Add($path)) { throw "PCK-DUPLICATE: '$path'." }
        $relativeOffset = [int64]$reader.ReadUInt64()
        $size = [int64]$reader.ReadUInt64()
        $md5 = Read-ExactBytes $reader 16
        $entryFlags = $reader.ReadUInt32()
        $absoluteOffset = $fileBase + $relativeOffset
        if (($absoluteOffset % 32) -ne 0) { throw "PCK-DENY: data offset $absoluteOffset for '$path' is not 32-byte aligned." }
        if ($relativeOffset -lt 0 -or $size -lt 0 -or $absoluteOffset -lt $fileBase -or $absoluteOffset + $size -gt $directoryOffset) {
            throw "PCK-DENY: data range for '$path' is outside the file data area."
        }
        $entries.Add([pscustomobject]@{
            Path = $path
            RelativeOffset = $relativeOffset
            AbsoluteOffset = $absoluteOffset
            Bytes = $size
            Md5 = (($md5 | ForEach-Object { $_.ToString('x2') }) -join '')
            Flags = $entryFlags
        })
    }
    if ($reader.BaseStream.Position -gt $fileLength) { throw 'PCK-TRUNCATED: directory extends beyond file.' }

    $actual = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $entries) { [void]$actual.Add($entry.Path) }
    $missing = @($expected | Where-Object { -not $actual.Contains($_) } | Sort-Object)
    $unexpected = @($actual | Where-Object { -not $expected.Contains($_) } | Sort-Object)
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw "PCK-STRUCTURE-MISMATCH: missing=$($missing.Count), unexpected=$($unexpected.Count)."
    }

    $md5 = [Security.Cryptography.MD5]::Create()
    foreach ($entry in $entries) {
        $stream.Position = $entry.AbsoluteOffset
        if ($entry.Bytes -gt [int32]::MaxValue) { throw "PCK-DENY: entry '$($entry.Path)' is too large to verify." }
        $data = Read-ExactBytes $reader ([int]$entry.Bytes)
        $digest = $md5.ComputeHash($data)
        $actualMd5 = (($digest | ForEach-Object { $_.ToString('x2') }) -join '')
        if ($actualMd5 -ne $entry.Md5) { throw "PCK-MD5-MISMATCH: '$($entry.Path)'." }
        if ($entry.Path.StartsWith('.godot/imported/', [StringComparison]::Ordinal)) {
            $header = [Text.Encoding]::ASCII.GetString($data[0..3])
            if ($header -ne 'GST2') { throw "PCK-CTEX-DENY: '$($entry.Path)' lacks GST2 header." }
        }
    }
    $md5.Dispose()

    $outObject = [pscustomobject]@{
        Schema = 'Spire1ReleasePckManifest.v1'
        PckPath = $pck
        PckSha256 = (Get-FileHash -LiteralPath $pck -Algorithm SHA256).Hash.ToUpperInvariant()
        PckBytes = $fileLength
        Header = [pscustomobject]@{ Format=$format; Engine="$major.$minor.$patch"; Flags=$flags; FileBase=$fileBase; DirectoryOffset=$directoryOffset; HeaderBytes=$headerEnd }
        SourceFiles = $keptRows.Count
        ExpectedEntries = $expected.Count
        ActualEntries = $entries.Count
        CtexEntries = @($entries | Where-Object Path -like '.godot/imported/*.ctex').Count
        ImportEntries = @($entries | Where-Object Path -like 'Spire1/*.import').Count
        JsonEntries = @($entries | Where-Object Path -like 'Spire1/*.json').Count
        Missing = @()
        Unexpected = @()
        Md5Verified = $true
    }
    $parent = Split-Path -Parent $output
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    $outObject | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $output -Encoding UTF8
    Write-Host "PCK_VERIFY_PASS entries=$($entries.Count) sourceFiles=$($keptRows.Count) sha256=$($outObject.PckSha256)"
} finally {
    $reader.Dispose()
    $stream.Dispose()
}




