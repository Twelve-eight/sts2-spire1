# refresh-workshop-payloads.ps1 - regenerate each repo's workshop staging tree from its Release
# build, then verify source/build/staging identity before anything may publish it.
#
# Why this exists: workshop_upload.vdf's "contentfolder" points at <repo>\workshop\content, which
# is a STAGING COPY, not the build output. Nothing refreshes it automatically, so a push run
# without this step uploads whatever was staged last time. The 2026-09-15 release audit found 7
# of 8 items stale (e.g. Spire1 staged 2026-09-13 vs a build from 2026-09-15 carrying the AFTP-1
# layer), which would have shipped pre-fix binaries under new version numbers.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1"
#   add -WhatIf      to list the copies without performing them (read-only).
#   add -VerifyOnly  to verify source/build/staging identity WITHOUT copying anything (read-only).
#   add -Only <sub>  to restrict both modes to the rows whose Name contains <sub>.
#
# Exit codes: 0 = verified (or regenerated and verified); 1 = any problem (fail closed).
# This script never touches Steam, C:, the shared mod_configs, or any game install.
#
# ASCII only, PowerShell 5.1 compatible.
#
# PUBLISH ALLOWLIST (2026-10-02, A08). For every row the ONLY files that may sit in the staging tree
# are the ones this script names, and the CONTRACT comes from the mod manifest:
#   manifest : <repo>\<Manifest>            -> <stage>\<mod>.json   REQUIRED, source of truth
#   dll      : <buildDir>\<mod>.dll         -> <stage>\<mod>.dll    REQUIRED iff has_dll=true
#   pck      : <buildDir>\<mod>.pck         -> <stage>\<mod>.pck    REQUIRED iff has_pck=true
#   pdb      : <buildDir>\<mod>.pdb         -> <stage>\<mod>.pdb    optional payload (ships if built; a row may set PublishPdb=$false to omit it - see Get-RowAllowlist)
#   digest   : <buildDir>\<mod>.pck.sha256  -> NOT a payload        record only, verified if present
# Reconciliation is BIDIRECTIONAL: every staged file must be named by the allowlist (staging ->
# build), and every REQUIRED artifact must exist in the build output AND in staging with the same
# SHA256 (expected -> staging). A required artifact missing from the build output is a hard failure:
# there is no source-tree fallback, so a stale or hand-placed file can never pass as a build
# artifact. Build-only files (*.deps.json etc.) are deliberately excluded, never required.
# Manifest parsing is strict: id/version must be JSON strings, has_pck/has_dll must be JSON booleans,
# and a missing field or a duplicated top-level key fails closed instead of being coerced.
#
# INVARIANT: this list must cover every item in .tooling\workshop-push-all.ps1. That script uploads
# whatever its vdf contentfolders hold, so any pushed item without a row here ships stale silently.
# The controlled push entry also asserts the pairing at run time (payload provenance gate, exit 12).

param([switch]$WhatIf, [switch]$VerifyOnly, [string]$Only, [string]$Root)

# Fail closed: every read/enumeration failure must stop the row (or the run). Nothing that failed
# to read may be treated as "file absent", "no mismatch", or an empty digest. Callers still record
# a finding before they continue; the sentinel-comparison fail-open is gone.
$ErrorActionPreference = "Stop"

# Default root is the workspace Sts tree; -Root exists so the reconciliation can be exercised
# against an isolated copy without touching the real staging trees. The value is validated below
# BEFORE any manifest read or staging access: only the workspace Sts tree and the workspace .tmp
# tree are legal roots, no dot-dot segment, and no junction/symlink anywhere in the chain.
$root = if ($Root) { $Root } else { "G:\omp works\Sts" }

