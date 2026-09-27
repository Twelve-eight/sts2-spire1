# run-release-gates.ps1 -- 一键跑 Spire1 发布门禁 (供 CI / 发布脚本调用)。
#
# 做两件事:
#   1) dotnet build 本目录的 Spire1ReleaseGate.csproj (Release, 缓存输出到 G:, 绝不写 C:)
#   2) 对指定的已构建 Spire1.dll 跑三道结构门禁, 以门禁工具的退出码作为本脚本退出码。
#
# 用法:
#   powershell -NoProfile -ExecutionPolicy Bypass -File run-release-gates.ps1 -Dll "<Spire1.dll>" -Gates "assemblyref,manifest,typedef" -Json "<out.json>" -SkipBuild
#
#   -Dll        缺省 = 仓库内 Release 构建输出 mod\.godot\mono\temp\bin\Release\Spire1.dll
#   -Gates      缺省 = 全部三道 (assemblyref,manifest,typedef)
#   -Json       可选, 另存机器可读结果 JSON
#   -SkipBuild  已构建过工具时跳过 dotnet build
#
# 退出码 (透传门禁工具): 0=全通过  2=门禁失败  3=用法/输入错误;  1=本脚本自身错误(构建失败等)。
#
# PowerShell 5.1 兼容。

param(
    [string]$Dll,
    [string]$Gates = "assemblyref,manifest,typedef",
    [string]$Json,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $here "..\..")   # tools\build-gates -> repo root
$proj = Join-Path $here "Spire1ReleaseGate.csproj"

if (-not $Dll) {
    $Dll = Join-Path $repoRoot "mod\.godot\mono\temp\bin\Release\Spire1.dll"
}
if (-not (Test-Path $Dll)) {
    Write-Error "Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)"
    exit 3
}

# --- 缓存重定向到 G: (AGENTS.md Sec3) ---
if (-not $env:NUGET_PACKAGES)        { $env:NUGET_PACKAGES        = "G:\omp works\.nuget-packages" }
if (-not $env:NUGET_HTTP_CACHE_PATH) { $env:NUGET_HTTP_CACHE_PATH = "G:\omp works\.nuget\http-cache" }
if (-not $env:DOTNET_CLI_HOME)       { $env:DOTNET_CLI_HOME       = "G:\omp works\.dotnet-home" }

# --- 1) 构建门禁工具本体 ---
$gateDll = Join-Path $here "bin\Release\net9.0\Spire1ReleaseGate.dll"
if (-not $SkipBuild) {
    Write-Host "[gate] 构建门禁工具 ..."
    & dotnet build $proj -c Release -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "门禁工具构建失败 (dotnet build exit $LASTEXITCODE)"; exit 1 }
}
if (-not (Test-Path $gateDll)) { Write-Error "门禁工具未构建: $gateDll (去掉 -SkipBuild 再跑)"; exit 1 }

# --- 2) 跑门禁 ---
$cfg = Join-Path $here "gate-config.json"
$argv = @($gateDll, "--dll", $Dll, "--config", $cfg, "--gates", $Gates)
if ($Json) { $argv += @("--json", $Json) }

Write-Host "[gate] 校验 $Dll ..."
& dotnet @argv
$code = $LASTEXITCODE
Write-Host "[gate] 门禁工具退出码: $code"
exit $code
