# Release gates DLL identity supervisor r14 retry - 2026-10-04

- 审查对象: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1
- 实现者报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-retry-20261004.md
- 审查方式: 只读静态审查 + 只读命令 (git diff, Get-Content, PowerShell 5.1 Parser, check-agent-text.mjs). 未构建, 未测试, 未部署, 未启动游戏, 未修改 Steam 或共享 mod_configs, 未写 C:.
- 当前结论: S1-S5 静态通过; S6 语言门禁失败; 最终 verdict 见文末.

## 已确认

### S1 [P0] r14 retry 字节面与修复事实

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L1-L96; 修复点 L66; 新增回写块 L63-L95; 头部说明 L12, L15.
- 触发条件: 任何后续编辑, 提交或语言检查.
- 契约: 脚本 no BOM, CRLF, 无 C0 控制字符 (含 U+0008); r14 retry 只修 L66 的损坏子串, L68-L95 运行语义不变.
- 控制流: 只读验证. 当前 len=4989, no BOM, CRLF=96, LF-only=0, C0 control=0, U+0008 absent, first char U+0023, SHA256=61901BCDC4D9B7DE13114C0A08EF22845F17FF865BE21A9DB9768590105E4739; git diff = 36 insertions / 2 deletions; tools/build-gates 下只有该脚本 modified, Program.cs 与 gate tool 二进制未改.
- 可复现命令: $p='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'; $b=[System.IO.File]::ReadAllBytes($p); $t=[System.Text.Encoding]::UTF8.GetString($b); 'len={0} crlf={1} lf={2} ctrl={3}' -f $b.Length,([regex]::Matches($t,"`r`n").Count),([regex]::Matches($t,"(?<!`r)`n").Count),(@(for($i=0;$i -lt $t.Length;$i++){ $c=[int][char]$t[$i]; if($c -lt 32 -and $c -ne 10 -and $c -ne 13 -and $c -ne 9){$i} }).Count); (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash; git -C 'G:\omp works\Sts\sts2-spire1' diff --numstat -- tools/build-gates/run-release-gates.ps1; git -C 'G:\omp works\Sts\sts2-spire1' status --porcelain -- tools/build-gates
- 最小修复范围: 无 (S1 通过).
- 尚缺中央验证: 未构建, 未运行. 编辑前基线 (len=4987, 1 个 U+0008) 无法从 git 独立复现; 只能采信实现者报告 B6 与当前无控制字符的字节结果.

### S2 [P0] 门禁工具 JSON/退出码契约与脚本判定条件一致

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs L26, L45-L46, L55, L63, L77, L103-L113, L115; run-release-gates.ps1 L56, L59-L60, L68, L96.
- 触发条件: 调用方传 -Json, 门禁结果为 PASS 或 FAIL.
- 契约: 工具只在 L103-L113 写 JSON, 之后 L115 返回 0 (PASS) 或 2 (FAIL); 用法/输入错误经 L26 返回 3 且不写 JSON. 脚本在 $code 为 0 或 2 时才回写, 与工具写 JSON 的两条路径一致.
- 控制流: L103 jsonOut 非空 -> L111 File.WriteAllText -> L115 return anyFail ? 2 : 0. 脚本 L68 条件 ($Json -and ($code -eq 0 -or $code -eq 2)) 覆盖同一集合; L96 exit $code 透传 3 或其他码.
- 可复现命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs' -Pattern 'File.WriteAllText|return anyFail|static int Fail'; Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' | Select-Object -Skip 52
- 最小修复范围: 不改 Program.cs 与 gate tool 二进制; 脚本判定条件无需修改.
- 尚缺中央验证: 未构建, 未运行; PASS/FAIL 两条真实路径的 JSON 字段与退出码需中央验证.

### S3 [P0] 身份回写逻辑静态审查

- 绝对路径与行号: run-release-gates.ps1 L68-L95.
- 触发条件: -Json 指定且 $code 为 0 或 2.
- 契约: 保留门禁工具 JSON 原字段; 顶层新增 dllSha256 (大写 SHA256) 与 dllLength (Int64); 两者来自同一实际 DLL 快照; 写回或身份计算失败必须显式失败, 不得伪造 PASS; 用法/输入错误不应被错误改写.
- 控制流: L70 Test-Path -LiteralPath $Json -PathType Leaf, 缺文件即 throw; L72 单次 [System.IO.File]::ReadAllBytes($Dll) 形成同一快照; L73-L75 SHA256 + BitConverter.ToString().Replace('-','') 得大写十六进制; L76 [int64]$dllBytes.Length; L79 ReadAllText($Json); L80 ConvertFrom-Json 验证原 JSON; L81-L82 LastIndexOf('}') 定位根对象结束符, 找不到即 throw; L83-L84 在根对象结束符前插入逗号 (空对象除外); L86-L87 生成新文本, 保留 Substring($close) 的尾随字节; L88 WriteAllText UTF8 no BOM; L89 成功日志; L91-L94 catch 写 stderr, $code=0 时 exit 1, $code=2 时保留 2; L96 exit $code.
- 契约对应: 同快照 (L72); 大写 SHA256 (BitConverter 默认大写); Int64 (L76); 原字段保留 (只插入, 不重序列化); 显式失败 (L70, L82, L91-L94); 用法错误不改写 (L68 排除 code 3).
- 可复现命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' | Select-Object -Skip 62
- 最小修复范围: 无需修改 (S3 静态通过).
- 尚缺中央验证: 未构建, 未运行. 真实 DLL 上 PASS 与 FAIL 两条路径的 dllSha256/dllLength 值需与 Get-FileHash 对照; 插入后 JSON 的合法性需中央运行验证. 理论边界: 若工具 JSON 形态变化 (非对象根或已有同名字段), 当前插入逻辑不覆盖; 当前 Program.cs L105-L110 固定为对象且无同名字段, 故静态不阻塞.
### S4 [P0] PowerShell 5.1 兼容, 依赖, 路径与二进制边界

- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L63-L95 (新块), L40-L42 (既有缓存重定向), L72-L76, L88, L96.
- 触发条件: 在 PowerShell 5.1 下执行脚本, 或任何后续改动该文件.
- 契约: PS 5.1 兼容; 无第三方依赖; 新逻辑无硬编码路径; 不修改 gate tool 二进制.
- 控制流: powershell.exe 5.1.19041.6328 对脚本做 Parser::ParseFile 与 ACP (gb2312) 解码 ParseInput, 两者 parseErrors=0; 新块只用 BCL 类型 (System.IO.File, System.Security.Cryptography.SHA256, System.BitConverter, System.Text.UTF8Encoding, ConvertFrom-Json, Test-Path -LiteralPath); L63-L95 无字面量文件路径; git status --porcelain -- tools/build-gates 只显示 run-release-gates.ps1 为 M, Program.cs 与 gate tool 二进制未改.
- 可复现命令: powershell.exe -NoProfile -Command '$t=$null;$e=$null;$null=[System.Management.Automation.Language.Parser]::ParseFile("G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1",[ref]$t,[ref]$e); "PS="+$PSVersionTable.PSVersion.ToString()+" parseErrors="+$e.Count'; Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' | Select-Object -Skip 62; git -C 'G:\omp works\Sts\sts2-spire1' status --porcelain -- tools/build-gates
- 最小修复范围: 无 (S4 通过).
- 尚缺中央验证: 未构建, 未运行; 仅静态解析, 未在 PS 5.1 真实执行. 观察 (非阻塞, 既有): 脚本为 UTF-8 no BOM, PS 5.1 按 ACP 读取, 中文提示在 5.1 控制台可能显示为乱码; 解析已验证安全, 非 r14 引入.

### S5 [P1] JSON 合法性, 尾随空白, 重复字段, 路径安全与最小改动

- 绝对路径与行号: run-release-gates.ps1 L68-L88; G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs L103-L113 (JSON 生产者), L105-L110 (payload 字段).
- 触发条件: -Json 指定且 $code 为 0 或 2, JSON 为工具写出的合法对象.
- 契约: 插入后 JSON 合法; 尾随空白不破坏; 不产生重复字段; 路径安全; 改动最小.
- 控制流: L80 ConvertFrom-Json 先验证; L81-L82 LastIndexOf('}') 对合法对象根成立; L83-L84 仅在根对象结束符前插入逗号 (空对象 {} 不加逗号); L86-L87 只插入两个字段, 原文其余字节保留, Substring($close) 保留 } 之后的尾随空白; L88 UTF8 no BOM. 重复字段: 当前生产者 L105-L110 无 dllSha256/dllLength, 且每次运行 L111 File.WriteAllText 先重写 JSON, 故重复运行不会累积重复字段. 非对象根 (如 null) 时 LastIndexOf('}') 为 -1, 显式 throw, 不产生损坏文件. 路径: 沿用调用方 $Json, Test-Path 用 -LiteralPath, 无新增路径面或硬编码路径.
- 可复现命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' | Select-Object -Skip 67; Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs' -Pattern 'dll = dllPath|passed =|gates ='; git -C 'G:\omp works\Sts\sts2-spire1' diff --numstat -- tools/build-gates/run-release-gates.ps1
- 最小修复范围: 无 (S5 静态通过).
- 尚缺中央验证: 未构建, 未运行; 真实 PASS/FAIL 输出的插入后 JSON 需中央用 ConvertFrom-Json 复核. 残留 (非阻塞): 若未来 Program.cs 改为自带同名顶层字段, 当前脚本会写出重复键; 当前生产者不可能触发; 未来可在插入前检查键存在性.

