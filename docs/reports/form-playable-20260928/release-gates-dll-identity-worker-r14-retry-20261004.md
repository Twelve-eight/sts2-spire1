# Release gates DLL identity worker r14 retry - 2026-10-04

## 已确认

### B1 [P0] 门禁工具 JSON/退出码契约

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs L26, L45-L46, L103-L113, L115.
- 触发条件: 调用方传 --json, 门禁结果 PASS 或 FAIL.
- 契约: 工具仅在 L103-L113 写 JSON, 之后 L115 返回 0 (PASS) 或 2 (FAIL); 用法/输入错误经 L26 返回 3 且不写 JSON.
- 控制流: L103 jsonOut 非空 -> L111 File.WriteAllText -> L115 return anyFail ? 2 : 0.
- 可复现命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs' -Pattern 'File.WriteAllText|return anyFail|static int Fail'
- 最小修复范围: 不改 Program.cs 与 gate tool 二进制; 脚本按退出码 0/2 判定本次 JSON 已写出.
- 尚缺中央验证: 未构建, 未运行.

### B2 [P0] 工作树已有 r14 尝试实现; L66 注释含 0x08 控制字符

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L12, L63-L95; 编辑前 L66 在 tools 与 uild-gatesProgram.cs 之间含 U+0008.
- 触发条件: 解析, 审查或提交脚本时.
- 契约: 请求语言面只允许中/英/法/德/俄文与 ASCII 控制字符及 ASCII 标点; 注释应准确指向 Program.cs L103-L115.
- 控制流: 该字符只出现在注释, 不改变 L68-L95 的运行语义; 但会污染文件字节与提交内容.
- 可复现命令: $t=[System.IO.File]::ReadAllText('G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'); $t.IndexOf([char]8)
- 最小修复范围: 只把 L66 的损坏子串替换为 tools\build-gates\Program.cs; 不改控制流.
- 尚缺中央验证: 未构建, 未运行.

### B3 [P0] L66 已修复; 本次会话只改这一行

- 绝对路径与行号: run-release-gates.ps1 L66; 修复前工作树基线来自 r14 首次尝试 (L12, L63-L95 已有实现).
- 触发条件: -Json 分支执行时读取注释不影响运行; 修复目标是字节面与注释准确性.
- 契约: 注释指向 tools\build-gates\Program.cs L103-L115; 脚本 no BOM + CRLF; 不写 C:.
- 控制流: 修复后 L66 为 `# (tools\build-gates\Program.cs L103-L115); 退出码 3/异常时不改文件, 原退出码透传.`; L68-L95 身份回写逻辑逐字未动.
- 可复现命令: $t=[System.IO.File]::ReadAllText('G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'); $t.IndexOf([char]8)  # 期望 -1; 另: git -C 'G:\omp works\Sts\sts2-spire1' diff -- tools/build-gates/run-release-gates.ps1
- 最小修复范围: 仅 L66; 无其它源码改动.
- 尚缺中央验证: 未构建, 未运行.

### B4 [P1] 现有回写块与验收契约的对应关系 (源码证据)

- 绝对路径与行号: run-release-gates.ps1 L68-L95.
- 触发条件: -Json 指定且 $code 为 0 或 2; 未指定 -Json 时 L68 为假, 无新增副作用.
- 契约: 保留门禁工具 JSON 原字段; 顶层新增 dllSha256 (大写 SHA256) 与 dllLength (Int64); 写回或身份计算失败必须显式失败, 不伪造 PASS; 原始 stdout 与退出码保留.
- 控制流: L72 单次 [System.IO.File]::ReadAllBytes($Dll) -> L73-L75 SHA256 + BitConverter.ToString().Replace('-','') -> L76 [int64]$dllBytes.Length -> L79-L87 在根对象结束符前插入 -> L88 WriteAllText (UTF8 no BOM); L92 catch 写 stderr; L93 $code=0 时 exit 1, $code=2 时保留 2. LastIndexOf('}') 取到的是文件中最后一个 '}' , 即根对象结束符 (字符串内的 '}' 必在它之前), 对合法 JSON 成立.
- 可复现命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' | Select-Object -Skip 62
- 最小修复范围: 无需再改; 保留 L68-L95.
- 尚缺中央验证: 真实 PASS/FAIL 两条路径的字段值与退出码需中央验证; 未构建, 未运行.

### B5 [P1] 字节级现状 (修复后)

