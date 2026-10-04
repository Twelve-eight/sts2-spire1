# release-gates 缺失 DLL 退出码修复报告 (worker r16, 2026-10-04)

- 任务: 让缺失 DLL 的错误路径真正返回 3, 同时保留错误文本写到 stderr.
- 范围: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 (唯一允许修改的产品文件).
- 本报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-code3-worker-r16-20261004.md
- 请求文件: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-code3-worker-r16-20261004-request.md
- 基线快照时间: 2026-10-04 14:00 (Asia/Shanghai); 修复完成时间: 2026-10-04 14:01 (Asia/Shanghai)
- 边界: 未构建, 未运行门禁, 未部署, 未启动游戏, 未修改 Steam 安装, 未修改共享 mod_configs, 未写 C:.
- 落盘顺序: 编辑前基线先写入本报告文件, 之后才编辑产品脚本 (先落盘后继续).
- 实际路由元数据 (AGENTS.md Sec 4b): 本会话 turn_context payload.model = global:deepseek-v4.1-flash; world_state payload.state.model = global:deepseek-v4.1-flash; 会话 config.toml L12 model_provider = "gateway", L113 [model_providers.wb2api] L114 name = "wb2api (bridged)", 与请求文件声明的路由 gateway/wb2api 一致. 本会话无 spawn_agent/task 调用, 未再委派, 未启动其它代理运行时.
- 证据分层: 以下 B* 为编辑前基线, F* 为修复后静态检查, W* 为进行中, U* 为未知/待中央实机验证.

## 已确认

### B1 [P0] 编辑前基线: L26-L37 与缺失 DLL 错误路径控制流

- 绝对路径与准确行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L26-L37.
- 触发条件: 调用方传入不存在的 -Dll, 或省略 -Dll 且缺省 Release Spire1.dll 不存在.
- 契约 (L15): 退出码 3=用法/输入错误; 缺失 DLL 属输入错误, 应返回 3.
- 当前控制流 (编辑前): L26 $ErrorActionPreference = "Stop" -> L34 Test-Path 为 false -> L35 Write-Error 抛终止错误, powershell.exe 立即结束 -> L36 exit 3 不可达.
- L26-L37 原文 (编辑前):
  26: $ErrorActionPreference = "Stop"
  27: $here = Split-Path -Parent $MyInvocation.MyCommand.Path
  28: $repoRoot = Resolve-Path (Join-Path $here "..\..")   # tools\build-gates -> repo root
  29: $proj = Join-Path $here "Spire1ReleaseGate.csproj"
  30:
  31: if (-not $Dll) {
  32:     $Dll = Join-Path $repoRoot "mod\.godot\mono\temp\bin\Release\Spire1.dll"
  33: }
  34: if (-not (Test-Path $Dll)) {
  35:     Write-Error "Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)"
  36:     exit 3
  37: }
- 中央复现命令 (主会话真实执行原文; 只读引用, 本轮未重跑):
  $root='G:\omp works\.tmp\release-gates-central-r15-20261004'
  $sentinel=Join-Path $root 'code3-sentinel.json'
  $initial='{ "sentinel": "unchanged" }'
  [IO.File]::WriteAllText($sentinel,$initial,(New-Object Text.UTF8Encoding($false)))
  $script='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $missing='G:\omp works\.tmp\release-gates-central-r15-20261004\missing\Spire1.dll'
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -Dll $missing -Json $sentinel -SkipBuild
  $code=$LASTEXITCODE
  $after=[IO.File]::ReadAllText($sentinel)
  "EXIT=$code"
  "UNCHANGED=$($after -eq $initial)"
  "JSON=$after"
- 中央实际结果: EXIT=1 (不是 3); stderr 出现 Write-Error 文本 "Spire1.dll 找不到..." 与 WriteErrorException 堆栈; sentinel JSON 未被改写 (UNCHANGED=True).
- 复现证据位置 (只读): C:\Users\o_Obl\.codex\sessions\2026\10\04\rollout-2026-10-04T01-03-09-01a0f876-eafc-7801-8ccd-905cb175805e_01a102b8-37a2-7c41-9182-94fc10f7f2ea.jsonl 行 20784 (function_call 原文), 行 20787 (function_call_output, EXIT=1, UNCHANGED=True).
- sentinel 当前状态 (2026-10-04 14:00 只读复核): 内容 { "sentinel": "unchanged" }, LastWriteTime 2026-10-04 13:55:21, 即中央运行未改写该文件.
- 最小修复范围: 仅 L35 的错误输出方式; L36 exit 3 保留.
- 尚缺的实机证据: 修复后真实进程退出码 3 与 stderr 文本 (本轮按请求不运行).

