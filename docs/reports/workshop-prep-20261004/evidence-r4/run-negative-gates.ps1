param([string]$Gate='G:\omp works\.tooling\refresh-workshop-payloads.ps1')
$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp'; $env:TMP='G:\tmp'
$central='G:\omp works\.tmp\workshop-prep-20261004-central'
$seed=Join-Path $central 'fixture-release-v2'
$base=Join-Path $central ('negative-gates-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
if(Test-Path -LiteralPath $base){throw 'Fixture output already exists'}
New-Item -ItemType Directory -Path $base | Out-Null
$hostExe=(Get-Process -Id $PID).Path
$results=[Collections.Generic.List[object]]::new()
function Snap([string]$root){
  $rows=foreach($f in Get-ChildItem -LiteralPath $root -File -Recurse -Force){
    [pscustomobject]@{Path=$f.FullName.Substring($root.Length);Size=$f.Length;Ticks=$f.LastWriteTimeUtc.Ticks;Hash=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash}
  }
  return ($rows | Sort-Object Path | ConvertTo-Json -Depth 4 -Compress)
}
function FreshFixture([string]$name){
  $dest=Join-Path $base $name
  New-Item -ItemType Directory -Path $dest | Out-Null
  Get-ChildItem -LiteralPath $seed -Force | Copy-Item -Destination $dest -Recurse -Force
  foreach($vdf in Get-ChildItem -LiteralPath $dest -File -Recurse -Filter '*.vdf'){
    $text=[IO.File]::ReadAllText($vdf.FullName)
    $escapedSeed=$seed.Replace('\','\\'); $escapedDest=$dest.Replace('\','\\')
    if(-not $text.Contains($escapedSeed)){throw ('VDF does not reference escaped seed: '+$vdf.FullName)}
    [IO.File]::WriteAllText($vdf.FullName,$text.Replace($escapedSeed,$escapedDest),[Text.UTF8Encoding]::new($false))
  }
  return $dest
}
function InvokeGate([string]$root,[string]$tag,[string[]]$extra=@()){
  $log=Join-Path $base ($tag+'.log')
  & $hostExe -NoProfile -ExecutionPolicy Bypass -File $Gate -Root $root @extra *> $log
  $exitCode=$LASTEXITCODE
  return [pscustomobject]@{Exit=$exitCode;Text=[IO.File]::ReadAllText($log);Log=$log}
}
function Record([string]$name,[bool]$ok,[object]$detail){
  $results.Add([pscustomobject]@{Name=$name;Passed=$ok;Detail=$detail})
  $results | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $base 'results.json') -Encoding utf8
  if(-not $ok){throw ('FAILED: '+$name)}
  Write-Output ('PASS: '+$name)
}
$root=FreshFixture 'baseline'
$before=Snap $root; $r=InvokeGate $root '01-baseline' @('-VerifyOnly')
Record 'spire1-three-file-and-other-pdb-baseline' ($r.Exit -eq 0 -and $r.Text -match 'VERIFY RESULT: OK' -and (Snap $root) -eq $before) $r.Log
$spireBuild=Join-Path $root 'sts2-spire1\mod\.godot\mono\temp\bin\Release'
$spireStage=Join-Path $root 'sts2-spire1\workshop\content\Spire1'
$r=InvokeGate $root '02-regenerate'
Record 'regenerate-does-not-introduce-spire1-pdb' ($r.Exit -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $spireStage 'Spire1.pdb'))) $r.Log
$root=FreshFixture 'staged-pdb'
$stage=Join-Path $root 'sts2-spire1\workshop\content\Spire1\Spire1.pdb'
[IO.File]::WriteAllText($stage,'staged pdb is forbidden',[Text.Encoding]::ASCII)
$before=Snap $root; $r=InvokeGate $root '03-staged-pdb'
Record 'staged-spire1-pdb-fails-before-write' ($r.Exit -ne 0 -and $r.Text -match 'STAGED_FILE_NOT_ALLOWLISTED' -and (Snap $root) -eq $before) $r.Log
$root=FreshFixture 'same-hash-new-stage-mtime'
$stage=Join-Path $root 'sts2-spire1\workshop\content\Spire1\Spire1.pck'
$build=Join-Path $root 'sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.pck'
[IO.File]::WriteAllBytes($stage,[IO.File]::ReadAllBytes($stage))
$before=Snap $root; $r=InvokeGate $root '04-mtime-verify' @('-VerifyOnly')
Record 'mtime-mismatch-verify-is-read-only' ($r.Exit -ne 0 -and $r.Text -match 'PCK_MTIME_MISMATCH' -and (Snap $root) -eq $before) $r.Log
$r=InvokeGate $root '05-mtime-whatif' @('-WhatIf')
Record 'mtime-mismatch-whatif-is-read-only' ((Snap $root) -eq $before -and $r.Text -match 'WOULD_COPY') $r.Log
$r=InvokeGate $root '06-mtime-refresh'
$valid=((Get-Item -LiteralPath $stage).LastWriteTimeUtc.Ticks -eq (Get-Item -LiteralPath $build).LastWriteTimeUtc.Ticks)
Record 'same-hash-mtime-refresh-copies-source' ($r.Exit -eq 0 -and $valid) $r.Log
$r=InvokeGate $root '07-mtime-after' @('-VerifyOnly')
Record 'mtime-refreshed-fixture-verifies' ($r.Exit -eq 0 -and $r.Text -match 'VERIFY RESULT: OK') $r.Log
foreach($case in 'missing','malformed','mismatch','stale'){
  $root=FreshFixture ('digest-'+$case)
  $pck=Join-Path $root 'sts2-perfect\mod\.godot\mono\temp\bin\Release\Perfect.pck'
  $digest=$pck+'.sha256'
  $pattern=''
  switch($case){
    missing {Remove-Item -LiteralPath $digest -Force; $pattern='PCK_DIGEST_MISSING'}
    malformed {[IO.File]::WriteAllText($digest,'not-a-digest',[Text.Encoding]::ASCII);$pattern='DIGEST_MALFORMED'}
    mismatch {[IO.File]::WriteAllText($digest,('0'*64),[Text.Encoding]::ASCII);$pattern='DIGEST_MISMATCH'}
    stale {[IO.File]::WriteAllBytes($pck,[IO.File]::ReadAllBytes($pck));$pattern='DIGEST_STALE'}
  }
  $before=Snap $root; $r=InvokeGate $root ('08-digest-'+$case)
  Record ('digest-'+$case+'-fails-before-write') ($r.Exit -ne 0 -and $r.Text -match $pattern -and (Snap $root) -eq $before) $r.Log
}
Write-Output ('RESULTS: '+($results | Where-Object Passed).Count+'/'+$results.Count)
Write-Output ('EVIDENCE: '+$base)