function Test-RawPathHasDotDot([string]$path) {
    $segments = ($path -replace '/', '\') -split '\\'
    foreach ($segment in $segments) {
        if ($segment -eq '..') { return $true }
    }
    return $false
}

if (Test-RawPathHasDotDot $root) {
    Write-Output ("FATAL: -Root must not contain a dot-dot segment: {0}" -f $root)
    exit 1
}
if (-not [System.IO.Path]::IsPathRooted($root)) {
    Write-Output ("FATAL: -Root must be an absolute path: {0}" -f $root)
    exit 1
}
$root = ([System.IO.Path]::GetFullPath($root)).TrimEnd('\').TrimEnd('/')
$rootAllowed = $false
if ($root -ieq 'G:\omp works\Sts' -or $root.StartsWith('G:\omp works\Sts\', [System.StringComparison]::OrdinalIgnoreCase)) { $rootAllowed = $true }
if ($root -ieq 'G:\omp works\.tmp' -or $root.StartsWith('G:\omp works\.tmp\', [System.StringComparison]::OrdinalIgnoreCase)) { $rootAllowed = $true }
if (-not $rootAllowed) {
    Write-Output ("FATAL: -Root is outside the allowlist (G:\omp works\Sts or G:\omp works\.tmp): {0}" -f $root)
    exit 1
}
$rootReparse = $false
try {
    $rootProbe = $root
    while ($rootProbe) {
        if (Test-Path -LiteralPath $rootProbe -ErrorAction Stop) {
            $rootProbeItem = Get-Item -LiteralPath $rootProbe -Force -ErrorAction Stop
            if (($rootProbeItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { $rootReparse = $true; break }
        }
        $rootProbeParent = Split-Path -Path $rootProbe -Parent
        if (-not $rootProbeParent -or $rootProbeParent -eq $rootProbe) { break }
        $rootProbe = $rootProbeParent
    }
} catch {
    Write-Output ("FATAL: -Root path chain is unreadable: {0} ({1})" -f $root, $_.Exception.Message)
    exit 1
}
if ($rootReparse) {
    Write-Output ("FATAL: -Root or one of its ancestors is a junction/symlink: {0}" -f $root)
    exit 1
}
$rootSteamMarker = $false
$rootMarkerProbe = $root
while ($rootMarkerProbe) {
    if (Test-Path -LiteralPath (Join-Path $rootMarkerProbe 'steam_appid.txt') -PathType Leaf) { $rootSteamMarker = $true; break }
    $rootMarkerParent = Split-Path -Path $rootMarkerProbe -Parent
    if (-not $rootMarkerParent -or $rootMarkerParent -eq $rootMarkerProbe) { break }
    $rootMarkerProbe = $rootMarkerParent
}
if ($rootSteamMarker) {
    Write-Output ("FATAL: -Root sits under a Steam install marker (steam_appid.txt): {0}" -f $root)
    exit 1
}

# Fail closed before any comparison: if SHA256 cannot be computed in this host, every gate below
# would compare empty strings and pass. Probe once, with real bytes, and abort if it does not match
# the known digest of the ASCII string "abc".
$probeBytes = [System.Text.Encoding]::ASCII.GetBytes("abc")
$probeSha = $null
try {
    $probeSha = [System.Security.Cryptography.SHA256]::Create()
    $probeHash = ([System.BitConverter]::ToString($probeSha.ComputeHash($probeBytes)) -replace "-", "")
} catch {
    Write-Output ("FATAL: SHA256 is unavailable in this host ({0}) - refusing to run, because every" -f $_.Exception.Message)
    Write-Output "digest comparison would degrade into a false pass."
    exit 1
} finally {
    if ($probeSha) { $probeSha.Dispose() }
}
if ($probeHash -ne "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD") {
    Write-Output "FATAL: SHA256 self-test mismatch - refusing to run on an unverified hash implementation."
    exit 1
}

# One row per publishable item: repo, manifest id, built DLL, staged DLL, source manifest and the
# VDF that publishes the staged tree. Id is the manifest id this row must publish; Vdf is checked so
# the uploaded contentfolder is exactly the staged tree this script verifies.
$items = @(
    @{ Name = "Spire1";               Repo = "sts2-spire1";             Id = "Spire1";               Build = "mod\.godot\mono\temp\bin\Release\Spire1.dll";               Stage = "workshop\content\Spire1\Spire1.dll";                                 Manifest = "mod\Spire1.json";              Vdf = "workshop\workshop_upload.vdf"; PublishPdb = $false }
    @{ Name = "Perfect";              Repo = "sts2-perfect";            Id = "Perfect";              Build = "mod\.godot\mono\temp\bin\Release\Perfect.dll";              Stage = "workshop\content\Perfect\Perfect.dll";                               Manifest = "mod\Perfect.json";             Vdf = "workshop\workshop_upload.vdf" }
    @{ Name = "ChaosBridge";          Repo = "chaosbridge";             Id = "ChaosBridge";          Build = ".godot\mono\temp\bin\Release\ChaosBridge.dll";              Stage = "workshop\content\ChaosBridge\ChaosBridge.dll";                       Manifest = "ChaosBridge.json";             Vdf = "workshop\workshop_upload.vdf" }
    @{ Name = "RegentFXFastBoot";     Repo = "sts2-regentfxfastboot";   Id = "RegentFXFastBoot";     Build = "mod\.godot\mono\temp\bin\Release\RegentFXFastBoot.dll";     Stage = "workshop\content\RegentFXFastBoot\RegentFXFastBoot.dll";             Manifest = "mod\RegentFXFastBoot.json";    Vdf = "workshop\workshop_upload.vdf" }
    @{ Name = "MpConfigSync";         Repo = "sts2-mpconfigsync";       Id = "MpConfigSync";         Build = "mod\.godot\mono\temp\bin\Release\MpConfigSync.dll";         Stage = "workshop\content\MpConfigSync\MpConfigSync.dll";                     Manifest = "mod\MpConfigSync.json";        Vdf = "workshop\workshop_upload.vdf" }
    @{ Name = "HeartShake";           Repo = "sts2-heartshake";         Id = "HeartShake";           Build = "mod\.godot\mono\temp\bin\Release\HeartShake.dll";           Stage = "workshop\content\HeartShake\HeartShake.dll";                         Manifest = "mod\HeartShake.json";          Vdf = "workshop\workshop_upload.vdf" }
    @{ Name = "QuriousCraftingRelics";Repo = "AutoAnthonyRelics";       Id = "QuriousCraftingRelics";Build = "mod\.godot\mono\temp\bin\Release\QuriousCraftingRelics.dll";Stage = "workshop\content\QuriousCraftingRelics\QuriousCraftingRelics.dll";  Manifest = "mod\QuriousCraftingRelics.json";Vdf = "workshop\workshop_upload.vdf" }
)

$rows = $items
if ($Only) {
    $rows = @($items | Where-Object { $_.Name -like "*$Only*" })
    if ($rows.Count -eq 0) {
        Write-Output ("FATAL: -Only '{0}' matched 0 row(s). Names: {1}" -f $Only, (($items | ForEach-Object { $_.Name }) -join ", "))
        exit 1
    }
}

$results = New-Object System.Collections.ArrayList
$findings = New-Object System.Collections.ArrayList
$notes = New-Object System.Collections.ArrayList
$findingKeys = New-Object System.Collections.ArrayList

# Findings are deduplicated by (item, status, detail): the allowlist reconciliation is bidirectional,
# so one real mismatch can be reachable from both directions. A repeated table row would only make
# the problem count look worse without adding information.
function Add-Finding([string]$item, [string]$status, [string]$detail) {
    $key = ("{0}|{1}|{2}" -f $item, $status, $detail)
    if ($findingKeys -ccontains $key) { return }
    [void]$findingKeys.Add($key)
    [void]$findings.Add([pscustomobject]@{ Item = $item; Status = $status; Detail = $detail })
}

# SHA256 via .NET, NOT Get-FileHash: cmdlet autoloading depends on PSModulePath, and under an
# inherited PSModulePath (observed 2026-10-02 in a PowerShell 5.1 -File run) Get-FileHash is simply
# absent. A cmdlet that "cannot be found" returns nothing, so both sides of a comparison became ""
# and compared EQUAL - a silent fail-open that would have passed every SHA256 gate in this script.
# The .NET API has no module dependency. A read/open failure returns $null and EVERY caller must
# treat $null as a hard failure: two unreadable files must never compare as "equal bytes".
function Hash-Of([string]$path) {
    $sha = $null
    $fs = $null
    try {
        $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $bytes = $sha.ComputeHash($fs)
        return ([System.BitConverter]::ToString($bytes) -replace "-", "")
    } catch {
        return $null
    } finally {
        if ($fs) { $fs.Dispose() }
        if ($sha) { $sha.Dispose() }
    }
}

function Short-Hash([string]$hash) {
    if (-not $hash) { return "(none)" }
    if ($hash.Length -le 16) { return $hash }
    return $hash.Substring(0, 16)
}

function Full-Path([string]$path) {
    return [System.IO.Path]::GetFullPath($path)
}

function Get-NormalizedDir([string]$path) {
    return (Full-Path $path).TrimEnd('\').TrimEnd('/')
}

function Test-PathInside([string]$child, [string]$parent) {
    $c = Get-NormalizedDir $child
    $p = Get-NormalizedDir $parent
    if ($c -ieq $p) { return $true }
    return $c.StartsWith($p + '\', [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-ReparseChain([string]$path) {
    # Walk from the path itself up to the drive root; any existing level carrying ReparsePoint
    # (junction/symlink/mount point) means the payload path can escape the allowlist. Get-Item uses
    # -ErrorAction Stop: a level that cannot be inspected is a hard error for the caller.
    $p = Full-Path $path
    while ($p) {
        if (Test-Path -LiteralPath $p -ErrorAction Stop) {
            $item = Get-Item -LiteralPath $p -Force -ErrorAction Stop
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { return $true }
        }
        $parent = Split-Path -Path $p -Parent
        if (-not $parent -or $parent -eq $p) { break }
        $p = $parent
    }
    return $false
}

function Test-SteamMarkerAncestor([string]$path) {
    $p = Full-Path $path
    while ($p) {
        if (Test-Path -LiteralPath (Join-Path $p 'steam_appid.txt') -PathType Leaf -ErrorAction Stop) { return $true }
        $parent = Split-Path -Path $p -Parent
        if (-not $parent -or $parent -eq $p) { break }
        $p = $parent
    }
    return $false
}

function Get-FileListChecked([string]$item, [string]$dir, [string]$label) {
    # Bounded BFS that checks EVERY directory for reparse BEFORE descending, so a junction can never
    # make the walk escape the staging/build tree. Returns { Ok; Files }. Enumeration failures are
    # hard failures: an unreadable tree is never treated as empty.
    $files = New-Object System.Collections.ArrayList
    try {
        if (Test-ReparseChain $dir) {
            Add-Finding $item "PATH_REPARSE_POINT" ("{0}: {1}" -f $label, $dir)
            return [pscustomobject]@{ Ok = $false; Files = @() }
        }
    } catch {
        Add-Finding $item "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $label, $dir, $_.Exception.Message)
        return [pscustomobject]@{ Ok = $false; Files = @() }
    }
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) {
        return [pscustomobject]@{ Ok = $true; Files = @() }
    }
    $queue = New-Object System.Collections.Queue
    $queue.Enqueue([pscustomobject]@{ Dir = $dir; Depth = 0 })
    while ($queue.Count -gt 0) {
        $cur = $queue.Dequeue()
        if ($cur.Depth -gt 4) {
            Add-Finding $item "PATH_TOO_DEEP" ("{0}: {1}" -f $label, $cur.Dir)
            return [pscustomobject]@{ Ok = $false; Files = @() }
        }
        try {
            $items = @(Get-ChildItem -LiteralPath $cur.Dir -Force -ErrorAction Stop)
        } catch {
            Add-Finding $item "PATH_ENUM_FAILED" ("{0}: {1} ({2})" -f $label, $cur.Dir, $_.Exception.Message)
            return [pscustomobject]@{ Ok = $false; Files = @() }
        }
        foreach ($it in $items) {
            if (($it.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                Add-Finding $item "PATH_REPARSE_POINT" ("{0}: {1}" -f $label, $it.FullName)
                return [pscustomobject]@{ Ok = $false; Files = @() }
            }
            if ($it.PSIsContainer) {
                $queue.Enqueue([pscustomobject]@{ Dir = $it.FullName; Depth = $cur.Depth + 1 })
            } else {
                [void]$files.Add($it)
            }
        }
    }
    return [pscustomobject]@{ Ok = $true; Files = @($files) }
}

function Get-JsonTypeName([object]$value) {
    if ($null -eq $value) { return "null" }
    if ($value -is [bool]) { return "boolean" }
    if ($value -is [string]) { return "string" }
    if ($value -is [ValueType]) { return "number" }
    if ($value -is [System.Array]) { return "array" }
    if ($value -is [System.Management.Automation.PSCustomObject]) { return "object" }
    return $value.GetType().Name
}

# Strict JSON scanner: collects member names from every object frame and fully decodes JSON string
# escapes (including \uXXXX) before duplicate comparison. Duplicates fail closed per object, while
# the same key name may still appear in separate dependency objects. The scan also rejects illegal
# escapes, raw control characters, unterminated strings, unbalanced braces, and trailing commas.
# ConvertFrom-Json still performs the full grammar validation afterwards.
function Convert-JsonUnicodeEscape([string]$hex) {
    $code = 0
    for ($i = 0; $i -lt 4; $i++) {
        $c = $hex[$i]
        $v = -1
        if ($c -ge '0' -and $c -le '9') { $v = [int]$c - [int][char]'0' }
        elseif ($c -ge 'a' -and $c -le 'f') { $v = 10 + [int]$c - [int][char]'a' }
        elseif ($c -ge 'A' -and $c -le 'F') { $v = 10 + [int]$c - [int][char]'A' }
        else { throw ("invalid hex digit in unicode escape: {0}" -f $hex) }
        $code = ($code * 16) + $v
    }
    return [char]$code
}

function Get-TopLevelJsonScan([string]$text) {
    # Lex every JSON object, not only the manifest root. ConvertFrom-Json in Windows PowerShell
    # accepts duplicate keys in some nested objects, so duplicate detection must happen here for
    # every object frame before the compatibility parser runs. Keys are compared ordinal-ignore-case
    # because the consumer-side property lookup is case-insensitive.
    $keys = New-Object System.Collections.ArrayList
    $frames = New-Object System.Collections.ArrayList
    $first = -1
    for ($i = 0; $i -lt $text.Length; $i++) {
        if (-not [char]::IsWhiteSpace($text[$i])) { $first = $i; break }
    }
    if ($first -lt 0 -or $text[$first] -ne '{') {
        return [pscustomobject]@{ Keys = @(); Error = "root value is not a JSON object" }
    }
    $inString = $false
    $escaped = $false
    $isKey = $false
    $sb = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $text.Length; $i++) {
        $c = $text[$i]
        if ($inString) {
            if ($escaped) {
                $escaped = $false
                if ($isKey) {
                    if ($c -eq 'u') {
                        if ($i + 4 -ge $text.Length) { return [pscustomobject]@{ Keys = @(); Error = "short unicode escape in a JSON key" } }
                        $hex = $text.Substring($i + 1, 4)
                        try { [void]$sb.Append((Convert-JsonUnicodeEscape $hex)) }
                        catch { return [pscustomobject]@{ Keys = @(); Error = $_.Exception.Message } }
                        $i += 4
                    } elseif ($c -eq '"') { [void]$sb.Append('"') }
                    elseif ($c -eq '\') { [void]$sb.Append('\') }
                    elseif ($c -eq '/') { [void]$sb.Append('/') }
                    elseif ($c -eq 'b') { [void]$sb.Append([char]8) }
                    elseif ($c -eq 'f') { [void]$sb.Append([char]12) }
                    elseif ($c -eq 'n') { [void]$sb.Append([char]10) }
                    elseif ($c -eq 'r') { [void]$sb.Append([char]13) }
                    elseif ($c -eq 't') { [void]$sb.Append([char]9) }
                    else { return [pscustomobject]@{ Keys = @(); Error = ("invalid JSON escape '\{0}' in a JSON key" -f $c) } }
                }
                continue
            }
            if ($c -eq '\') { $escaped = $true; continue }
            if ($c -eq '"') {
                $inString = $false
                if ($isKey) {
                    $frame = $frames[$frames.Count - 1]
                    $key = $sb.ToString()
                    [void]$keys.Add($key)
                    if (-not $frame.Keys.Add($key)) {
                        return [pscustomobject]@{ Keys = @(); Error = ("duplicate JSON object key: {0}" -f $key) }
                    }
                    $frame.ExpectKey = $false
                    $frame.ExpectColon = $true
                    $isKey = $false
                }
                continue
            }
            if ([int][char]$c -lt 0x20) { return [pscustomobject]@{ Keys = @(); Error = "raw control character in a JSON string" } }
            if ($isKey) { [void]$sb.Append($c) }
            continue
        }
        if ($c -eq '"') {
            $inString = $true
            $escaped = $false
            if ($frames.Count -gt 0) {
                $frame = $frames[$frames.Count - 1]
                # A value string is an element: a comma before it was a separator, not a trailing
                # comma. A key string is different and is cleared by ':' below.
                if (-not ($frame.Kind -eq 'object' -and $frame.ExpectKey)) { $frame.LastComma = $false }
                if ($frame.Kind -eq 'object' -and $frame.ExpectKey) {
                    $isKey = $true
                    $sb = New-Object System.Text.StringBuilder
                } else {
                    $isKey = $false
                }
            } else {
                $isKey = $false
            }
            continue
        }
        if ($c -eq '{' -or $c -eq '[') {
            # A nested object/array is an element of its parent: the comma before it was a separator.
            if ($frames.Count -gt 0) { $frames[$frames.Count - 1].LastComma = $false }
            if ($c -eq '{') {
                $frame = [pscustomobject]@{
                    Kind = 'object'
                    ExpectKey = $true
                    ExpectColon = $false
                    LastComma = $false
                    Keys = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
                }
            } else {
                $frame = [pscustomobject]@{
                    Kind = 'array'
                    ExpectKey = $false
                    ExpectColon = $false
                    LastComma = $false
                    Keys = $null
                }
            }
            [void]$frames.Add($frame)
            continue
        }
        if ($c -eq '}' -or $c -eq ']') {
            if ($frames.Count -eq 0) {
                return [pscustomobject]@{ Keys = @(); Error = "unexpected closing JSON delimiter" }
            }
            $frame = $frames[$frames.Count - 1]
            if (($c -eq '}' -and $frame.Kind -ne 'object') -or ($c -eq ']' -and $frame.Kind -ne 'array')) {
                return [pscustomobject]@{ Keys = @(); Error = "mismatched JSON delimiter" }
            }
            if ($frame.LastComma) {
                return [pscustomobject]@{ Keys = @(); Error = "trailing comma before a JSON closing delimiter" }
            }
            [void]$frames.RemoveAt($frames.Count - 1)
            continue
        }
        if ($c -eq ':') {
            if ($frames.Count -gt 0) {
                $frame = $frames[$frames.Count - 1]
                if ($frame.Kind -eq 'object') {
                    $frame.ExpectColon = $false
                    $frame.ExpectKey = $false
                    $frame.LastComma = $false
                }
            }
            continue
        }
        if ($c -eq ',') {
            if ($frames.Count -gt 0) {
                $frame = $frames[$frames.Count - 1]
                $frame.LastComma = $true
                if ($frame.Kind -eq 'object') {
                    $frame.ExpectKey = $true
                    $frame.ExpectColon = $false
                }
            }
            continue
        }
        if (-not [char]::IsWhiteSpace($c) -and $frames.Count -gt 0) {
            $frames[$frames.Count - 1].LastComma = $false
        }
    }
    if ($inString) { return [pscustomobject]@{ Keys = @(); Error = "unterminated JSON string" } }
    if ($frames.Count -ne 0) { return [pscustomobject]@{ Keys = @(); Error = "unbalanced JSON braces/brackets" } }
    return [pscustomobject]@{ Keys = @($keys); Error = $null }
}
function Get-ManifestInfo([string]$path, [string]$item) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Finding $item "MISSING_MANIFEST" $path
        return $null
    }
    $raw = ""
    try {
        $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    } catch {
        Add-Finding $item "MANIFEST_PARSE_FAILED" ("{0}: {1}" -f $path, $_.Exception.Message)
        return $null
    }
    if (-not $raw) {
        Add-Finding $item "MANIFEST_EMPTY" ("{0}: file is empty" -f $path)
        return $null
    }
    $scan = Get-TopLevelJsonScan $raw
    if ($scan.Error) {
        $scanStatus = if ($scan.Error -like "duplicate JSON object key:*") { "MANIFEST_DUPLICATE_KEY" } else { "MANIFEST_PARSE_FAILED" }
        Add-Finding $item $scanStatus ("{0}: {1}" -f $path, $scan.Error)
        return $null
    }
    # Get-TopLevelJsonScan already rejects duplicates within every object frame. Do not run a
    # second global set here: valid dependency objects may each contain the same key name (for
    # example, every dependency object can have its own id).

    $json = $null
    try {
        $json = $raw | ConvertFrom-Json
    } catch {
        Add-Finding $item "MANIFEST_PARSE_FAILED" ("{0}: {1}" -f $path, $_.Exception.Message)
        return $null
    }
    if ($null -eq $json -or -not ($json -is [System.Management.Automation.PSCustomObject])) {
        Add-Finding $item "MANIFEST_PARSE_FAILED" ("{0}: the root value is not a JSON object" -f $path)
        return $null
    }

    # id/version must be present and be JSON STRINGS: a numeric version is not a version string, and
    # coercing it with [string] would silently accept a manifest the loader may reject.
    $idProp = $json.PSObject.Properties['id']
    if ($null -eq $idProp) {
        Add-Finding $item "MANIFEST_INCOMPLETE" ("{0}: id is missing" -f $path)
        return $null
    }
    if (-not ($idProp.Value -is [string]) -or -not $idProp.Value.Trim()) {
        Add-Finding $item "MANIFEST_ID_TYPE" ("{0}: id must be a non-empty JSON string" -f $path)
        return $null
    }
    $verProp = $json.PSObject.Properties['version']
    if ($null -eq $verProp) {
        Add-Finding $item "MANIFEST_INCOMPLETE" ("{0}: version is missing" -f $path)
        return $null
    }
    if (-not ($verProp.Value -is [string]) -or -not $verProp.Value.Trim()) {
        Add-Finding $item "MANIFEST_VERSION_TYPE" ("{0}: version must be a non-empty JSON string" -f $path)
        return $null
    }

    # has_pck/has_dll must EXIST and be JSON booleans. The old code used [bool]$json.has_pck, which
    # turned a missing field into $false and the string "false" into $true - both silent contract
    # breaks, so anything but a real boolean fails closed here.
    $pckProp = $json.PSObject.Properties['has_pck']
    if ($null -eq $pckProp) {
        Add-Finding $item "MANIFEST_HAS_PCK_MISSING" ("{0}: has_pck is missing" -f $path)
        return $null
    }
    if (-not ($pckProp.Value -is [bool])) {
        Add-Finding $item "MANIFEST_HAS_PCK_TYPE" ("{0}: has_pck must be a JSON boolean (got {1})" -f $path, (Get-JsonTypeName $pckProp.Value))
        return $null
    }
    $dllProp = $json.PSObject.Properties['has_dll']
    if ($null -eq $dllProp) {
        Add-Finding $item "MANIFEST_HAS_DLL_MISSING" ("{0}: has_dll is missing" -f $path)
        return $null
    }
    if (-not ($dllProp.Value -is [bool])) {
        Add-Finding $item "MANIFEST_HAS_DLL_TYPE" ("{0}: has_dll must be a JSON boolean (got {1})" -f $path, (Get-JsonTypeName $dllProp.Value))
        return $null
    }

    return [pscustomobject]@{
        Id = $idProp.Value.Trim()
        Version = $verProp.Value.Trim()
        HasPck = [bool]$pckProp.Value
        HasDll = [bool]$dllProp.Value
    }
}

# Manifest-declared publish allowlist for one row. The manifest is the authority on WHICH artifacts
# ship: has_dll/has_pck decide whether the DLL/PCK are REQUIRED. The pdb is an optional payload (it
# ships when the build produced it), and the digest is a record-only file that is never uploaded.
function Get-RowAllowlist([object]$row, [object]$info, [string]$manifestLeaf) {
    $base = [System.IO.Path]::GetFileNameWithoutExtension((Split-Path -Path $row.Build -Leaf))
    $entries = New-Object System.Collections.ArrayList
    [void]$entries.Add([pscustomobject]@{ Role = "manifest"; Leaf = $manifestLeaf;                  Required = $true;              Payload = $true })
    [void]$entries.Add([pscustomobject]@{ Role = "dll";      Leaf = ("{0}.dll" -f $base);           Required = [bool]$info.HasDll; Payload = $true })
    [void]$entries.Add([pscustomobject]@{ Role = "pck";      Leaf = ("{0}.pck" -f $base);           Required = [bool]$info.HasPck; Payload = $true })
    # Row policy (2026-10-05): a row may set PublishPdb=$false to omit the pdb from its publish
    # allowlist entirely. Then neither copy direction can introduce it (direction 1 looks every
    # staged file up in this list; direction 2 only creates entries listed here) and a staged
    # <mod>.pdb is a hard STAGED_FILE_NOT_ALLOWLISTED finding, never silently deleted. Rows
    # without the key keep the historical optional-pdb behavior (default $true).
    $publishPdb = $true
    if ($row.ContainsKey('PublishPdb')) { $publishPdb = [bool]$row.PublishPdb }
    if ($publishPdb) {
        [void]$entries.Add([pscustomobject]@{ Role = "pdb";  Leaf = ("{0}.pdb" -f $base);           Required = $false;             Payload = $true })
    }
    [void]$entries.Add([pscustomobject]@{ Role = "digest";   Leaf = ("{0}.pck.sha256" -f $base);    Required = $false;             Payload = $false })
    return ,$entries
}

function Invoke-AllowlistReconciliation([object]$row, [object]$info, [string]$stageDir, [string]$buildDir, [string]$manifestLeaf) {
    $allow = Get-RowAllowlist $row $info $manifestLeaf
    $allowLeaves = New-Object System.Collections.ArrayList
    foreach ($a in $allow) { [void]$allowLeaves.Add($a.Leaf) }
    $base = [System.IO.Path]::GetFileNameWithoutExtension((Split-Path -Path $row.Build -Leaf))

    # Direction 1 (staging -> allowlist): the staging tree may contain ONLY allowlisted files, so
    # nothing can upload that this script cannot trace back to a build artifact. Nested paths are not
    # allowed today: every published row is a flat JSON/DLL/PCK/PDB payload. Enumeration failures and
    # reparse points are HARD failures: an unreadable or redirected staging tree is never "empty".
    $stageList = Get-FileListChecked $row.Name $stageDir "staging"
    if (-not $stageList.Ok) { return }
    foreach ($staged in $stageList.Files) {
        $rel = $staged.FullName.Substring($stageDir.Length + 1)
        if ($rel -match '[\\/]') {
            Add-Finding $row.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (nested path; the publish allowlist is flat)" -f $rel)
            continue
        }
        if (-not ($allowLeaves -icontains $rel)) {
            Add-Finding $row.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (not named by the manifest-declared publish allowlist)" -f $rel)
        }
    }

    # Direction 2 (allowlist -> staging, per SHA256): every REQUIRED artifact must exist in the build
    # output and in staging with identical bytes. Nothing here reads the repo source tree. Any hash
    # that cannot be computed is a hard finding; two failures never compare as equal bytes.
    $buildList = Get-FileListChecked $row.Name $buildDir "build"
    if (-not $buildList.Ok) { return }
    foreach ($a in $allow) {
        if ($a.Role -eq "manifest") { continue }
        $buildPath = Join-Path $buildDir $a.Leaf
        $stagedPath = Join-Path $stageDir $a.Leaf
        $inBuild = Test-Path -LiteralPath $buildPath -PathType Leaf
        $inStage = Test-Path -LiteralPath $stagedPath -PathType Leaf

        if ($a.Payload) {
            if (-not $inBuild) {
                if ($a.Required) {
                    Add-Finding $row.Name "MISSING_BUILD_ARTIFACT" ("{0} ({1} is required by the manifest but is absent from the build output)" -f $a.Leaf, $a.Role)
                } elseif ($inStage) {
                    Add-Finding $row.Name "MISSING_BUILD_FILE" $a.Leaf
                }
                continue
            }
            if (-not $inStage) {
                if ($a.Required) {
                    if ($a.Role -eq "pck") {
                        Add-Finding $row.Name "MANIFEST_PCK_MISSING" ("{0}: manifest has_pck=true but the staged payload has no {0}" -f $a.Leaf)
                    } elseif ($a.Role -eq "dll") {
                        Add-Finding $row.Name "MANIFEST_DLL_MISSING" ("{0}: manifest has_dll=true but the staged payload has no {0}" -f $a.Leaf)
                    } else {
                        Add-Finding $row.Name "MISSING_STAGED_FILE" ("{0} ({1} is required but is absent from staging)" -f $a.Leaf, $a.Role)
                    }
                }
                continue
            }
            $stagedHash = Hash-Of $stagedPath
            $buildHash = Hash-Of $buildPath
            if ($null -eq $stagedHash -or $null -eq $buildHash) {
                Add-Finding $row.Name "HASH_UNREADABLE" ("{0}: staged={1} build={2}" -f $a.Leaf, (Short-Hash $stagedHash), (Short-Hash $buildHash))
                continue
            }
            if ($a.Role -eq "pck") {
                # Provenance for a shipped PCK: nonzero length, produced no earlier than the DLL it
                # accompanies, and staged as the exact same bytes AND the same timestamp.
                try {
                    $buildPckItem = Get-Item -LiteralPath $buildPath -Force -ErrorAction Stop
                    $stagedPckItem = Get-Item -LiteralPath $stagedPath -Force -ErrorAction Stop
                    if ($buildPckItem.Length -eq 0) {
                        Add-Finding $row.Name "PCK_EMPTY" ("{0}: build PCK is zero bytes" -f $a.Leaf)
                    }
                    if ($stagedPckItem.Length -ne $buildPckItem.Length) {
                        Add-Finding $row.Name "PCK_LENGTH_MISMATCH" ("{0}: staged {1} bytes vs build {2} bytes" -f $a.Leaf, $stagedPckItem.Length, $buildPckItem.Length)
                    }
                    if ($stagedPckItem.LastWriteTimeUtc -ne $buildPckItem.LastWriteTimeUtc) {
                        Add-Finding $row.Name "PCK_MTIME_MISMATCH" ("{0}: staged {1:o} vs build {2:o}" -f $a.Leaf, $stagedPckItem.LastWriteTimeUtc, $buildPckItem.LastWriteTimeUtc)
                    }
                    $buildDllPath = Join-Path $buildDir ("{0}.dll" -f $base)
                    if (Test-Path -LiteralPath $buildDllPath -PathType Leaf) {
                        $buildDllItem = Get-Item -LiteralPath $buildDllPath -Force -ErrorAction Stop
                        if ($buildPckItem.LastWriteTimeUtc -lt $buildDllItem.LastWriteTimeUtc) {
                            Add-Finding $row.Name "PCK_STALE" ("{0}: build PCK mtime {1:o} is older than the build DLL mtime {2:o}" -f $a.Leaf, $buildPckItem.LastWriteTimeUtc, $buildDllItem.LastWriteTimeUtc)
                        }
                    }
                } catch {
                    Add-Finding $row.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $a.Leaf, $buildPath, $_.Exception.Message)
                }
            }
            if ($stagedHash -ne $buildHash) {
                Add-Finding $row.Name "SHA_MISMATCH" ("{0}: staged {1} vs build {2}" -f $a.Leaf, (Short-Hash $stagedHash), (Short-Hash $buildHash))
            }
            continue
        }

        # Record-only artifact (digest): never a payload file. For has_pck=true it is REQUIRED: it
        # binds the build PCK to a producer result, so a missing/empty/malformed/mismatched record is
        # a hard finding, not a note. For has_pck=false the record must not exist at all.
        if ($inStage) {
            Add-Finding $row.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (record-only artifact must not be staged)" -f $a.Leaf)
        }
        if ($inBuild) {
            $pckPath = Join-Path $buildDir ("{0}.pck" -f $base)
            if (-not $info.HasPck) {
                Add-Finding $row.Name "DIGEST_WITHOUT_PCK" ("{0} is present although the manifest declares has_pck=false" -f $a.Leaf)
            } elseif (-not (Test-Path -LiteralPath $pckPath -PathType Leaf)) {
                Add-Finding $row.Name "DIGEST_WITHOUT_PCK" ("{0} is present but {1} is missing" -f $a.Leaf, ("{0}.pck" -f $base))
            } else {
                $recorded = $null
                try {
                    $recorded = (Get-Content -LiteralPath $buildPath -Raw -ErrorAction Stop).Trim()
                } catch {
                    Add-Finding $row.Name "DIGEST_UNREADABLE" ("{0}: {1}" -f $buildPath, $_.Exception.Message)
                }
                if ($null -ne $recorded) {
                    if (-not $recorded) {
                        Add-Finding $row.Name "DIGEST_UNREADABLE" ("{0}: digest record is empty" -f $buildPath)
                    } elseif ($recorded -notmatch '^[0-9A-Fa-f]{64}$') {
                        Add-Finding $row.Name "DIGEST_MALFORMED" ("{0}: digest record is not a 64-hex SHA256" -f $buildPath)
                    } else {
                        $actual = Hash-Of $pckPath
                        if ($null -eq $actual) {
                            Add-Finding $row.Name "HASH_UNREADABLE" ("{0}: cannot hash {1}" -f $a.Leaf, $pckPath)
                        } elseif ($recorded.ToUpperInvariant() -ne $actual) {
                            Add-Finding $row.Name "DIGEST_MISMATCH" ("{0}: record {1} vs pck {2}" -f $a.Leaf, (Short-Hash $recorded.ToUpperInvariant()), (Short-Hash $actual))
                        }
                        try {
                            $pckItem = Get-Item -LiteralPath $pckPath -Force -ErrorAction Stop
                            $digestItem = Get-Item -LiteralPath $buildPath -Force -ErrorAction Stop
                            if ($pckItem.Length -eq 0) {
                                Add-Finding $row.Name "PCK_EMPTY" ("{0}: build PCK is zero bytes" -f $pckPath)
                            }
                            if ($digestItem.LastWriteTimeUtc -lt $pckItem.LastWriteTimeUtc) {
                                Add-Finding $row.Name "DIGEST_STALE" ("{0}: digest mtime {1:o} predates the PCK mtime {2:o}" -f $a.Leaf, $digestItem.LastWriteTimeUtc, $pckItem.LastWriteTimeUtc)
                            }
                        } catch {
                            Add-Finding $row.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $a.Leaf, $buildPath, $_.Exception.Message)
                        }
                    }
                }
            }
        } elseif ($info.HasPck) {
            Add-Finding $row.Name "PCK_DIGEST_MISSING" ("{0} is absent from the build output although the manifest declares has_pck=true (the producer must write {0} next to the PCK)" -f $a.Leaf)
        }
    }

    # The manifest decides whether the DLL/PCK may ship at all: a file the manifest says is not part
    # of the mod must not be published as if it were.
    if (-not $info.HasDll) {
        $dllLeaf = ("{0}.dll" -f $base)
        if (Test-Path -LiteralPath (Join-Path $stageDir $dllLeaf) -PathType Leaf) {
            Add-Finding $row.Name "MANIFEST_DLL_UNEXPECTED" ("manifest has_dll=false but {0} is staged" -f $dllLeaf)
        }
    }
    if (-not $info.HasPck) {
        $pckLeaf = ("{0}.pck" -f $base)
        if (Test-Path -LiteralPath (Join-Path $stageDir $pckLeaf) -PathType Leaf) {
            Add-Finding $row.Name "MANIFEST_PCK_UNEXPECTED" ("manifest has_pck=false but {0} is staged" -f $pckLeaf)
        }
    }
}

function Get-ContentFolder([string]$vdfPath, [string]$item) {
    if (-not (Test-Path -LiteralPath $vdfPath -PathType Leaf)) {
        Add-Finding $item "MISSING_VDF" $vdfPath
        return $null
    }
    try {
        $raw = Get-Content -LiteralPath $vdfPath -Raw -Encoding UTF8 -ErrorAction Stop
    } catch {
        Add-Finding $item "VDF_UNREADABLE" ("{0}: {1}" -f $vdfPath, $_.Exception.Message)
        return $null
    }
    $m = [regex]::Match($raw, '"contentfolder"[ \t]*"([^"]*)"')
    if (-not $m.Success) {
        Add-Finding $item "VDF_CONTENTFOLDER_MISSING" $vdfPath
        return $null
    }
    $value = $m.Groups[1].Value -replace '\\\\', '\'
    if (Test-RawPathHasDotDot $value) {
        Add-Finding $item "VDF_CONTENTFOLDER_DOTDOT" ("{0}: {1}" -f $vdfPath, $value)
        return $null
    }
    if (-not [System.IO.Path]::IsPathRooted($value)) {
        Add-Finding $item "VDF_CONTENTFOLDER_NOT_ROOTED" ("{0}: {1}" -f $vdfPath, $value)
        return $null
    }
    try {
        $cfFull = Full-Path $value
    } catch {
        Add-Finding $item "VDF_CONTENTFOLDER_INVALID" ("{0}: {1}" -f $vdfPath, $_.Exception.Message)
        return $null
    }
    return $cfFull
}
function Invoke-Verification([object[]]$verifyRows, [bool]$checkFreshness) {
    foreach ($it in $verifyRows) {
        $repoDir = Full-Path (Join-Path $root $it.Repo)
        if (-not (Test-Path -LiteralPath $repoDir -PathType Container)) {
            Add-Finding $it.Name "MISSING_REPO" $repoDir
            continue
        }
        $stageDll = Full-Path (Join-Path $repoDir $it.Stage)
        $stageDir = (Full-Path (Split-Path -Path $stageDll -Parent)).TrimEnd('\')
        $stageRoot = (Split-Path -Path $stageDir -Parent).TrimEnd('\')
        $buildDll = Full-Path (Join-Path $repoDir $it.Build)
        $buildDir = (Full-Path (Split-Path -Path $buildDll -Parent)).TrimEnd('\')
        $srcManifest = Full-Path (Join-Path $repoDir $it.Manifest)
        $vdfPath = Full-Path (Join-Path $repoDir $it.Vdf)

        foreach ($pair in @(
            @{ P = $stageDir; L = "stage" },
            @{ P = $buildDir; L = "build" },
            @{ P = $srcManifest; L = "manifest" },
            @{ P = $vdfPath; L = "vdf" })) {
            if (Test-RawPathHasDotDot $pair.P) {
                Add-Finding $it.Name "PATH_DOTDOT" ("{0}: {1}" -f $pair.L, $pair.P)
            }
            if (-not (Test-PathInside $pair.P $repoDir)) {
                Add-Finding $it.Name "PATH_ESCAPES_REPO" ("{0}: {1}" -f $pair.L, $pair.P)
            }
            try {
                if (Test-ReparseChain $pair.P) {
                    Add-Finding $it.Name "PATH_REPARSE_POINT" ("{0}: {1}" -f $pair.L, $pair.P)
                }
            } catch {
                Add-Finding $it.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $pair.L, $pair.P, $_.Exception.Message)
            }
            try {
                if (Test-SteamMarkerAncestor $pair.P) {
                    Add-Finding $it.Name "PATH_STEAM_MARKER" ("{0}: {1}" -f $pair.L, $pair.P)
                }
            } catch {
                Add-Finding $it.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $pair.L, $pair.P, $_.Exception.Message)
            }
        }
        $stageDirList = Get-FileListChecked $it.Name $stageDir "staging (verify)"
        if (-not $stageDirList.Ok) { continue }
        $buildDirList = Get-FileListChecked $it.Name $buildDir "build (verify)"
        if (-not $buildDirList.Ok) { continue }


        if (-not (Test-Path -LiteralPath $stageDir -PathType Container)) {
            Add-Finding $it.Name "MISSING_STAGE_DIR" $stageDir
            continue
        }
        if (-not (Test-Path -LiteralPath $buildDir -PathType Container)) {
            Add-Finding $it.Name "MISSING_BUILD_DIR" $buildDir
            continue
        }
        $cf = Get-ContentFolder $vdfPath $it.Name
        if ($cf) {
            if (-not (Test-PathInside $cf $repoDir)) {
                Add-Finding $it.Name "VDF_CONTENTFOLDER_ESCAPES_REPO" $cf
            }
            if ((Full-Path $cf).TrimEnd('\') -ine $stageRoot) {
                Add-Finding $it.Name "VDF_CONTENTFOLDER_MISMATCH" ("vdf={0} expected={1}" -f $cf, $stageRoot)
            }
        }

        $srcInfo = Get-ManifestInfo $srcManifest $it.Name
        $manifestLeaf = Split-Path -Path $it.Manifest -Leaf
        $stageManifest = Join-Path $stageDir $manifestLeaf
        $stageInfo = Get-ManifestInfo $stageManifest $it.Name
        if ($srcInfo) {
            if ($srcInfo.Id -ne $it.Id) {
                Add-Finding $it.Name "MANIFEST_ID_MISMATCH" ("source id={0} expected={1}" -f $srcInfo.Id, $it.Id)
            }
            if ($stageInfo) {
                if ($stageInfo.Id -ne $srcInfo.Id) {
                    Add-Finding $it.Name "MANIFEST_ID_MISMATCH" ("staged id={0} source={1}" -f $stageInfo.Id, $srcInfo.Id)
                }
                if ($stageInfo.Version -ne $srcInfo.Version) {
                    Add-Finding $it.Name "MANIFEST_VERSION_MISMATCH" ("source={0} staged={1}" -f $srcInfo.Version, $stageInfo.Version)
                }
                $srcHash = Hash-Of $srcManifest
                $stageHash = Hash-Of $stageManifest
                if ($null -eq $srcHash -or $null -eq $stageHash) {
                    Add-Finding $it.Name "HASH_UNREADABLE" ("manifest hash unavailable: source={0} staged={1}" -f (Short-Hash $srcHash), (Short-Hash $stageHash))
                } elseif ($srcHash -ne $stageHash) {
                    Add-Finding $it.Name "MANIFEST_CONTENT_MISMATCH" ("source {0} vs staged {1}" -f (Short-Hash $srcHash), (Short-Hash $stageHash))
                }

            }
            # The manifest-declared publish allowlist, reconciled in BOTH directions with SHA256. The
            # old staged -> build pass could not see a required payload file that was missing from
            # staging, and the old copy pass could satisfy a staged file from the repo source tree.
            Invoke-AllowlistReconciliation $it $srcInfo $stageDir $buildDir $manifestLeaf
        }

        if ($checkFreshness) {
            $buildFile = Join-Path $repoDir $it.Build
            if (Test-Path -LiteralPath $buildFile -PathType Leaf) {
                try {
                    $buildTime = (Get-Item -LiteralPath $buildFile -Force -ErrorAction Stop).LastWriteTimeUtc
                    $exts = @("*.cs", "*.csproj", "*.props", "*.json")
                    $newest = $null
                    foreach ($ext in $exts) {
                        $found = @(Get-ChildItem -LiteralPath $repoDir -Filter $ext -Recurse -File -ErrorAction Stop |
                            Where-Object {
                                # Exclusions match the repo-relative path: an absolute FullName lets an ancestor
                                # segment (e.g. a .tmp isolation root) swallow every input. Root docs/**/*.json
                                # is review evidence, not a production input; docs .cs/.csproj/.props stay scanned.
                                $rel = $_.FullName.Substring($repoDir.Length + 1)
                                ('\' + $rel) -notmatch '[\\/](obj|bin|\.godot|node_modules|\.tmp|\.nuget|\.dotnethome|research|tools)[\\/]' -and
                                -not ($rel -imatch '^docs[\\/].*\.json$') -and
                                $_.LastWriteTimeUtc -gt $buildTime -and
                                (-not (Test-HeldBackSource $_.FullName))
                            } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
                        if ($found -and ((-not $newest) -or $found.LastWriteTimeUtc -gt $newest.LastWriteTimeUtc)) { $newest = $found }
                    }
                    if ($newest) {
                        $rel = $newest.FullName.Substring($repoDir.Length + 1)
                        Add-Finding $it.Name "REBUILD_REQUIRED" ("{0} is newer than the build output (source {1} vs build {2})" -f $rel, $newest.LastWriteTimeUtc.ToString('yyyy-MM-dd HH:mm'), $buildTime.ToString('yyyy-MM-dd HH:mm'))
                    }
                } catch {
                    Add-Finding $it.Name "FRESHNESS_SCAN_FAILED" ("{0}: {1}" -f $repoDir, $_.Exception.Message)
                }
            }
        }
    }
}


# ---- Held-back layers: declared ONCE, used by both the pre-copy gate and the content assert -----
# A layer listed here is deliberately NOT part of the shipped payload. Both are IMPLEMENTED - they
# are held back because each needs evidence this release cannot produce (AFTP-2 rewrites third-party
# IL in AFTP's render path and needs a native A/B; the card-state layer needs an in-engine run), so
# shipping them would put an unverified change on the Workshop through a one-way door. "Held back"
# is a decision about evidence, not about whether the code exists.
#
# INVARIANT: "held back" means NOT COMPILED INTO THE PAYLOAD - not "not wired". MainFile's Phase3
# scan applies every type carrying [HarmonyPatch] unconditionally (MainFile.cs:113-124), so an
# attribute-declared patch goes LIVE the moment it compiles, with no TryApply call at all. These
# layers use the manual-install shape instead (0 [HarmonyPatch] attributes; they call
# harmony.Patch inside TryApply, which MainFile must invoke), which is why they are inert until
# wired - but never rely on that. The marker scan below reads the COMPILED BYTES, so it catches the
# attribute case too.
#
# To RELEASE a layer: delete its entry here in the same commit that wires its TryApply call, and
# update docs/PUSH-INSTRUCTIONS-2026-09-15.md.
$heldBack = @(
    @{ Name = "Spire1"; Dll = "sts2-spire1\workshop\content\Spire1\Spire1.dll"
       Build = "sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll"
       Sources = @("sts2-spire1\mod\Spire1Code\Interop\AftpFireFlyPerfCompat.cs",
                   "sts2-spire1\mod\Spire1Code\Interop\AftpCardStateCompat.cs")
       Markers = @("AftpFireFlyPerfCompat", "AftpCardStateCompat") }
)

# Absolute paths of every held-back source, for the freshness scan's exclusion test.
$heldBackSources = New-Object System.Collections.ArrayList
foreach ($h in $heldBack) {
    foreach ($s in $h.Sources) { [void]$heldBackSources.Add((Join-Path $root $s)) }
}
function Test-HeldBackSource([string]$fullPath) {
    return ($heldBackSources -contains $fullPath)
}

function Get-MarkerLeaks([string]$dllPath, [string]$itemName, [string]$where) {
    $found = New-Object System.Collections.ArrayList
    if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) { return $found }
    try {
        $text = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($dllPath))
    } catch {
        Add-Finding $itemName "MARKER_SCAN_FAILED" ("{0}: {1} ({2})" -f $where, $dllPath, $_.Exception.Message)
        return $found
    }
    foreach ($h in $heldBack) {
        if ($h.Name -ne $itemName) { continue }
        foreach ($m in $h.Markers) {
            if ($text.Contains($m)) {
                [void]$found.Add([pscustomobject]@{
                    Item   = $itemName
                    Status = "HELD_BACK_LAYER_PRESENT"
                    Detail = "$m is present in the $where"
                })
            }
        }
    }
    return $found
}
# ---- PRE-COPY GATE: refuse to touch staging when a build output is polluted ---------------------
# This runs BEFORE the copy phase, and that ordering is the whole point. On 2026-09-15 a gate-open
# verification build (IncludeHeldBackLayers=true) left AftpCardStateCompat in the Release output,
# the copy loop faithfully wrote those polluted bytes OVER a clean staging tree, and the only signal
# the operator got was REBUILD_REQUIRED. The refresh is supposed to be safe to run at any time; a
# check that can only report after it has already propagated the bad bytes is not a check.
# ---- PRE-WRITE PATH GATE (A10, 2026-10-02) --------------------------------------------------------
# Runs BEFORE any Copy/Delete/WriteLines/digest/manifest write and also before the verify-only read
# phase. Every final path (repo, manifest, build dir, stage dir, VDF) must be inside the validated
# root, free of dot-dot segments, readable, and free of junction/symlink at EVERY level including
# the leaf directories themselves. A single failure stops the run before the first write.
# -WhatIf and -VerifyOnly keep their no-write semantics: this gate only reads.
$preWriteFailed = $false
foreach ($it in $rows) {
    $repoDir = Get-NormalizedDir (Join-Path $root $it.Repo)
    $srcManifest = Get-NormalizedDir (Join-Path $repoDir $it.Manifest)
    $buildDll = Get-NormalizedDir (Join-Path $repoDir $it.Build)
    $buildDir = Get-NormalizedDir (Split-Path -Path $buildDll -Parent)
    $stageDll = Get-NormalizedDir (Join-Path $repoDir $it.Stage)
    $stageDir = Get-NormalizedDir (Split-Path -Path $stageDll -Parent)
    $vdfPath = Get-NormalizedDir (Join-Path $repoDir $it.Vdf)

    foreach ($pair in @(
        @{ P = $repoDir; L = "repo" },
        @{ P = $srcManifest; L = "manifest" },
        @{ P = $buildDir; L = "build" },
        @{ P = $stageDir; L = "stage" },
        @{ P = $vdfPath; L = "vdf" })) {
        if (Test-RawPathHasDotDot $pair.P) {
            Add-Finding $it.Name "PATH_DOTDOT" ("{0}: {1}" -f $pair.L, $pair.P)
            $preWriteFailed = $true
            continue
        }
        if (-not (Test-PathInside $pair.P $root)) {
            Add-Finding $it.Name "PATH_ESCAPES_REPO" ("{0}: {1}" -f $pair.L, $pair.P)
            $preWriteFailed = $true
            continue
        }
        try {
            if (Test-ReparseChain $pair.P) {
                Add-Finding $it.Name "PATH_REPARSE_POINT" ("{0}: {1}" -f $pair.L, $pair.P)
                $preWriteFailed = $true
            }
        } catch {
            Add-Finding $it.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $pair.L, $pair.P, $_.Exception.Message)
            $preWriteFailed = $true
        }
        try {
            if (Test-SteamMarkerAncestor $pair.P) {
                Add-Finding $it.Name "PATH_STEAM_MARKER" ("{0}: {1}" -f $pair.L, $pair.P)
                $preWriteFailed = $true
            }
        } catch {
            Add-Finding $it.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $pair.L, $pair.P, $_.Exception.Message)
            $preWriteFailed = $true
        }
    }

    # Validate the VDF contentfolder pairing BEFORE any copy. A VDF file can be readable and still
    # point at a different directory, so checking only the VDF path is not enough: the exact upload
    # target must be the stage root that this row is about to refresh, and that target must pass the
    # same root/reparse/Steam checks before the first write.
    $preContentFolder = Get-ContentFolder $vdfPath $it.Name
    if (-not $preContentFolder) {
        $preWriteFailed = $true
    } else {
        $stageRoot = (Split-Path -Path $stageDir -Parent).TrimEnd('\')
        if (-not (Test-PathInside $preContentFolder $root)) {
            Add-Finding $it.Name "VDF_CONTENTFOLDER_ESCAPES_REPO" $preContentFolder
            $preWriteFailed = $true
        }
        if ((Get-NormalizedDir $preContentFolder) -ine (Get-NormalizedDir $stageRoot)) {
            Add-Finding $it.Name "VDF_CONTENTFOLDER_MISMATCH" ("vdf={0} expected={1}" -f $preContentFolder, $stageRoot)
            $preWriteFailed = $true
        }
        try {
            if (Test-ReparseChain $preContentFolder) {
                Add-Finding $it.Name "PATH_REPARSE_POINT" ("VDF contentfolder: {0}" -f $preContentFolder)
                $preWriteFailed = $true
            }
            if (Test-SteamMarkerAncestor $preContentFolder) {
                Add-Finding $it.Name "PATH_STEAM_MARKER" ("VDF contentfolder: {0}" -f $preContentFolder)
                $preWriteFailed = $true
            }
        } catch {
            Add-Finding $it.Name "PATH_UNREADABLE" ("VDF contentfolder: {0} ({1})" -f $preContentFolder, $_.Exception.Message)
            $preWriteFailed = $true
        }
    }

    # Enumerate both payload trees now, with reparse/read checks at every directory level. This is
    # what makes "unreadable" and "junction" hard failures BEFORE the copy phase starts.
    $stagePre = Get-FileListChecked $it.Name $stageDir "staging (pre-write)"
    if (-not $stagePre.Ok) { $preWriteFailed = $true }
    $buildPre = Get-FileListChecked $it.Name $buildDir "build (pre-write)"
    if (-not $buildPre.Ok) { $preWriteFailed = $true }
}
if ($preWriteFailed) {
    Write-Output ""
    Write-Output "PRE-WRITE PATH GATE: refusing to touch staging or build paths (no file was written):"
    $findings | Format-Table -AutoSize
    exit 1
}


$preLeaks = New-Object System.Collections.ArrayList
foreach ($h in $heldBack) {
    $buildDll = Join-Path $root $h.Build
    foreach ($l in (Get-MarkerLeaks $buildDll $h.Name "BUILD OUTPUT (before the copy)")) {
        [void]$preLeaks.Add($l)
    }
}
if ($preLeaks.Count -gt 0) {
    Write-Output "PRE-COPY GATE: a held-back layer is compiled into a build output - staging NOT touched:"
    $preLeaks | Format-Table -AutoSize
    Write-Output "The build output is polluted (most likely a -p:IncludeHeldBackLayers=true verification"
    Write-Output "build). Rebuild with the gate CLOSED (no -p:IncludeHeldBackLayers=true), then re-run."
    Write-Output "Nothing was copied, so the staging tree still holds whatever was verified last."
    exit 1
}

# ---- PRE-COPY ARTIFACT GATE -----------------------------------------------------------------------
# Path safety alone is not enough: a zero-byte/stale/undigested build artifact must not be copied
# before the later reconciliation discovers the problem. This gate validates every source that the
# copy phase could touch, while allowing an existing staged payload to differ (that is the purpose
# of refresh). It therefore blocks bad build provenance and unreadable files before the first write,
# but still permits a clean build to repair stale staged bytes.
$preCopyArtifactFailed = $false
foreach ($it in $rows) {
    $repoDir = Full-Path (Join-Path $root $it.Repo)
    $srcManifest = Full-Path (Join-Path $repoDir $it.Manifest)
    $buildDll = Full-Path (Join-Path $repoDir $it.Build)
    $buildDir = (Full-Path (Split-Path -Path $buildDll -Parent)).TrimEnd('\')
    $stageDll = Full-Path (Join-Path $repoDir $it.Stage)
    $stageDir = (Full-Path (Split-Path -Path $stageDll -Parent)).TrimEnd('\')

    if (-not (Test-Path -LiteralPath $buildDir -PathType Container)) {
        Add-Finding $it.Name "MISSING_BUILD_DIR" $buildDir
        $preCopyArtifactFailed = $true
        continue
    }
    if (-not (Test-Path -LiteralPath $stageDir -PathType Container)) {
        Add-Finding $it.Name "MISSING_STAGE_DIR" $stageDir
        $preCopyArtifactFailed = $true
        continue
    }

    $info = Get-ManifestInfo $srcManifest $it.Name
    if (-not $info) {
        $preCopyArtifactFailed = $true
        continue
    }
    $manifestLeaf = Split-Path -Path $it.Manifest -Leaf
    $allow = Get-RowAllowlist $it $info $manifestLeaf
    $allowLeaves = @($allow | ForEach-Object { $_.Leaf })

    $stageList = Get-FileListChecked $it.Name $stageDir "staging (pre-copy artifact gate)"
    $buildList = Get-FileListChecked $it.Name $buildDir "build (pre-copy artifact gate)"
    if (-not $stageList.Ok -or -not $buildList.Ok) {
        $preCopyArtifactFailed = $true
        continue
    }

    # Any staged file that the copy loop would inspect must be readable before the first write.
    foreach ($staged in $stageList.Files) {
        $rel = $staged.FullName.Substring($stageDir.Length + 1)
        if ($rel -match '[\\/]') {
            Add-Finding $it.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (nested path; the publish allowlist is flat)" -f $rel)
            $preCopyArtifactFailed = $true
            continue
        }
        if (-not ($allowLeaves -icontains $rel)) {
            Add-Finding $it.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (not named by the manifest-declared publish allowlist)" -f $rel)
            $preCopyArtifactFailed = $true
            continue
        }
        $entry = @($allow | Where-Object { $_.Leaf -ieq $rel })
        if ($entry.Count -eq 0 -or -not $entry[0].Payload) {
            Add-Finding $it.Name "STAGED_FILE_NOT_ALLOWLISTED" ("{0} (record-only artifact must not be staged)" -f $rel)
            $preCopyArtifactFailed = $true
            continue
        }
        $stagedHash = Hash-Of $staged.FullName
        if ($null -eq $stagedHash) {
            Add-Finding $it.Name "HASH_UNREADABLE" ("staging artifact unreadable before copy: {0}" -f $staged.FullName)
            $preCopyArtifactFailed = $true
        }
    }

    $srcManifestHash = Hash-Of $srcManifest
    if ($null -eq $srcManifestHash) {
        Add-Finding $it.Name "HASH_UNREADABLE" ("source manifest unreadable before copy: {0}" -f $srcManifest)
        $preCopyArtifactFailed = $true
    }
    $stageManifest = Join-Path $stageDir $manifestLeaf
    if (Test-Path -LiteralPath $stageManifest -PathType Leaf) {
        if ($null -eq (Hash-Of $stageManifest)) {
            Add-Finding $it.Name "HASH_UNREADABLE" ("staged manifest unreadable before copy: {0}" -f $stageManifest)
            $preCopyArtifactFailed = $true
        }
    }

    foreach ($a in $allow) {
        if ($a.Role -eq "manifest") { continue }
        $buildPath = Join-Path $buildDir $a.Leaf
        $stagedPath = Join-Path $stageDir $a.Leaf
        $inBuild = Test-Path -LiteralPath $buildPath -PathType Leaf
        $inStage = Test-Path -LiteralPath $stagedPath -PathType Leaf

        if ($a.Required -and -not $inBuild) {
            Add-Finding $it.Name "MISSING_BUILD_ARTIFACT" ("{0} ({1} is required by the manifest but is absent from the build output)" -f $a.Leaf, $a.Role)
            $preCopyArtifactFailed = $true
            continue
        }
        if (-not $inBuild) {
            if ($inStage) {
                Add-Finding $it.Name "MISSING_BUILD_FILE" $a.Leaf
                $preCopyArtifactFailed = $true
            }
            continue
        }

        if ($null -eq (Hash-Of $buildPath)) {
            Add-Finding $it.Name "HASH_UNREADABLE" ("build artifact unreadable before copy: {0}" -f $buildPath)
            $preCopyArtifactFailed = $true
        }
        if ($inStage -and $null -eq (Hash-Of $stagedPath)) {
            Add-Finding $it.Name "HASH_UNREADABLE" ("staged artifact unreadable before copy: {0}" -f $stagedPath)
            $preCopyArtifactFailed = $true
        }
    }

    if ($info.HasPck) {
        $base = [System.IO.Path]::GetFileNameWithoutExtension((Split-Path -Path $it.Build -Leaf))
        $pckPath = Join-Path $buildDir ("{0}.pck" -f $base)
        $digestPath = Join-Path $buildDir ("{0}.pck.sha256" -f $base)
        if (-not (Test-Path -LiteralPath $pckPath -PathType Leaf)) {
            Add-Finding $it.Name "MISSING_BUILD_ARTIFACT" ("{0}: manifest has_pck=true but the build PCK is absent" -f $pckPath)
            $preCopyArtifactFailed = $true
        } else {
            try {
                $pckItem = Get-Item -LiteralPath $pckPath -Force -ErrorAction Stop
                if ($pckItem.Length -eq 0) {
                    Add-Finding $it.Name "PCK_EMPTY" ("{0}: build PCK is zero bytes" -f $pckPath)
                    $preCopyArtifactFailed = $true
                }
                $dllPath = Join-Path $buildDir ("{0}.dll" -f $base)
                if (Test-Path -LiteralPath $dllPath -PathType Leaf) {
                    $dllItem = Get-Item -LiteralPath $dllPath -Force -ErrorAction Stop
                    if ($pckItem.LastWriteTimeUtc -lt $dllItem.LastWriteTimeUtc) {
                        Add-Finding $it.Name "PCK_STALE" ("{0}: build PCK mtime {1:o} is older than the build DLL mtime {2:o}" -f $pckPath, $pckItem.LastWriteTimeUtc, $dllItem.LastWriteTimeUtc)
                        $preCopyArtifactFailed = $true
                    }
                }
            } catch {
                Add-Finding $it.Name "PATH_UNREADABLE" ("PCK provenance unreadable before copy: {0} ({1})" -f $pckPath, $_.Exception.Message)
                $preCopyArtifactFailed = $true
            }
        }
        if (-not (Test-Path -LiteralPath $digestPath -PathType Leaf)) {
            Add-Finding $it.Name "PCK_DIGEST_MISSING" ("{0}: manifest has_pck=true but the build digest is absent" -f $digestPath)
            $preCopyArtifactFailed = $true
        } else {
            $recorded = $null
            try {
                $recorded = (Get-Content -LiteralPath $digestPath -Raw -ErrorAction Stop).Trim()
            } catch {
                Add-Finding $it.Name "DIGEST_UNREADABLE" ("{0}: {1}" -f $digestPath, $_.Exception.Message)
                $preCopyArtifactFailed = $true
            }
            if ($null -ne $recorded) {
                if (-not $recorded) {
                    Add-Finding $it.Name "DIGEST_UNREADABLE" ("{0}: digest record is empty" -f $digestPath)
                    $preCopyArtifactFailed = $true
                } elseif ($recorded -notmatch '^[0-9A-Fa-f]{64}$') {
                    Add-Finding $it.Name "DIGEST_MALFORMED" ("{0}: digest record is not a 64-hex SHA256" -f $digestPath)
                    $preCopyArtifactFailed = $true
                } else {
                    $actual = Hash-Of $pckPath
                    if ($null -eq $actual) {
                        Add-Finding $it.Name "HASH_UNREADABLE" ("{0}: cannot hash {1}" -f $digestPath, $pckPath)
                        $preCopyArtifactFailed = $true
                    } elseif ($recorded.ToUpperInvariant() -ne $actual) {
                        Add-Finding $it.Name "DIGEST_MISMATCH" ("{0}: record {1} vs pck {2}" -f $digestPath, (Short-Hash $recorded.ToUpperInvariant()), (Short-Hash $actual))
                        $preCopyArtifactFailed = $true
                    }
                    try {
                        if (Test-Path -LiteralPath $pckPath -PathType Leaf) {
                            $pckItem = Get-Item -LiteralPath $pckPath -Force -ErrorAction Stop
                            $digestItem = Get-Item -LiteralPath $digestPath -Force -ErrorAction Stop
                            if ($digestItem.LastWriteTimeUtc -lt $pckItem.LastWriteTimeUtc) {
                                Add-Finding $it.Name "DIGEST_STALE" ("{0}: digest mtime {1:o} predates the PCK mtime {2:o}" -f $digestPath, $digestItem.LastWriteTimeUtc, $pckItem.LastWriteTimeUtc)
                                $preCopyArtifactFailed = $true
                            }
                        }
                    } catch {
                        Add-Finding $it.Name "PATH_UNREADABLE" ("digest provenance unreadable before copy: {0} ({1})" -f $digestPath, $_.Exception.Message)
                        $preCopyArtifactFailed = $true
                    }
                }
            }
        }
    }
}
if ($preCopyArtifactFailed) {
    Write-Output "PRE-COPY ARTIFACT GATE: refusing to refresh staging (no file was written):"
    $findings | Format-Table -AutoSize
    exit 1
}
# ---- Copy phase (skipped by -VerifyOnly): regenerate staging from the build output ---------------
# The copy phase has NO fallback: a staged file is refreshed only from the manifest-declared build
# artifact, and a file that is not named by that allowlist is never created, never overwritten and
# never refreshed. The old candidate list ended with the repo source tree, which could silently
# publish a stale or hand-edited file as if it had come from the build. If a payload file has no
# build artifact, the copy phase records MISSING_BUILD_ARTIFACT and the verify phase reports it.
if (-not $VerifyOnly) {
    foreach ($it in $rows) {
        # Re-check the write targets immediately before touching them (TOCTOU window closed for the
        # ordinary case): dot-dot, allowlist, reparse chain, Steam marker, enumeration. Any failure
        # stops this row before a single byte is written.
        $preRowFailed = $false
        foreach ($pair in @(
            @{ P = (Get-NormalizedDir (Join-Path $root $it.Repo)); L = "repo" },
            @{ P = (Get-NormalizedDir (Join-Path (Join-Path $root $it.Repo) $it.Manifest)); L = "manifest" },
            @{ P = (Get-NormalizedDir (Split-Path -Path (Join-Path (Join-Path $root $it.Repo) $it.Build) -Parent)); L = "build" },
            @{ P = (Get-NormalizedDir (Split-Path -Path (Join-Path (Join-Path $root $it.Repo) $it.Stage) -Parent)); L = "stage" })) {
            if (Test-RawPathHasDotDot $pair.P) { Add-Finding $it.Name "PATH_DOTDOT" ("{0}: {1}" -f $pair.L, $pair.P); $preRowFailed = $true; continue }
            if (-not (Test-PathInside $pair.P $root)) { Add-Finding $it.Name "PATH_ESCAPES_REPO" ("{0}: {1}" -f $pair.L, $pair.P); $preRowFailed = $true; continue }
            try {
                if (Test-ReparseChain $pair.P) { Add-Finding $it.Name "PATH_REPARSE_POINT" ("{0}: {1}" -f $pair.L, $pair.P); $preRowFailed = $true }
                if (Test-SteamMarkerAncestor $pair.P) { Add-Finding $it.Name "PATH_STEAM_MARKER" ("{0}: {1}" -f $pair.L, $pair.P); $preRowFailed = $true }
            } catch {
                Add-Finding $it.Name "PATH_UNREADABLE" ("{0}: {1} ({2})" -f $pair.L, $pair.P, $_.Exception.Message); $preRowFailed = $true
            }
        }
        if ($preRowFailed) { [void]$results.Add([pscustomobject]@{ Name = $it.Name; Status = "PATH_ENUM_FAILED"; Detail = "per-row pre-write gate failed; row skipped before write" }); continue }


        $repoDir = Full-Path (Join-Path $root $it.Repo)
        $srcManifest = Full-Path (Join-Path $repoDir $it.Manifest)
        $buildDll = Full-Path (Join-Path $repoDir $it.Build)
        $buildDir = (Full-Path (Split-Path -Path $buildDll -Parent)).TrimEnd('\')
        $stageDll = Full-Path (Join-Path $repoDir $it.Stage)
        $stageDir = (Full-Path (Split-Path -Path $stageDll -Parent)).TrimEnd('\')

        if (-not (Test-Path -LiteralPath $buildDir -PathType Container)) {
            [void]$results.Add([pscustomobject]@{ Name = $it.Name; Status = "MISSING_BUILD"; Detail = $buildDir })
            continue
        }
        if (-not (Test-Path -LiteralPath $stageDir -PathType Container)) {
            [void]$results.Add([pscustomobject]@{ Name = $it.Name; Status = "MISSING_STAGE_DIR"; Detail = $stageDir })
            continue
        }

        $manifestLeaf = Split-Path -Path $it.Manifest -Leaf
        $info = Get-ManifestInfo $srcManifest $it.Name
        if (-not $info) {
            [void]$results.Add([pscustomobject]@{ Name = $it.Name; Status = "MISSING_BUILD"; Detail = ("manifest unusable: {0}" -f $srcManifest) })
            continue
        }
        $allow = Get-RowAllowlist $it $info $manifestLeaf

        # Copy direction 1: refresh every staged payload file from its own build artifact.
        $stageList = Get-FileListChecked $it.Name $stageDir "staging (copy)"
        if (-not $stageList.Ok) {
            [void]$results.Add([pscustomobject]@{ Name = $it.Name; Status = "PATH_ENUM_FAILED"; Detail = "staging tree failed reparse/readability check; copy phase stopped for this row" })
            continue
        }
        foreach ($staged in $stageList.Files) {
            $rel = $staged.FullName.Substring($stageDir.Length + 1)
            # The staged manifest is an ALLOWED payload file, but its bytes come from the repo
            # manifest, not from the build output dir, so it is refreshed separately below and must
            # not be looked up as a build artifact.
            if ($rel -ieq $manifestLeaf) { continue }
            $entry = @($allow | Where-Object { $_.Leaf -ieq $rel })
            if ($entry.Count -eq 0 -or -not $entry[0].Payload) {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "STAGED_FILE_NOT_ALLOWLISTED"; Detail = "record-only or unknown file is not a payload" })
                continue
            }
            $source = Join-Path $buildDir $rel
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "MISSING_BUILD_ARTIFACT"; Detail = ("{0} has no build artifact in {1}" -f $rel, $buildDir) })
                continue
            }

            $srcHash = Hash-Of $source
            $dstHash = Hash-Of $staged.FullName
            if ($null -eq $srcHash -or $null -eq $dstHash) {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "HASH_UNREADABLE"; Detail = ("source={0} staged={1}" -f (Short-Hash $srcHash), (Short-Hash $dstHash)) })
                continue
            }
            if ($srcHash -eq $dstHash) {
                # Same bytes. For every payload role that is a plain byte copy this is the terminal
                # state. The PCK is the ONE role whose staged identity includes its mtime (the
                # verifier compares staged vs build mtime), so identical bytes with different mtimes
                # is NOT "already current": regenerate must re-copy the build PCK so the real
                # producer mtime travels with the bytes. Times are never rewritten in place - that
                # would hide a producer problem instead of fixing it. VerifyOnly never reaches this
                # code (the whole copy phase is skipped), and WhatIf only reports WOULD_COPY.
                $sameHashNeedsCopy = $false
                if ($entry[0].Role -eq "pck") {
                    try {
                        $srcItem = Get-Item -LiteralPath $source -Force -ErrorAction Stop
                        $dstItem = Get-Item -LiteralPath $staged.FullName -Force -ErrorAction Stop
                        if ($srcItem.LastWriteTimeUtc -ne $dstItem.LastWriteTimeUtc) { $sameHashNeedsCopy = $true }
                    } catch {
                        [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "PATH_UNREADABLE"; Detail = ("PCK mtime read failed: {0}" -f $_.Exception.Message) })
                        continue
                    }
                }
                if (-not $sameHashNeedsCopy) {
                    [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "ALREADY_CURRENT"; Detail = (Short-Hash $srcHash) })
                    continue
                }
            }
            if ($WhatIf) {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "WOULD_COPY"; Detail = ("{0} -> {1}" -f (Short-Hash $dstHash), (Short-Hash $srcHash)) })
                continue
            }
            try {
                Copy-Item -LiteralPath $source -Destination $staged.FullName -Force -ErrorAction Stop
                $afterHash = Hash-Of $staged.FullName
                if ($null -ne $afterHash -and $afterHash -eq $srcHash) {
                    [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "REFRESHED"; Detail = ("{0} -> {1}" -f (Short-Hash $dstHash), (Short-Hash $afterHash)) })
                } else {
                    [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "COPY_VERIFY_FAILED"; Detail = ("{0} vs {1}" -f (Short-Hash $srcHash), (Short-Hash $afterHash)) })
                }
            } catch {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $rel); Status = "COPY_FAILED"; Detail = $_.Exception.Message })
            }
        }


        # Copy direction 2: a REQUIRED payload artifact that is missing from staging is refreshed from
        # the build output. The manifest source file is copied into staging when it differs, because
        # the staged manifest is itself a payload file (direction 1 above never touches it: it is not
        # a build artifact and has its own source). Hash failures are hard: a missing staged manifest
        # (hash null) is NOT "different bytes", it is a missing payload that must be recreated; an
        # unreadable source manifest stops the row before any write.
        $stageManifest = Join-Path $stageDir $manifestLeaf
        $srcManifestHash = Hash-Of $srcManifest
        $stageManifestHash = Hash-Of $stageManifest
        if ($null -eq $srcManifestHash) {
            [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $manifestLeaf); Status = "HASH_UNREADABLE"; Detail = ("source manifest unreadable: {0}" -f $srcManifest) })
        } else {
            $manifestNeedsCopy = ($null -eq $stageManifestHash) -or ($srcManifestHash -ne $stageManifestHash)
            if ($manifestNeedsCopy) {
                if ($WhatIf) {
                    [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $manifestLeaf); Status = "WOULD_COPY"; Detail = "manifest source -> staging" })
                } else {
                    try {
                        Copy-Item -LiteralPath $srcManifest -Destination $stageManifest -Force -ErrorAction Stop
                        $afterManifestHash = Hash-Of $stageManifest
                        if ($null -ne $afterManifestHash -and $afterManifestHash -eq $srcManifestHash) {
                            [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $manifestLeaf); Status = "REFRESHED"; Detail = "manifest source -> staging" })
                        } else {
                            [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $manifestLeaf); Status = "COPY_VERIFY_FAILED"; Detail = ("manifest readback {0}" -f (Short-Hash $afterManifestHash)) })
                        }
                    } catch {
                        [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $manifestLeaf); Status = "COPY_FAILED"; Detail = $_.Exception.Message })
                    }
                }
            }
        }
        foreach ($a in $allow) {
            if (-not $a.Payload -or $a.Role -eq "manifest") { continue }
            $stagedPath = Join-Path $stageDir $a.Leaf
            if (Test-Path -LiteralPath $stagedPath -PathType Leaf) { continue }
            $source = Join-Path $buildDir $a.Leaf
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
            if ($WhatIf) {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $a.Leaf); Status = "WOULD_COPY"; Detail = "build -> staging (staged file absent)" })
                continue
            }
            try {
                Copy-Item -LiteralPath $source -Destination $stagedPath -Force -ErrorAction Stop
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $a.Leaf); Status = "REFRESHED"; Detail = "build -> staging (staged file absent)" })
            } catch {
                [void]$results.Add([pscustomobject]@{ Name = ("{0}:{1}" -f $it.Name, $a.Leaf); Status = "COPY_FAILED"; Detail = $_.Exception.Message })
            }
        }
    }
}