- 绝对路径与行号: run-release-gates.ps1 L1-L96.
- 触发条件: 任何后续编辑或提交.
- 契约: 脚本 no BOM + CRLF; 不写 C:.
- 控制流: 只读验证: 脚本 len=4989, CRLF=96, LF-only=0, 控制字符=0, 首字符 U+0023, 末尾 CRLF; SHA256=61901BCDC4D9B7DE13114C0A08EF22845F17FF865BE21A9DB9768590105E4739; git diff 仍为 36 insertions / 2 deletions (r14 实现 + 本次 L66 修复).
- 可复现命令: $b=[System.IO.File]::ReadAllBytes('G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'); $t=[System.Text.Encoding]::UTF8.GetString($b); 'len={0} crlf={1} ctrl={2}' -f $b.Length,([regex]::Matches($t,'\r\n').Count),(@(for($i=0;$i -lt $t.Length;$i++){ $c=[int][char]$t[$i]; if($c -lt 32 -and $c -ne 10 -and $c -ne 13 -and $c -ne 9){$i} }).Count)
- 最小修复范围: 编辑时保持 no BOM 与 CRLF.
- 尚缺中央验证: 未构建, 未运行.

### B6 [P0] 编辑前基线 (本次会话动手之前)

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L12, L63-L95; L66 在 tools 与 uild-gatesProgram.cs 之间含 U+0008.
- 触发条件: 任何解析, 审查, diff 或提交该脚本时.
- 契约: 编辑前工作树已包含 r14 首次尝试的身份回写实现 (L12 -Json 说明, L63-L95 回写块); HEAD 版本不含该实现; 本次只允许修注释, 不允许改运行语义.
- 控制流: 编辑前脚本 len=4987, CRLF=96, LF-only=0, no BOM, 首字符 U+0023; 控制字符恰好 1 个 (U+0008); git diff 为 36 insertions / 2 deletions.
- 可复现命令: git -C 'G:\omp works\Sts\sts2-spire1' show HEAD:tools/build-gates/run-release-gates.ps1 | Select-Object -First 70; git -C 'G:\omp works\Sts\sts2-spire1' diff -- tools/build-gates/run-release-gates.ps1
- 最小修复范围: 只修 L66 的 U+0008 子串为 tools\build-gates\Program.cs.
- 尚缺中央验证: 未构建, 未运行.

### B7 [P0] 实现摘要 (本次会话实际写入)

- 绝对路径与行号: run-release-gates.ps1 L66 (唯一改动点); L12 与 L63-L95 逐字保留.
- 触发条件: -Json 指定且 $code 为 0 或 2; 未指定 -Json 时 L68 为假, 无副作用.
- 契约: 顶层新增 dllSha256 (大写 SHA256, BitConverter.ToString().Replace('-','')) 与 dllLength (Int64, [int64]$dllBytes.Length), 二者来自同一 [System.IO.File]::ReadAllBytes($Dll) 快照; 原 JSON 字段与 gate tool stdout/退出码保留; 写回失败时 $code=0 改判 exit 1, $code=2 保留 2 并在 stderr 报错; 不改 gate tool 二进制, 不新增依赖.
- 控制流: L66 由 `# (tools<U+0008>uild-gatesProgram.cs L103-L115); ...` 改为 `# (tools\build-gates\Program.cs L103-L115); ...`; L68-L95 身份回写逻辑逐字未动; 保持 PowerShell 5.1 兼容语法 (try/finally, .Replace, BitConverter, UTF8Encoding($false)).
- 可复现命令: $t=[System.IO.File]::ReadAllText('G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'); $t.IndexOf([char]8)  # 期望 -1
- 最小修复范围: 已完成, 无新增改动.
- 尚缺中央验证: 未构建, 未运行; 真实 PASS/FAIL 路径字段值与退出码需中央验证.
## 进行中

- 源码实现已收口: 本次会话只修 L66 的 0x08 控制字符, r14 首次尝试的 L12/L63-L95 身份回写实现原样保留.
- 无更多源码改动计划. 待中央验证: 真实 DLL 上跑 PASS 与 FAIL 两条路径, 对照 Get-FileHash 检查 dllSha256/dllLength, 并确认 -Json 未指定时 stdout 与退出码不变.

## 未知

- 未构建, 未测试, 未部署, 未启动游戏, 未修改 Steam 与共享 mod_configs.
- PowerShell 5.1 ConvertFrom-Json 的容量上限未验证; 门禁工具异常退出时的残留文件行为未枚举.
- 本报告未独立核实会话模型路由元数据; 请求文件声明 global:deepseek-v4.1-flash @ gateway/wb2api.
- 当前未知 (补充): 报告文件曾出现读取缓存/空壳观感; 以磁盘字节为准, 本次已复核 len 与三段结构.
- 当前未知 (补充): 未核实中央发布脚本调用方是否假定 JSON 无新增字段; 兼容性需中央验证.
- 当前未知 (补充): 未核实门禁工具 JSON 若包含顶层尾随空白/注释时的字节保留行为; 仅对合法 JSON 根对象结束符做插入.