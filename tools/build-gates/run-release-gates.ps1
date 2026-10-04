# run-release-gates.ps1 -- 一键跑 Spire1 发布门禁 (供 CI / 发布脚本调用).
#
# 做两件事:
#   1) dotnet build 本目录的 Spire1ReleaseGate.csproj (Release, 缓存输出到 G:, 绝不写 C:)
#   2) 对指定的已构建 Spire1.dll 跑三道结构门禁, 以门禁工具的退出码作为本脚本退出码.
#
# 用法:
#   powershell -NoProfile -ExecutionPolicy Bypass -File run-release-gates.ps1 -Dll "<Spire1.dll>" -Gates "assemblyref,manifest,typedef" -Json "<out.json>" -SkipBuild
#
#   -Dll        缺省 = 仓库内 Release 构建输出 mod\.godot\mono\temp\bin\Release\Spire1.dll
#   -Gates      缺省 = 全部三道 (assemblyref,manifest,typedef)
#   -Json       可选, 另存机器可读结果 JSON; 门禁写出后顶层追加 dllSha256 (大写 SHA256) 与 dllLength (Int64 字节)
#   -SkipBuild  已构建过工具时跳过 dotnet build
#
# 退出码 (透传门禁工具): 0=全通过  2=门禁失败  3=用法/输入错误;  1=本脚本自身错误(构建失败, 身份字段追加失败等).
#
# PowerShell 5.1 兼容.

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
    [Console]::Error.WriteLine("Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)")
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

# --- 3) DLL 身份字段回写 (输出契约) ---
# 契约: 保留门禁工具 JSON 原字段, 顶层新增 dllSha256 (大写 SHA256) 与 dllLength (Int64 字节).
# 只在退出码 0 (PASS) 或 2 (门禁 FAIL) 时回写: 门禁工具仅在这两条路径写 JSON
# (tools\build-gates\Program.cs L103-L115); 退出码 3/异常时不改文件, 原退出码透传.
# 回写失败不伪造 PASS: 原退出码 0 时改判本脚本错误 exit 1; 原退出码 2 时保留 2 并在 stderr 报错.
if ($Json -and ($code -eq 0 -or $code -eq 2)) {
    try {
        if (-not (Test-Path -LiteralPath $Json -PathType Leaf)) { throw "门禁工具未写出 JSON: $Json" }
        # 同一文件快照: 单次读取已验证的 $Dll, 哈希与长度都取自这份字节.
        $dllBytes = [System.IO.File]::ReadAllBytes($Dll)
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { $dllSha256 = [System.BitConverter]::ToString($sha.ComputeHash($dllBytes)).Replace("-", "") }
        finally { $sha.Dispose() }
        $dllLength = [int64]$dllBytes.Length

        # 原 JSON 文本保留, 仅在根对象结束符前插入两个顶层字段 (不对 JSON 自身取哈希).
        $raw = [System.IO.File]::ReadAllText($Json)
        $null = $raw | ConvertFrom-Json
        $close = $raw.LastIndexOf("}")
        if ($close -lt 0) { throw "JSON 结构异常, 找不到根对象结束符: $Json" }
        $head = $raw.Substring(0, $close).TrimEnd()
        if (-not $head.EndsWith("{")) { $head = $head + "," }
        $nl = [System.Environment]::NewLine
        $insert = $nl + '  "dllSha256": "' + $dllSha256 + '",' + $nl + '  "dllLength": ' + $dllLength + $nl
        $newRaw = $head + $insert + $raw.Substring($close)
        [System.IO.File]::WriteAllText($Json, $newRaw, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "[gate] DLL 身份已写入: dllSha256=$dllSha256 dllLength=$dllLength"
    }
    catch {
        [Console]::Error.WriteLine("[gate] 错误: 追加 DLL 身份字段失败 (门禁退出码 $code, JSON: $Json): " + $_.Exception.Message)
        if ($code -eq 0) { exit 1 }
    }
}
exit $code