Invoke-Verification $rows $true

# Copy-phase failures are hard findings too (missing build, missing staging, no source candidate).
$badStatuses = @("MISSING_BUILD","MISSING_STAGE_DIR","MISSING_BUILD_ARTIFACT","MISSING_SOURCE_FILE","STAGED_FILE_NOT_ALLOWLISTED","COPY_FAILED","COPY_VERIFY_FAILED","HASH_UNREADABLE","PATH_ENUM_FAILED","PATH_UNREADABLE")
foreach ($b in @($results | Where-Object { $badStatuses -contains $_.Status })) {
    Add-Finding $b.Name $b.Status $b.Detail
}

# (1) the build output, scanned after the copy so the report lists every finding in one place (the
#     actual enforcement is the PRE-COPY GATE above, which runs before anything is copied).
$leaks = New-Object System.Collections.ArrayList
foreach ($h in $heldBack) {
    $buildDll = Join-Path $root $h.Build
    foreach ($l in (Get-MarkerLeaks $buildDll $h.Name "BUILD OUTPUT (a gate-open verification build leaves this behind)")) {
        [void]$leaks.Add($l)
    }
}
# (2) post-copy: the staged bytes that actually upload
foreach ($h in $heldBack) {
    $stagedDll = Join-Path $root $h.Dll
    foreach ($l in (Get-MarkerLeaks $stagedDll $h.Name "STAGED PAYLOAD (this is what uploads)")) {
        [void]$leaks.Add($l)
    }
}
foreach ($l in $leaks) { Add-Finding $l.Item $l.Status $l.Detail }

