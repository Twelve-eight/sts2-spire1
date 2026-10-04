param(
    [Parameter(Mandatory=$true)][string]$Source,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$HarmonyPath='E:\Slay the Spire 2\data_sts2_windows_x86_64\0Harmony.dll'
)
$ErrorActionPreference='Stop'
$env:TEMP='G:\tmp';$env:TMP='G:\tmp'
foreach($n in 'NUGET_PACKAGES','NUGET_HTTP_CACHE_PATH','DOTNET_CLI_HOME'){[Environment]::SetEnvironmentVariable($n,[Environment]::GetEnvironmentVariable($n,'User'),'Process')}
$root=$PSScriptRoot
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(!$out.StartsWith('G:\omp works\.tmp\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be under the workspace .tmp directory'}
New-Item -ItemType Directory -Path $out -Force|Out-Null
$raw=[IO.File]::ReadAllText($Source)
$pattern='(?m)^\[module: RefSafetyRules\(11\)\]\r?\n'
$matches=[regex]::Matches($raw,$pattern)
if($matches.Count -ne 1){throw 'Unexpected compiler metadata; source was not changed'}
$prepared=[regex]::Replace($raw,$pattern,'')
$generated=$out+'\ViolaSnakeBite.fixture.cs'
[IO.File]::WriteAllText($generated,$prepared,[Text.UTF8Encoding]::new($false))
$manifest=[ordered]@{CheckedAt=(Get-Date -Format o);Source=[IO.Path]::GetFullPath($Source);SourceSHA256=(Get-FileHash -LiteralPath $Source).Hash;Generated=$generated;GeneratedSHA256=(Get-FileHash -LiteralPath $generated).Hash;Transformation='Only the explicit compiler-reserved module RefSafetyRules attribute is removed; C# emits it. Target method body and debug attributes are unchanged.'}
$manifest|ConvertTo-Json|Set-Content -LiteralPath ($out+'\fixture-preparation.json') -Encoding utf8NoBOM
$projects=@(@{Name='contracts';Project='Contracts\Contracts.csproj'},@{Name='fixture';Project='Fixture\Fixture.csproj'},@{Name='host';Project='Host\ViolaProbe.csproj'})
foreach($pr in $projects){
    $dir=$out+'\'+$pr.Name
    New-Item -ItemType Directory -Path $dir -Force|Out-Null
    $buildArgs=@('build',($root+'\'+$pr.Project),'-c','Release',('-p:OutputPath='+$dir+'\bin\'),('-p:BaseIntermediateOutputPath='+$dir+'\obj\'),('-p:IntermediateOutputPath='+$dir+'\obj\'),('-p:ContractsDll='+$out+'\contracts\bin\ViolaProbe.Contracts.dll'),('-p:FixtureSource='+$generated),('-p:HarmonyPath='+$HarmonyPath))
    & dotnet @buildArgs 2>&1|Tee-Object -FilePath ($dir+'\build.log')
    if($LASTEXITCODE -ne 0){throw ('Build failed: '+$pr.Name)}
}
& dotnet ($out+'\host\bin\ViolaProbe.dll') ($out+'\fixture\bin\ViolaSnakeBite.dll') 2>&1|Tee-Object -FilePath ($out+'\run.log')
$code=$LASTEXITCODE
[ordered]@{CheckedAt=(Get-Date -Format o);ExitCode=$code;CompatSHA256=(Get-FileHash -LiteralPath ($root+'\..\..\mod\Spire1Code\Experimental\MenuPerformance\ViolaPortraitLogCompat.cs')).Hash;FixtureSourceSHA256=(Get-FileHash -LiteralPath $Source).Hash;HarmonySHA256=(Get-FileHash -LiteralPath $HarmonyPath).Hash;Boundary='Real Harmony and production compat source; a decompiled target is recompiled against controlled card/resource contracts, not the original installation DLL or native game'}|ConvertTo-Json|Set-Content -LiteralPath ($out+'\result.json') -Encoding utf8NoBOM
exit $code