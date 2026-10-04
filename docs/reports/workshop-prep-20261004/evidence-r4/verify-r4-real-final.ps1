$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp';$env:TMP='G:\tmp'
$central='G:\omp works\.tmp\workshop-prep-20261004-central'
$gate='G:\omp works\.tooling\refresh-workshop-payloads.ps1'
$exe=(Get-Command pwsh).Source
$results=[Collections.Generic.List[object]]::new()
function Run([string]$name,[string]$script,[string[]]$arguments){
 $log=Join-Path $central ('r4-real-'+$name+'.log');$start=(Get-Date).ToString('o');$saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $exe -NoProfile -ExecutionPolicy Bypass -File $script @arguments *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 $text=[IO.File]::ReadAllText($log)
 $r=[pscustomobject]@{Name=$name;Exit=$code;Started=$start;Finished=(Get-Date).ToString('o');Log=$log;Script=$script;Arguments=$arguments;GateSha256=(Get-FileHash -LiteralPath $gate).Hash}
 $results.Add($r);$results|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $central 'r4-real-final-verification-results.json') -Encoding utf8
 [Console]::WriteLine($name+' exit='+$code);[Console]::WriteLine($text)
 return $r
}
$before=foreach($p in @('G:\omp works\.tmp\workshop-push-last-attempt','G:\omp works\.tooling\steamcmd\logs\console_log.txt')){if(Test-Path -LiteralPath $p){$f=Get-Item -LiteralPath $p;[pscustomobject]@{Path=$p;Exists=$true;Bytes=$f.Length;Ticks=$f.LastWriteTimeUtc.Ticks}}else{[pscustomobject]@{Path=$p;Exists=$false;Bytes=$null;Ticks=$null}}}
$spireBefore=@(Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\workshop\content\Spire1' -File|ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash;Ticks=$_.LastWriteTimeUtc.Ticks}})
foreach($name in @('Perfect','ChaosBridge','RegentFXFastBoot','MpConfigSync','HeartShake','QuriousCraftingRelics')){
 $r=Run ($name+'-verify') $gate @('-Only',$name,'-VerifyOnly')
 if($r.Exit -ne 0 -or [IO.File]::ReadAllText($r.Log) -notmatch 'VERIFY RESULT: OK'){throw ($name+' verification failed')}
}
$r=Run 'full-verify-final' $gate @('-VerifyOnly')
if($r.Exit -ne 1 -or [IO.File]::ReadAllText($r.Log) -notmatch 'Spire1\s+REBUILD_REQUIRED'){throw 'Unexpected final full verification outcome'}
$r=Run 'Spire1-wrapper-GuardsOnly' 'G:\omp works\Sts\sts2-spire1\workshop\workshop-push.ps1' @('-Vdf','G:\omp works\Sts\sts2-spire1\workshop\workshop_upload.vdf','-GuardsOnly')
if($r.Exit -eq 0 -or [IO.File]::ReadAllText($r.Log) -notmatch 'REBUILD_REQUIRED'){throw 'Unexpected guarded wrapper outcome'}
$changes=@(foreach($f in $before){$exists=Test-Path -LiteralPath $f.Path;if($exists -ne $f.Exists){$f.Path;continue};if($exists){$now=Get-Item -LiteralPath $f.Path;if($now.Length -ne $f.Bytes -or $now.LastWriteTimeUtc.Ticks -ne $f.Ticks){$f.Path}}})
$spireChanges=@(foreach($f in $spireBefore){$now=Get-Item -LiteralPath $f.Path;if((Get-FileHash -LiteralPath $f.Path).Hash -ne $f.Hash -or $now.LastWriteTimeUtc.Ticks -ne $f.Ticks){$f.Path}})
[pscustomobject]@{At=(Get-Date).ToString('o');LoginHistoryBefore=$before;LoginHistoryChanges=$changes;Spire1StagingChanges=$spireChanges;Boundary='GuardsOnly refused at read-only provenance; no later guard or Steam login claimed passed. Freshness is post-copy for regenerate, so earlier full refresh legitimately changed six staging rows before exit 1.'}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $central 'r4-no-upload-and-spire-staging-unchanged.json') -Encoding utf8
if($changes.Count -or $spireChanges.Count){throw 'Protected marker/staging change during read-only checks'}