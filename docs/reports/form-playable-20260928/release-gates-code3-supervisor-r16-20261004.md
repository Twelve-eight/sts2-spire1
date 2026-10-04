# release-gates 缺失 DLL 退出码修复监督审查报告 (supervisor r16, 2026-10-04)

- 审查对象: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1
- 实现者: worker agent id 01a1057d-522d-7bb3-a1b9-28c77de4af20
- 实现者报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-code3-worker-r16-20261004.md
- 审查方式: 只读静态监督 + 只读字节/文本检查 (wait_threads, 文件读取, 逐行比较, check-agent-text.mjs, PS 5.1 Parser). 未构建, 未测试, 未运行门禁, 未部署, 未启动游戏, 未修改 Steam 安装或共享 mod_configs, 未写 C:.
- 硬性顺序合规: 先调用当前 harness 原生 wait_threads 等待 worker 01a1057d-522d-7bb3-a1b9-28c77de4af20. 第一次 timeoutMs=120000 返回 timedOut=true (worker inProgress, latestTurn inProgress); 第二次 timeoutMs=300000 被 harness 以参数上限拒绝 (无效调用, 无副作用); 第三次 timeoutMs=120000 返回 timedOut=false, wake.reason=turnCompleted, turn status=completed, error=null, durationMs=304140. 收到完成状态后才读取脚本与 worker 报告. 未并行提前审查.
- 实际路由元数据 (AGENTS.md Sec 4b): worker 会话 rollout-2026-10-04T13-57-41-01a1057d-522d-7bb3-a1b9-28c77de4af20.jsonl L5 world_state payload.state.model = global:deepseek-v4.1-flash, L6 turn_context payload.model = global:deepseek-v4.1-flash; C:\Users\o_Obl\.codex\config.toml L12 model_provider = "gateway", L113 [model_providers.wb2api], L114 name = "wb2api (bridged)". 与请求文件声明的模型 global:deepseek-v4.1-flash 与路由 gateway/wb2api 一致. worker 会话 function_call 统计: exec_command=56, 无 spawn_agent / task / 其它代理运行时调用. 本监督会话未再委派, 只使用 wait_threads 与只读 exec_command.
- 审查限制: 本轮只做只读静态监督与文本检查; 未做运行验证, 不产生实机结论.

## S1. 实现者先落盘中央发现的真实复现基线, 且只修改 run-release-gates.ps1 的缺失 DLL 错误输出行为

- 状态: PASS.
- 先落盘证据 (worker 会话时间线, 物理行号): L257 (2026-10-04T06:01:28Z = 本地 14:01:28) 写入报告首版, 输出见 L260 "WROTE bytes=6452"; L264 (06:01:32Z = 本地 14:01:32) 才编辑产品脚本, 输出见 L267 "matches=1 WROTE"; L317 与 L338 之后仅追加修复段与 git diff 段 (L320 "WROTE bytes=11343", L341 "updated ... len=11677"). 目标脚本 mtime 2026-10-04 14:01:32, 报告最终 mtime 14:02:33. 结论: 基线先于产品脚本修改落盘.
- 真实复现基线证据: 中央会话 rollout-2026-10-04T01-03-09-01a0f876-eafc-7801-8ccd-905cb175805e_01a102b8-37a2-7c41-9182-94fc10f7f2ea.jsonl ordinal 20784 (物理行 4815) 为复现命令原文; ordinal 20787 (物理行 4818) 为 function_call_output, 实测 EXIT=1 (不是 3), UNCHANGED=True, stderr 出现 Write-Error 文本与 WriteErrorException 堆栈. sentinel 只读复核: G:\omp works\.tmp\release-gates-central-r15-20261004\code3-sentinel.json 内容 { "sentinel": "unchanged" }, LastWriteTime 2026-10-04 13:55:21, 未被中央运行改写. worker 报告 B1 引用内容与上述证据一致; 报告用 ordinal 20784/20787 标注为 "行", 物理行号为 4815/4818, 属引用格式不精确, 证据本体已独立复核, 不构成返工.
- 只改目标脚本证据: 编辑前 r15 基线快照 G:\omp works\.tmp\release-gates-central-r15-20261004\fail-harness\tools\build-gates\run-release-gates.ps1, 4971 bytes, SHA256 19DE80674200285C7E56B8D06F6C33BFD36FF58FD48CA50174B1D8FAC2F31965, mtime 13:35:33 (早于 worker 启动 13:57, 未被 worker 改写). 逐行比较 96 行 vs 96 行: 只有 L35 一行不同, 非 L35 的 95 行逐字一致 (ALL_NON_L35_LINES_IDENTICAL=True). 长度变化后按字节索引直接比较会产生假差异, 本审查采用逐行比较. 仓库时间窗扫描 (>= 2026-10-04 13:55, 排除 .git/bin/obj): 只有 worker 请求 md, supervisor 请求 md, 目标脚本 (14:01:32), worker 报告 (14:02:33); worker 会话的文件写入调用只有 4 次 (报告 L257, 脚本 L264, 报告 L317, 报告 L338), 未触碰其它产品文件.