# In -WhatIf the staging tree is intentionally not rewritten, so "staged != build" and "staged
# manifest != source manifest" are the expected planning output, not a hard failure. Everything
# else (missing build, missing source, path escape, reparse, freshness) stays hard.
$softWhenWhatIf = @("SHA_MISMATCH", "MANIFEST_CONTENT_MISMATCH", "MISSING_BUILD_FILE", "MANIFEST_PCK_MISSING", "MANIFEST_DLL_MISSING")
$hardFindings = New-Object System.Collections.ArrayList
foreach ($f in $findings) {
    if ($WhatIf -and ($softWhenWhatIf -contains $f.Status)) { continue }
    [void]$hardFindings.Add($f)
}

if ($results.Count -gt 0) {
    $results | Format-Table -AutoSize
}
if ($notes.Count -gt 0) {
    Write-Output ""
    Write-Output "Notes (record-only artifacts; not uploaded, not a failure):"
    $notes | Format-Table -AutoSize
}
if ($findings.Count -gt 0) {
    Write-Output ""
    Write-Output "PAYLOAD VERIFICATION: problem(s) found - do NOT push until resolved:"
    $findings | Format-Table -AutoSize
}

Write-Output ""
if ($hardFindings.Count -gt 0) {
    Write-Output ("VERIFY RESULT: {0} hard problem(s) - refusing to publish this staging tree" -f $hardFindings.Count)
    exit 1
}

