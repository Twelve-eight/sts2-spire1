# Build-Spire1Release.ps1
# Source-driven Spire1 release rebuild.
# Default behavior writes only under G:\omp works\.tmp and never touches a game install.

[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [string]$RepoRoot,
    [string]$OutputRoot,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$PlanOnly,
    [switch]$SkipBuild,
    [switch]$Promote,
    [switch]$KeepScratch
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Resolve-FullPath([string]$Path) {
    return [System.IO.Path]::GetFullPath($Path)
}

function Normalize([string]$Path) {
    return (Resolve-FullPath $Path).TrimEnd([char[]]"\/").ToLowerInvariant()
}

function Assert-Under([string]$Path, [string]$Root, [string]$Name) {
    $p = Normalize $Path
    $r = Normalize $Root
    if ($p -ne $r -and -not $p.StartsWith($r + '\')) {
        throw "PATH-DENY [$Name]: '$Path' is outside '$Root'."
    }
}

function Assert-NoReparse([string]$Path, [string]$Name) {
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "PATH-DENY [$Name]: reparse point '$Path' is not allowed."
    }
}

function Convert-ToSnake([string]$Name) {
    # Model IDs in this codebase split every capital boundary, including acronyms:
    # CreativeAI -> creative_a_i, FTL -> f_t_l, JAX -> j_a_x.
    $value = [regex]::Replace($Name, '([A-Z])', '_$1')
    return $value.TrimStart('_').ToLowerInvariant()
}

function Add-Keep([System.Collections.Generic.HashSet[string]]$Set, [string]$RelativePath) {
    $rel = $RelativePath.Replace('\','/').TrimStart('/')
    [void]$Set.Add($rel)
}

function Add-ConcreteModelStems([string]$CodeDir, [string]$Pattern, [System.Collections.Generic.HashSet[string]]$StemSet) {
    foreach ($file in Get-ChildItem -LiteralPath $CodeDir -Filter '*.cs' -File) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($match in [regex]::Matches($text, $Pattern)) {
            Add-Keep $StemSet (Convert-ToSnake $match.Groups[1].Value)
        }
    }
}

function Copy-Checked([string]$Source, [string]$Destination, [string]$SourceRoot, [string]$StageRoot) {
    Assert-Under $Source $SourceRoot 'source-file'
    Assert-Under $Destination $StageRoot 'stage-file'
    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

if (-not $RepoRoot) {
    $RepoRoot = Resolve-FullPath (Join-Path $PSScriptRoot '..\..')
} else {
    $RepoRoot = Resolve-FullPath $RepoRoot
}

$workspaceRoot = 'G:\omp works'
Assert-Under $RepoRoot $workspaceRoot 'repo-root'

$sourceRoot = Join-Path $RepoRoot 'mod\Spire1'
$codeRoot = Join-Path $RepoRoot 'mod\Spire1Code'
$project = Join-Path $RepoRoot 'mod\Spire1.csproj'
$manifest = Join-Path $RepoRoot 'mod\Spire1.json'
$gates = Join-Path $RepoRoot 'tools\build-gates\run-release-gates.ps1'
$pckVerifier = Join-Path $RepoRoot 'tools\release\Verify-Spire1Pck.ps1'
$packer = 'G:\omp works\Sts\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\tools\net9.0\any\StS2PckPacker.dll'

foreach ($path in @($sourceRoot,$codeRoot,$project,$manifest,$gates,$pckVerifier,$packer)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "INPUT-MISSING: '$path'." }
}
Assert-NoReparse $sourceRoot 'source-root'
Assert-NoReparse $codeRoot 'code-root'

if (-not $OutputRoot) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputRoot = Join-Path $workspaceRoot ".tmp\spire1-release-$stamp"
} else {
    $OutputRoot = Resolve-FullPath $OutputRoot
}
Assert-Under $OutputRoot (Join-Path $workspaceRoot '.tmp') 'output-root'
if ((Normalize $OutputRoot) -eq (Normalize $sourceRoot)) { throw 'PATH-DENY: output root equals source root.' }

$stageRoot = Join-Path $OutputRoot 'stage'
$stageAssetRoot = Join-Path $stageRoot 'Spire1'
$evidenceRoot = Join-Path $OutputRoot 'evidence'
$payloadRoot = Join-Path $OutputRoot 'payload'
$payloadModsRoot = Join-Path $payloadRoot 'mods\Spire1'