### S6 [P0] 语言字符门禁失败 (唯一阻塞项)

- 绝对路径与行号: run-release-gates.ps1 L1, L5, L15, L17, L64, L66, L67, L71, L78 (U+3002 全角句号); 实现者报告 G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-retry-20261004.md 通过检查器.
- 触发条件: 对脚本运行项目参考检查器, 或按监督请求第 6 项审查语言面.
- 契约: 报告与脚本只允许中文/英文/法文/德文/俄文与 ASCII punctuation 及 ASCII 控制字符; U+3002 不在允许集内.
- 控制流: & node 'G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs' --file 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1' -> Rejected code point U+3002, exit 2; 同命令对实现者报告 exit 0 (agent text accepted). C0 控制字符: 脚本与报告均为 0 个 (U+0008 已消失). 当前脚本共 9 个 U+3002: HEAD 已有 4 个 (L1, L5, L15, L17), r14 新增 5 个 (L64, L66, L67, L71, L78).
- 可复现命令: & node 'G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs' --file 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'; $t=[System.IO.File]::ReadAllText('G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'); $t.IndexOf([char]0x3002)
- 最小修复范围: 把脚本内全部 9 个 U+3002 替换为 ASCII '.' 或删除 (仅注释与行尾标点, 不动运行语义); 修复后保持 no BOM + CRLF, 重跑检查器至 exit 0. 这是本次唯一需要返工的门禁项.
- 尚缺中央验证: 未构建, 未运行; 修复后仍需中央验证并记录.

