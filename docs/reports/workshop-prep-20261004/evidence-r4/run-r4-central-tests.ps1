$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp';$env:TMP='G:\tmp'
$central='G:\omp works\.tmp\workshop-prep-20261004-central'
$ps7=(Get-Command pwsh).Source
$ps51=(Get-Command powershell).Source
$results=[Collections.Generic.List[object]]::new()
foreach($runtime in @(@{Tag='ps7';Exe=$ps7},@{Tag='ps51';Exe=$ps51})){
 foreach($kind in @('parse','freshness','negative')){
  $log=Join-Path $central ('r4-'+$runtime.Tag+'-'+$kind+'-run.log')
  $start=(Get-Date).ToString('o')
  if($kind -eq 'parse'){$args=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $central 'parse-current-r4.ps1'),'-Report',(Join-Path $central ('r4-'+$runtime.Tag+'-parse.json')))}
  elseif($kind -eq 'freshness'){$args=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $central 'run-source-freshness-fixtures.ps1'),'-Gate','G:\omp works\.tooling\refresh-workshop-payloads.ps1','-Tag',('r4-'+$runtime.Tag))}
  else{$args=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $central 'run-negative-gates.ps1'),'-Gate','G:\omp works\.tooling\refresh-workshop-payloads.ps1')}
  $saved=$ErrorActionPreference
  try{$ErrorActionPreference='Continue';& $runtime.Exe @args *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
  $results.Add([pscustomobject]@{Runtime=$runtime.Tag;Kind=$kind;Exit=$code;Started=$start;Finished=(Get-Date).ToString('o');Log=$log;GateSha256=(Get-FileHash -LiteralPath 'G:\omp works\.tooling\refresh-workshop-payloads.ps1' -Algorithm SHA256).Hash})
  $results|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $central 'r4-central-test-runs.json') -Encoding utf8
  Get-Content -LiteralPath $log
  if($code -ne 0){throw ($runtime.Tag+' '+$kind+' failed; inspect raw log')}
 }
}