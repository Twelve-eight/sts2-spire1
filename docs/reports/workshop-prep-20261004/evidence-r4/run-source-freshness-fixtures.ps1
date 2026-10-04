param([string]$Gate,[string]$Tag)
$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp';$env:TMP='G:\tmp'
$central='G:\omp works\.tmp\workshop-prep-20261004-central'
$seed=Join-Path $central 'fixture-release-v2'
$base=Join-Path $central ('source-freshness-'+$Tag+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
if(Test-Path -LiteralPath $base){throw 'Fixture output exists'}
New-Item -ItemType Directory -Path $base | Out-Null
$hostExe=(Get-Process -Id $PID).Path
$results=[Collections.Generic.List[object]]::new()
$cases=@(
 @{Name='baseline';Path=$null;WantRebuild=$false},
 @{Name='root-doc-report-json';Path='docs\reports\new-evidence.json';WantRebuild=$false},
 @{Name='root-doc-snapshot-json';Path='DOCS\HANDOFF-source-snapshot.json';WantRebuild=$false},
 @{Name='production-json';Path='mod\Spire1\localization\eng\new-input.json';WantRebuild=$true},
 @{Name='nested-runtime-docs-json';Path='mod\Spire1\docs\new-runtime-input.json';WantRebuild=$true},
 @{Name='production-cs';Path='mod\Spire1Code\NewSource.cs';WantRebuild=$true},
 @{Name='root-doc-cs';Path='docs\Example.cs';WantRebuild=$true},
 @{Name='production-csproj';Path='mod\NewProject.csproj';WantRebuild=$true},
 @{Name='root-props';Path='Directory.Build.props';WantRebuild=$true},
 @{Name='root-doc-props';Path='docs\NewBuild.props';WantRebuild=$true},
 @{Name='internal-tools-cs';Path='tools\NewProbe.cs';WantRebuild=$false},
 @{Name='internal-research-json';Path='research\upstream\new-input.json';WantRebuild=$false},
 @{Name='internal-obj-cs';Path='mod\obj\Generated.cs';WantRebuild=$false}
)
foreach($case in $cases){
 $dest=Join-Path $base $case.Name
 New-Item -ItemType Directory -Path $dest | Out-Null
 Get-ChildItem -LiteralPath $seed -Force | Copy-Item -Destination $dest -Recurse -Force
 foreach($vdf in Get-ChildItem -LiteralPath $dest -File -Recurse -Filter '*.vdf'){
  $text=[IO.File]::ReadAllText($vdf.FullName)
  $escapedSeed=$seed.Replace('\','\\');$escapedDest=$dest.Replace('\','\\')
  if(-not $text.Contains($escapedSeed)){throw 'VDF seed mapping missing'}
  [IO.File]::WriteAllText($vdf.FullName,$text.Replace($escapedSeed,$escapedDest),[Text.UTF8Encoding]::new($false))
 }
 if($case.Path){
  $path=Join-Path (Join-Path $dest 'sts2-spire1') $case.Path
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
  [IO.File]::WriteAllText($path,'fixture source input or report',[Text.Encoding]::ASCII)
 }
 $log=Join-Path $dest 'gate.log'
 $saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $hostExe -NoProfile -ExecutionPolicy Bypass -File $Gate -Root $dest -VerifyOnly *> $log;$exit=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 $text=[IO.File]::ReadAllText($log)
 $passed=if($case.WantRebuild){$exit -eq 1 -and $text -match 'REBUILD_REQUIRED'}else{$exit -eq 0 -and $text -match 'VERIFY RESULT: OK'}
 $results.Add([pscustomobject]@{Name=$case.Name;Passed=$passed;Exit=$exit;WantRebuild=$case.WantRebuild;Log=$log;GateSha256=(Get-FileHash -LiteralPath $Gate).Hash;Boundary='Synthetic seven-row fixture, fresh files use actual WriteAllText time, no game or real staging writes'})
 $results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $base 'results.json') -Encoding utf8
 Write-Output ($case.Name+': Passed='+$passed+' Exit='+$exit)
}
Write-Output ('RESULTS: '+(Join-Path $base 'results.json'))
if(@($results | Where-Object {-not $_.Passed}).Count){exit 1}