# Machine-readable inventory for the controlled push entry's pairing assertion: one ITEM line per
# verified row, tab-separated. The push entry requires an ITEM line for every VDF it will upload,
# so adding a publishable item without adding a verifier row fails closed (exit 12) instead of
# shipping an unverified staging tree.
# Field order matters: workshop-push-all.ps1's pairing assertion keys its verified set on the FOURTH
# field (parts[3]) and tests it against each VDF's "contentfolder" value. The contentfolder is the
# PARENT of the per-mod staging directory (workshop\content), not the mod directory itself - so the
# fourth field must be that contentfolder. An earlier revision put the VDF path there, which made the
# key set and the comparison value disjoint and the pairing assertion unsatisfiable. The VDF path is
# still reported as a fifth field for humans; the consumer only reads fields 1..4.
foreach ($r in $rows) {
    $stageDllOfRow = Full-Path (Join-Path (Join-Path $root $r.Repo) $r.Stage)
    $stageDirOfRow = (Split-Path -Path $stageDllOfRow -Parent).TrimEnd('\')
    $contentFolderOfRow = (Split-Path -Path $stageDirOfRow -Parent).TrimEnd('\')
    $vdfOfRow = Full-Path (Join-Path (Join-Path $root $r.Repo) $r.Vdf)
    Write-Output ("ITEM`t{0}`t{1}`t{2}`t{3}" -f $r.Name, $stageDirOfRow, $contentFolderOfRow, $vdfOfRow)
}

$copied = @($results | Where-Object { $_.Status -eq "REFRESHED" }).Count
$resultFailures = @($results | Where-Object { $_.Status -in @("MISSING_BUILD","MISSING_STAGE_DIR","MISSING_BUILD_ARTIFACT","MISSING_SOURCE_FILE","STAGED_FILE_NOT_ALLOWLISTED","COPY_FAILED","COPY_VERIFY_FAILED","HASH_UNREADABLE","PATH_ENUM_FAILED","PATH_UNREADABLE") })
if ($resultFailures.Count -gt 0) {
    Write-Output ""
    Write-Output ("FINAL GATE: {0} copy-phase failure(s) remain - refusing to report success" -f $resultFailures.Count)
    $resultFailures | Format-Table -AutoSize
    exit 1
}
$already = @($results | Where-Object { $_.Status -eq "ALREADY_CURRENT" }).Count
if ($VerifyOnly) {
    Write-Output ("VERIFY RESULT: OK ({0} row(s) verified; read-only, nothing copied)" -f $rows.Count)
} elseif ($WhatIf) {
    Write-Output ("VERIFY RESULT: OK for planning ({0} row(s); {1} file(s) would be copied)" -f $rows.Count, (@($results | Where-Object { $_.Status -eq "WOULD_COPY" }).Count))
} else {
    Write-Output ("VERIFY RESULT: OK ({0} row(s) verified; {1} refreshed, {2} already current)" -f $rows.Count, $copied, $already)
}
exit 0