New-Item -ItemType Directory -Force -Path $stageAssetRoot,$evidenceRoot,$payloadModsRoot | Out-Null
Assert-NoReparse $OutputRoot 'output-root'

$keep = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$excludeReasons = @{}
$cardStems = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$relicStems = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$potionStems = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

Add-ConcreteModelStems (Join-Path $codeRoot 'Cards') '(?m)\bclass\s+([A-Za-z0-9_]+)\s*(?:\([^)]*\))?\s*:\s*(?:Spire1Card|Spire1Curse)\b' $cardStems
Add-ConcreteModelStems (Join-Path $codeRoot 'Relics') '(?m)\bclass\s+([A-Za-z0-9_]+)\s*(?:\([^)]*\))?\s*:\s*Spire1Relic\b' $relicStems
Add-ConcreteModelStems (Join-Path $codeRoot 'Potions') '(?m)\bclass\s+([A-Za-z0-9_]+)\s*(?:\([^)]*\))?\s*:\s*Spire1Potion\b' $potionStems

Add-Keep $keep 'mod_image.png'
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'localization') -Recurse -File) {
    Add-Keep $keep $file.FullName.Substring($sourceRoot.Length + 1)
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'images\run_history') -Recurse -File) {
    Add-Keep $keep $file.FullName.Substring($sourceRoot.Length + 1)
}
foreach ($rel in @(
    'images/charui/big_energy.png',
    'images/charui/text_energy.png',
    'images/card_portraits/card.png',
    'images/card_portraits/big/card.png',
    'images/relics/relic.png',
    'images/relics/relic_outline.png',
    'images/relics/big/relic.png',
    'images/potions/potion.png',
    'images/potions/outline/potion.png'
)) { Add-Keep $keep $rel }