## S2. 缺失 DLL 路径在 $ErrorActionPreference = Stop 下仍先写 stderr 再执行 exit 3

- 状态: PASS (静态控制流; 运行验证不在本轮范围).
- 当前控制流 (修复后): L26 $ErrorActionPreference = "Stop" -> L34 if (-not (Test-Path $Dll)) 为 true -> L35 [Console]::Error.WriteLine("Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)") -> L36 exit 3.
- 语义: [Console]::Error.WriteLine 是 .NET 方法调用, 直接写进程 stderr, 不产生 PowerShell ErrorRecord, 不进入 $ErrorActionPreference="Stop" 的终止路径; 因此控制流到达 L36, 进程退出码为 3, 同时错误文本已写入 stderr.
- 对照旧行为: 编辑前 L35 Write-Error 在 Stop 下抛终止错误, powershell.exe 以 1 退出, L36 不可达 (中央 ordinal 20787 实测 EXIT=1). 修复只替换 L35 输出方式, L36 保留未改.
- 尚缺: 修复后真实进程 exit 3, stderr 文本编码与内容, sentinel JSON 不被改写的实机复核 (worker 报告 U1/U4; 本轮按请求不构建, 不运行门禁).

## S3. 身份字段回写 L63-L95, 已有 U+3002 修复, 其它退出码分支和 SkipBuild 行为未被误改

- 状态: PASS (静态).
- L63-L95 逐语句复核: L68 condition ($Json -and ($code -eq 0 -or $code -eq 2)); L70 Test-Path -LiteralPath $Json -PathType Leaf, 缺文件 throw; L72 单次 [System.IO.File]::ReadAllBytes($Dll); L73-L75 SHA256 + BitConverter.ToString().Replace("-",""); L76 [int64]$dllBytes.Length; L79 ReadAllText($Json); L80 ConvertFrom-Json; L81-L82 LastIndexOf("}") 找不到即 throw; L83-L84 插入逗号 (空对象除外); L86-L87 生成新文本; L88 WriteAllText UTF8 no BOM; L89 成功日志; L91-L94 catch 写 stderr, $code=0 时 exit 1, $code=2 保留 2; L96 exit $code. 与 r14 retry / r15 报告记录的控制流一致, 且与 r15 基线逐字相同 (逐行比较已确认).
- U+3002 修复保持: 当前全文件 U+3002=0; r15 基线 (19DE80...) 也已 U+3002=0, 本轮未回退.
- 其它退出码分支: L49 门禁工具构建失败 Write-Error + exit 1, L51 门禁工具未构建 Write-Error + exit 1, L96 exit $code 均与 r15 基线逐字一致, 未被本轮修改.
- SkipBuild: L46 if (-not $SkipBuild) 与 L51 gateDll 检查与基线逐字一致, 未被本轮修改.
- 注: L49/L51 的 Write-Error 在 Stop 下同样会提前终止, 但其声明退出码为 1, 与本轮缺失 DLL 路径修复范围无关; 本轮未改动, 已记录不改.

## S4. 字节与文本门禁

- 状态: PASS (实际执行只读检查).
- 当前脚本: 4987 bytes; SHA256 7F27DE56FE566D2C3D206A3142C845392AB1128AA4E61339AA2A04923595E81F; 首 3 字节 23 20 72 (no BOM); CRLF=96; LF-only=0; CR-only=0; 行数 96; C0 控制字符 (除 TAB/CR/LF)=0; DEL=0; U+3002=0; 末 4 字节 64 65 0D 0A (仍以 CRLF 结束).
- check-agent-text.mjs: node tools/check-agent-text.mjs --file tools/build-gates/run-release-gates.ps1 -> agent text accepted, exit 0. 对 worker 报告同样 -> agent text accepted, exit 0.
- PS 5.1 Parser: powershell.exe 5.1.19041.6328, Parser::ParseFile parseErrors=0.
- 字节增量自洽: 基线 4971 bytes + L35 行增量 16 bytes (80 -> 96 bytes) = 4987 bytes, 与实测一致; 除 L35 外无字节变化.

