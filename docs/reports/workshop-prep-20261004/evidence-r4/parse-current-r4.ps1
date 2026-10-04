param([string]$Report)
$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp'; $env:TMP='G:\tmp'
$rows=foreach($p in @('G:\omp works\.tooling\refresh-workshop-payloads.ps1','G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1','G:\omp works\.tooling\workshop-push-all.ps1','G:\omp works\Sts\sts2-spire1\workshop\workshop-push.ps1')){
 $tokens=$null; $errors=$null
 $null=[Management.Automation.Language.Parser]::ParseFile($p,[ref]$tokens,[ref]$errors)
 [pscustomobject]@{Path=$p;Engine=$PSVersionTable.PSVersion.ToString();Sha256=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash;Passed=@($errors).Count -eq 0;Errors=@($errors|ForEach-Object {$_.Message})}
}
$rows|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Report -Encoding utf8
$rows|Select-Object Path,Engine,Passed|Format-List
if(@($rows|Where-Object {-not $_.Passed}).Count){exit 1}