foreach ($stem in $cardStems) {
    foreach ($rel in @("images/card_portraits/$stem.png", "images/card_portraits/big/$stem.png")) {
        if (Test-Path -LiteralPath (Join-Path $sourceRoot $rel)) { Add-Keep $keep $rel }
    }
}
foreach ($stem in $relicStems) {
    foreach ($rel in @("images/relics/$stem.png", "images/relics/${stem}_outline.png", "images/relics/big/$stem.png")) {
        if (Test-Path -LiteralPath (Join-Path $sourceRoot $rel)) { Add-Keep $keep $rel }
    }
}
foreach ($stem in $potionStems) {
    foreach ($rel in @("images/potions/$stem.png", "images/potions/outline/$stem.png")) {
        if (Test-Path -LiteralPath (Join-Path $sourceRoot $rel)) { Add-Keep $keep $rel }
    }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'images\powers') -Recurse -File) {
    $rel = $file.FullName.Substring($sourceRoot.Length + 1).Replace('\','/')
    if ($file.BaseName -ieq 'omega_power') {
        $excludeReasons[$rel] = 'confirmed-unused: no current product class or explicit path'
    } else {
        Add-Keep $keep $rel
    }
}

$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File
foreach ($file in $sourceFiles) {
    $rel = $file.FullName.Substring($sourceRoot.Length + 1).Replace('\','/')
    if (-not $keep.Contains($rel) -and -not $excludeReasons.ContainsKey($rel)) {
        if ($rel -like 'images/charui/*') {
            $excludeReasons[$rel] = 'confirmed-unused-template: Placeholder character UI asset; runtime uses PlaceholderID/vanilla assets'
        } elseif ($rel -like 'images/card_portraits/*') {
            $excludeReasons[$rel] = 'not-reachable-by-current-Spire1Card-or-Spire1Curse-id; legacy/reused/beta asset'
        } elseif ($rel -like 'images/relics/*') {
            $excludeReasons[$rel] = 'not-reachable-by-current-Spire1Relic-id; legacy/reused asset'
        } elseif ($rel -like 'images/potions/*') {
            $excludeReasons[$rel] = 'not-reachable-by-current-Spire1Potion-id; legacy/reused asset'
        } else {
            throw "ASSET-UNCLASSIFIED: '$rel' is neither allowlisted nor explicitly excluded. Update the release contract before publishing."
        }
    }
}

foreach ($rel in $keep) {
    $src = Join-Path $sourceRoot ($rel.Replace('/','\'))
    if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { throw "ASSET-MISSING: allowlisted '$rel' not found in source." }
    Copy-Checked $src (Join-Path $stageAssetRoot ($rel.Replace('/','\'))) $sourceRoot $stageRoot
}

$manifestObject = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
if ($manifestObject.has_dll -ne $true -or $manifestObject.has_pck -ne $true) {
    throw 'MANIFEST-DENY: Spire1.json must declare has_dll=true and has_pck=true.'
}
Copy-Checked $manifest (Join-Path $payloadModsRoot 'Spire1.json') $RepoRoot $payloadRoot

$assetRows = foreach ($rel in ($keep | Sort-Object)) {
    $path = Join-Path $stageAssetRoot ($rel.Replace('/','\'))
    $item = Get-Item -LiteralPath $path
    [pscustomobject]@{ Path=$rel; Bytes=[int64]$item.Length; Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant() }
}
$excludedRows = foreach ($rel in ($excludeReasons.Keys | Sort-Object)) {
    [pscustomobject]@{ Path=$rel; Reason=$excludeReasons[$rel] }
}
$assetReport = [pscustomobject]@{
    Schema = 'Spire1ReleaseAssetManifest.v1'
    GeneratedAt = (Get-Date).ToString('o')
    SourceRoot = $sourceRoot
    StageRoot = $stageAssetRoot
    Counts = [pscustomobject]@{ SourceFiles=$sourceFiles.Count; Kept=$assetRows.Count; Excluded=$excludedRows.Count; KeptBytes=(($assetRows | Measure-Object Bytes -Sum).Sum); ExcludedBytes=0 }
    ModelStems = [pscustomobject]@{ Cards=($cardStems | Sort-Object); Relics=($relicStems | Sort-Object); Potions=($potionStems | Sort-Object) }
    Kept = $assetRows
    Excluded = $excludedRows
}
$excludedBytes = 0L
foreach ($row in $excludedRows) { $src = Join-Path $sourceRoot ($row.Path.Replace('/','\')); if (Test-Path -LiteralPath $src) { $excludedBytes += (Get-Item -LiteralPath $src).Length } }
$assetReport.Counts.ExcludedBytes = $excludedBytes
$assetReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'asset-manifest.json') -Encoding UTF8

if (-not $PlanOnly -and -not $SkipBuild) {
    $dotnetArgs = @(
        'build', $project,
        '-c', $Configuration,
        '-p:CopyToModsFolderOnBuild=false',
        '-p:IncludeHeldBackLayers=false',
        '-p:PckPackerEnabled=false',
        '--nologo',
        '-v:minimal'
    )
    Write-Host '[release] building Spire1.dll without deployment or PCK side effects ...'
    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "BUILD-FAIL: dotnet exit $LASTEXITCODE." }
}

$dllCandidates = @(
    (Join-Path $RepoRoot "mod\.godot\mono\temp\bin\$Configuration\Spire1.dll"),
    (Join-Path $RepoRoot "mod\bin\$Configuration\Spire1.dll"),
    (Join-Path $RepoRoot "mod\publish\Spire1.dll")
)
$dll = $dllCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $dll -and -not $PlanOnly) { throw 'BUILD-ARTIFACT-MISSING: Spire1.dll not found in known build outputs.' }
if ($dll -and -not $PlanOnly) { Copy-Checked $dll (Join-Path $payloadModsRoot 'Spire1.dll') $RepoRoot $payloadRoot }

if (-not $PlanOnly) {
    Write-Host '[release] packing only the generated staging asset tree ...'
    & dotnet $packer $stageAssetRoot 'Spire1' (Join-Path $payloadModsRoot 'Spire1.pck')
    if ($LASTEXITCODE -ne 0) { throw "PCK-FAIL: packer exit $LASTEXITCODE." }
}

$pck = Join-Path $payloadModsRoot 'Spire1.pck'
if (Test-Path -LiteralPath $pck -PathType Leaf) {
    $pckItem = Get-Item -LiteralPath $pck
    if ($pckItem.Length -le 0) { throw 'PCK-DENY: generated PCK is empty.' }
    $pckHash = (Get-FileHash -LiteralPath $pck -Algorithm SHA256).Hash.ToUpperInvariant()
    Set-Content -LiteralPath (Join-Path $evidenceRoot 'Spire1.pck.sha256') -Value $pckHash -Encoding ASCII
} elseif (-not $PlanOnly) {
    throw 'PCK-MISSING: no PCK was produced.'
}

if (-not $PlanOnly) {
    $pckStructure = Join-Path $evidenceRoot 'pck-structure.json'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $pckVerifier `
        -PckPath $pck `
        -AssetManifestPath (Join-Path $evidenceRoot 'asset-manifest.json') `
        -OutputJson $pckStructure
    if ($LASTEXITCODE -ne 0) { throw "PCK-VERIFY-FAIL: verifier exit $LASTEXITCODE." }
}

$payloadRows = foreach ($file in Get-ChildItem -LiteralPath $payloadModsRoot -File) {
    [pscustomobject]@{ Path=$file.Name; Bytes=[int64]$file.Length; Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToUpperInvariant() }
}
$releaseReport = [pscustomobject]@{
    Schema='Spire1ReleaseManifest.v1'
    GeneratedAt=(Get-Date).ToString('o')
    Configuration=$Configuration
    SourceCommit=(git -C $RepoRoot rev-parse HEAD 2>$null)
    PayloadRoot=$payloadModsRoot
    Payload=$payloadRows
    AssetManifest='evidence/asset-manifest.json'
    PckDigest='evidence/Spire1.pck.sha256'
    HistoricalInputsExcluded=@('dist/deprecated','dist/friends-pack','dist/Spire1-Forms-Beta-20261003.zip')
}
$releaseReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'release-manifest.json') -Encoding UTF8

if (-not $PlanOnly -and $dll) {
    $gateJson = Join-Path $evidenceRoot 'release-gates.json'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $gates -Dll $dll -Gates 'assemblyref,manifest,typedef' -Json $gateJson
    if ($LASTEXITCODE -ne 0) { throw "GATE-FAIL: release gates exit $LASTEXITCODE." }
}

if ($Promote) {
    if ($PlanOnly) { throw 'PROMOTE-DENY: cannot promote a plan-only run.' }
    $workshopRoot = Join-Path $RepoRoot 'workshop\content\Spire1'
    Assert-Under $workshopRoot (Join-Path $RepoRoot 'workshop\content') 'workshop-payload'
    Assert-NoReparse (Join-Path $RepoRoot 'workshop\content') 'workshop-content-root'
    if (-not $PSCmdlet.ShouldProcess($workshopRoot, 'replace Spire1 Workshop payload with clean three-file staging')) { throw 'PROMOTE-CANCELLED.' }
    New-Item -ItemType Directory -Force -Path $workshopRoot | Out-Null
    foreach ($name in @('Spire1.dll','Spire1.json','Spire1.pck')) {
        Copy-Item -LiteralPath (Join-Path $payloadModsRoot $name) -Destination (Join-Path $workshopRoot $name) -Force
    }
    foreach ($stale in @('Spire1.pdb','Spire1.deps.json','Spire1.pck.sha256')) {
        $stalePath = Join-Path $workshopRoot $stale
        if (Test-Path -LiteralPath $stalePath) { Remove-Item -LiteralPath $stalePath -Force }
    }
    $children = Get-ChildItem -LiteralPath $workshopRoot -Force
    $leftovers = $children | Where-Object { $_.Name -notin @('Spire1.dll','Spire1.json','Spire1.pck') }
    if ($leftovers) { throw "PROMOTE-DENY: unexpected files or directories remain: $($leftovers.Name -join ', ')" }
    $dirs = $children | Where-Object { $_.PSIsContainer }
    if ($dirs) { throw "PROMOTE-DENY: unexpected directories remain: $($dirs.Name -join ', ')" }
    Write-Host "[release] promoted clean payload to $workshopRoot"
}

Write-Host "[release] output: $OutputRoot"
Write-Host "[release] kept assets: $($assetRows.Count); excluded assets: $($excludedRows.Count); excluded bytes: $excludedBytes"
if ($PlanOnly) { Write-Host '[release] plan only: no build, PCK or gate execution.' }