## S5. git diff 与最小改动证据, 报告三段与边界

- 状态: PASS (限定: git diff 对 HEAD 含历史未提交改动, 最小性以 r15 快照为准).
- git diff vs HEAD 显示 39 insertions / 5 deletions 级别的大 hunk, 因为 HEAD 未包含 r14 (身份回写 L63-L95) 与 r15 (U+3002 修复) 的未提交改动; 该 diff 不能单独作为本轮最小性证据. worker 报告 F1 已明确这一点, 表述正确.
- 本轮最小性证据: 对 r15 快照 (19DE80...) 逐行比较, 只有 L35 一行变化, +16 bytes; 当前 L35 = [Console]::Error.WriteLine("Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)"), 基线 L35 = Write-Error "Spire1.dll 找不到: $Dll (先构建 mod, 或用 -Dll 指定)"; L36 exit 3 保留. 与 git diff 中 L35 hunk (Write-Error -> [Console]::Error.WriteLine) 一致.
- worker 报告三段: ## 已确认 / ## 进行中 / ## 未知 均存在; 已确认含 B1/B2/F1/F2; 进行中含 W1; 未知含 U1-U5.
- 边界声明: worker 报告明确写 未构建, 未运行门禁, 未部署, 未启动游戏, 未修改 Steam 安装, 未修改共享 mod_configs, 未写 C:; U1 明确列出修复后真实 exit 3/stderr 未验证. 边界表述与本轮审查限制一致.

## 已确认

- S1 PASS: 基线先落盘 (报告 14:01:28 首写 -> 脚本 14:01:32 编辑 -> 报告 14:02:33 终版); 只改目标脚本 L35 一行; 非 L35 的 95 行与 r15 快照逐字一致; 时间窗内无其它产品文件被 worker 写入.
- S2 PASS (静态): [Console]::Error.WriteLine 直接写 stderr, 不触发 Stop 终止路径, L36 exit 3 可达; 旧 Write-Error 提前终止的 EXIT=1 基线有中央证据.
- S3 PASS (静态): L63-L95 身份回写与 L49/L51/L96 退出码分支, L46-L51 SkipBuild 均与 r15 基线逐字一致; U+3002=0 保持.
- S4 PASS: 4987 bytes, SHA256 7F27DE56..., no BOM, CRLF=96, LF-only=0, C0=0, U+3002=0; check-agent-text.mjs exit 0 (脚本与 worker 报告); PS 5.1 parseErrors=0.
- S5 PASS (限定): git diff 含历史改动故以 r15 快照隔离本轮增量; worker 报告三段完整并写明未构建/未运行边界.
- 合规: worker 会话元数据 model=global:deepseek-v4.1-flash, 路由 gateway/wb2api (config.toml L12/L113-L114), function_call 全部为 exec_command (56 次), 无再委派, 无其它代理运行时. 本监督会话未再委派.

## 进行中

- 无. 本轮只读静态监督审查已完成, 本报告已落盘.

## 未知

- 修复后真实进程行为未验证: exit 3, stderr 文本编码与内容, sentinel JSON 不被改写 (worker U1/U4; 本轮禁止运行).
- PASS/FAIL/-Json 身份回写路径的运行语义未验证 (worker U2); 其它调用方是否依赖 Write-Error 的 ErrorRecord 格式未验证 (worker U3).
- 工作树整体相对 HEAD 仍有大量历史未提交改动; 本监督只证明本轮时间窗内的写入与脚本相对 r15 快照的增量, 不能证明工作树其它历史改动.
- worker 报告以 ordinal 20784/20787 标注会话 "行", 物理行号为 4815/4818; 证据内容已独立复核一致, 属引用格式不精确, 不构成返工.

## 最终结论

SUPERVISION_PASS

- 依据: S1-S5 全部满足 (S2/S3/S5 为静态或限定结论; S4 为实际只读检查). 修复只改 L35 一行, +16 bytes, 保持 no BOM/CRLF/U+3002=0/C0=0, 检查器与 PS 5.1 解析通过; L36 exit 3 与 L63-L95 回写逻辑未被触碰.
- 返工范围: 无.
- 边界: 未构建, 未测试, 未运行门禁, 未部署; 本结论是只读静态监督结论, 不代表实机或运行通过. 运行验证 (exit 3 / stderr / sentinel 不改写) 仍待中央执行.