## 进行中

- 中央验证待办 (本轮按指令未执行): 在真实 DLL 上运行 PASS 与 FAIL 两条路径, 复核 dllSha256 与 Get-FileHash 一致, dllLength 与文件字节一致, 插入后 JSON 可被 ConvertFrom-Json 解析, 退出码 0/2/3 与原逻辑一致, -Json 未指定时 stdout 与退出码不变.
- 返工待办: S6 的 9 个 U+3002 替换为 ASCII '.' 并重跑检查器.

## 未知

- 未构建, 未测试, 未部署, 未启动游戏, 未修改 Steam 或共享 mod_configs, 未写 C:.
- 实现者报告 B6 的编辑前基线 (len=4987, 1 个 U+0008) 无法从 git 独立复现; 只采信当前字节结果 (无控制字符, SHA256=61901BCDC4D9B7DE13114C0A08EF22845F17FF865BE21A9DB9768590105E4739).
- PowerShell 5.1 ConvertFrom-Json 容量上限未验证; 当前门禁 JSON 很小 (3 个 gate 结果), 静态不阻塞.
- 门禁工具异常退出时残留 JSON 文件行为未枚举; 脚本对 code 3 不写不改, 静态符合契约.
- 监督会话实际模型路由元数据未在本会话内独立核实; 请求文件声明 global:deepseek-v4.1-flash @ gateway/wb2api. 依 AGENTS.md Sec 4b, 中央应以 session metadata 记录实际路由.
- 身份绑定为门禁运行后脚本单次 ReadAllBytes; 门禁工具读取 DLL 与脚本读取 DLL 之间存在理论 TOCTOU 窗口 (极小); 本轮契约只要求 hash 与 length 同源, 静态不阻塞; 未来可在 Program.cs 内同一读取处计算身份以完全闭合.

## 最终结论

REWORK_REQUIRED

- 原因: 六项门禁中 S1-S5 静态通过; S6 语言字符门禁失败 (check-agent-text.mjs exit 2, U+3002).
- 返工范围: 仅 run-release-gates.ps1 的 9 个 U+3002 替换为 ASCII '.' 或删除, 保持 no BOM + CRLF, 重跑检查器至 exit 0.
- 未构建, 未测试, 未部署; 本结论是只读静态审查结论, 不代表实机或运行通过.
- 本报告已通过 check-agent-text.mjs --file 检查 (exit 0).