### B2 [P0] 编辑前字节与文本门禁基线

- 绝对路径: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1.
- 触发条件: 任何对脚本字节的修改都可能破坏 no BOM / CRLF / 允许字符契约.
- 契约: no BOM, CRLF 换行, U+3002=0, 无 C0 控制字符 (除 TAB/CR/LF), check-agent-text.mjs exit 0, PowerShell 5.1 可解析.
- 当前状态 (编辑前, 只读):
  Length: 4971 bytes.
  SHA256: 19DE80674200285C7E56B8D06F6C33BFD36FF58FD48CA50174B1D8FAC2F31965.
  BOM: 无; 首 3 字节 23 20 72 (即 '#', ' ', 'r').
  换行: CRLF=96, LF-only=0.
  C0 控制字符 (除 TAB/CR/LF): 0.
  U+3002 计数: 0.
  check-agent-text.mjs --file: exit 0 (agent text accepted).
  PowerShell 5.1 Parser::ParseFile: parseErrors=0 (PS=5.1.19041.6328).
- 可复现命令:
  $p='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $b=[System.IO.File]::ReadAllBytes($p); $t=[System.Text.Encoding]::UTF8.GetString($b)
  'len={0} crlf={1} lfOnly={2} u3002={3}' -f $b.Length,([regex]::Matches($t,"`r`n").Count),([regex]::Matches($t,"(?<!`r)`n").Count),([regex]::Matches($t,[char]0x3002).Count)
  (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
  & node 'G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs' --file $p
  powershell.exe -NoProfile -Command '$t=$null;$e=$null;$null=[System.Management.Automation.Language.Parser]::ParseFile("G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1",[ref]$t,[ref]$e); "parseErrors="+$e.Count'
- 最小修复范围: 修复后保持 no BOM, CRLF, U+3002=0, C0=0, 并复跑上述检查.
- 尚缺的实机证据: 无 (编辑前基线已完整采集; 运行语义另见 B1).

### F1 [P0] 最小修复: 只改 L35 的错误输出方式

- 绝对路径与准确行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L35 (修复后行号不变, 仍为 L35).
- 修复内容 (最小 diff, 编辑前 r15 基线 -> 修复后):
  L35
    -     Write-Error "Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)"
    +     [Console]::Error.WriteLine("Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)")
- 触发条件: 同 B1, 缺失 DLL.
- 契约: stderr 保留同样的错误文本; 错误文本不经 PowerShell 错误流, 因此不受 $ErrorActionPreference = "Stop" 影响; 控制流继续到 L36 exit 3, 进程退出码为 3.
- 当前控制流 (修复后): L26 $ErrorActionPreference = "Stop" -> L34 Test-Path 为 false -> L35 [Console]::Error.WriteLine 直接写 stderr (无终止错误) -> L36 exit 3 -> 进程退出码 3.
- 修复后 L34-L37 原文:
  34: if (-not (Test-Path $Dll)) {
  35:     [Console]::Error.WriteLine("Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)")
  36:     exit 3
  37: }
- 最小修复范围证据 (逐行比较): 以 G:\omp works\.tmp\release-gates-central-r15-20261004\fail-harness\tools\build-gates\run-release-gates.ps1 (SHA256 19DE80674200285C7E56B8D06F6C33BFD36FF58FD48CA50174B1D8FAC2F31965, 即编辑前基线, 96 行) 与修复后脚本 (SHA256 7F27DE56FE566D2C3D206A3142C845392AB1128AA4E61339AA2A04923595E81F, 96 行) 逐行比较, 差异只有 L35 一行; 文件行数不变.
- 未改动项 (静态对照): L26-L34, L36-L96 与编辑前基线逐行一致; 门禁工具构建失败分支 (L49), 门禁工具未构建分支 (L51), SkipBuild 行为 (L46-L51), DLL 身份字段回写块 (L63-L95), 最终 exit $code (L96) 均未改动; U+3002 修复成果保持 (全文件 U+3002=0).
- git diff 证据: git -C 'G:\omp works\Sts\sts2-spire1' diff -- tools/build-gates/run-release-gates.ps1 显示 L34-L37 hunk 只含 L35 一行替换 (Write-Error -> [Console]::Error.WriteLine); 该 diff 对 HEAD 同时包含 r14/r15 轮次尚未提交的既有改动, 故本轮最小性以 r15 基线 (19DE80...) 逐行比较为准.
- 可复现命令:
  $base='G:\omp works\.tmp\release-gates-central-r15-20261004\fail-harness\tools\build-gates\run-release-gates.ps1'
  $cur='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $b=[System.IO.File]::ReadAllLines($base); $c=[System.IO.File]::ReadAllLines($cur)
  for($i=0;$i -lt [Math]::Max($b.Count,$c.Count);$i++){ $bl=if($i -lt $b.Count){$b[$i]}else{'<EOF>'}; $cl=if($i -lt $c.Count){$c[$i]}else{'<EOF>'}; if($bl -ne $cl){ "L$($i+1)"; "  - $bl"; "  + $cl" } }
- 尚缺的实机证据: 修复后缺失 DLL 路径的真实进程退出码 3 与 stderr 文本 (本轮按请求不构建, 不运行门禁; 需中央复核).

### F2 [P0] 修复后字节与文本门禁复检

- 绝对路径: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1.
- 触发条件: 修改后必须保持 no BOM / CRLF / 允许字符 / 可解析契约.
- 契约: 同 B2.
- 当前状态 (修复后, 只读):
  Length: 4987 bytes (+16, 等于 Write-Error -> [Console]::Error.WriteLine 的文本增量).
  SHA256: 7F27DE56FE566D2C3D206A3142C845392AB1128AA4E61339AA2A04923595E81F.
  BOM: 无; 首 3 字节 23 20 72.
  换行: CRLF=96, LF-only=0.
  C0 控制字符 (除 TAB/CR/LF): 0.
  U+3002 计数: 0.
  check-agent-text.mjs --file: exit 0 (agent text accepted).
  PowerShell 5.1 Parser::ParseFile: parseErrors=0 (PS=5.1.19041.6328).
- 可复现命令: 同 B2 命令块 (对修复后文件复跑).
- 最小修复范围: 仅 L35; 字节契约全部保持.
- 尚缺的实机证据: 无 (本项为静态字节/文本检查, 已完成); 运行语义验证见 F1 与 U1.

## 进行中

### W1 [P1] 中央实机复核待办 (本轮不做)

- 绝对路径与准确行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L34-L36.
- 触发条件: 需要确认修复后缺失 DLL 路径的真实进程行为.
- 契约: 进程退出码 3; stderr 含 "Spire1.dll 找不到"; -Json 指定的 sentinel 文件不被改写.
- 当前状态: 静态证据齐备 (F1, F2), 但按请求本轮未构建, 未运行门禁, 未部署.
- 可复现命令 (中央待执行, 建议):
  $root='G:\omp works\.tmp\release-gates-central-r15-20261004'
  $sentinel=Join-Path $root 'code3-sentinel.json'
  $initial='{ "sentinel": "unchanged" }'
  [IO.File]::WriteAllText($sentinel,$initial,(New-Object Text.UTF8Encoding($false)))
  $script='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $missing=Join-Path $root 'missing\Spire1.dll'
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -Dll $missing -Json $sentinel -SkipBuild 2>$null
  $code=$LASTEXITCODE
  $after=[IO.File]::ReadAllText($sentinel)
  "EXIT=$code"
  "UNCHANGED=$($after -eq $initial)"
- 最小修复范围: 无新增代码; 仅实机执行与记录.
- 尚缺的实机证据: 上述 EXIT/UNCHANGED/stderr 三项真实输出.

## 未知

- U1 [P0] 修复后缺失 DLL 路径的真实进程退出码 3 与 stderr 文本尚未实机验证 (本轮按请求不构建, 不运行门禁).
- U2 [P1] 修复对 PASS/FAIL/-Json 身份字段回写路径的影响未运行验证; 静态对照显示这些分支未被触碰 (F1), 但真实运行证据缺失.
- U3 [P1] 其它调用方是否依赖 Write-Error 的 PowerShell 错误记录格式 (ErrorRecord, FullyQualifiedErrorId, $Error 变量内容) 未验证; 已知契约只要求 stderr 文本与退出码 3.
- U4 [P1] 本脚本在 PowerShell 5.1 下 [Console]::Error.WriteLine 的输出编码 (中文 stderr 是否按调用方期望编码呈现) 未实机验证.
- U5 [P2] 本轮未构建, 未运行门禁, 未部署, 未启动游戏; 交付面仅为脚本源码修复与本报告.
