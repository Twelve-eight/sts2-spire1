# Release gates DLL identity worker r14 - 2026-10-04

## 已确认

### C1 [P0] 现状: -Json 只透传, 不记录 DLL 身份

- 绝对路径与行号:
  - G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L12 (-Json 说明), L19-L24 (参数), L31-L37 (Dll 解析与存在性检查), L53-L62 (门禁调用与退出).
  - G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs L27 (Fail 返回 3), L103-L113 (JSON 写入 dll/passed/gates), L115 (写 JSON 后 return 0 或 2).
- 触发条件: 调用方传 -Json <out> 且门禁工具退出码为 0 或 2.
- 契约: 保留工具 JSON 原字段, 顶层新增 dllSha256 (大写 SHA256 十六进制) 与 dllLength (Int64 字节长度); 不计算 JSON 自身哈希.
- 控制流 (现状): L55 组装 argv -> L56 追加 --json -> L59 执行 dotnet -> L60 取 $LASTEXITCODE -> L62 直接 exit $code. 脚本内无 SHA256/长度计算, 无 JSON 回读/回写.
- 可复现命令 (只读, 本次已执行同类读取):
  - Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  - Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs'
- 最小修复范围: 仅 run-release-gates.ps1 头部 L12 与 L53-L62; 不改 Program.cs, 不改 gate tool 二进制.
- 尚缺中央验证: 未构建, 未运行; 真实 DLL 的字段值与退出码透传需中央验证.

### C2 [P0] 身份字段只能在工具确实写出 JSON 后追加

- 绝对路径与行号: Program.cs L103-L115; run-release-gates.ps1 L59-L62.
- 触发条件: -Json 指定, 工具退出码为 0 (PASS), 2 (门禁 FAIL), 3 (用法/输入错误), 或异常退出码.
- 契约: 工具已写出 JSON 才追加; 追加失败必须明确失败, 不得伪造 PASS; 不得吞掉工具原退出码.
- 控制流 (源码证据): Program.cs 仅在 jsonOut 非空时 File.WriteAllText (L111), 之后 return anyFail ? 2 : 0 (L115); Fail 路径 (L27) 返回 3 且不写 JSON. 结论: 退出码 0/2 => 本次运行刚写出 JSON; 退出码 3/异常 => 不把旧文件当作本次结果.
- 可复现命令 (只读): Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\build-gates\Program.cs' -Pattern 'File.WriteAllText|return anyFail|static int Fail'
- 最小修复范围: 脚本仅在 $code -eq 0 -or $code -eq 2 时回写; 其余退出码跳过并原样透传.
- 尚缺中央验证: 异常退出码的数值与部分写入行为未在本次枚举; 中央需在真实 DLL 上跑 PASS 与 FAIL 两条路径.

### C3 [P1] 哈希与长度必须来自同一 DLL 快照

- 绝对路径与行号: 计划修改 run-release-gates.ps1 L53-L62.
- 触发条件: -Json 指定且工具退出码 0/2, 需要写 dllSha256 与 dllLength.
- 契约: 二者来自同一文件快照; 路径使用已通过 L34 Test-Path 的 $Dll; 不计算 JSON 哈希.
- 控制流 (计划): 单次 [System.IO.File]::ReadAllBytes($Dll) -> [System.Security.Cryptography.SHA256]::Create().ComputeHash(bytes) -> [System.BitConverter]::ToString(hash) -replace '-','' (大写 64 位) -> [int64]$bytes.Length. 不用 Get-FileHash + Get-Item 两次独立读取.
- 可复现命令 (只读): Get-Command Get-FileHash | Select-Object Name,Version (确认 PS 5.1 可用性, 本次不执行门禁).
- 最小修复范围: 上述单次读取内联在 -Json 回写分支; 无新增依赖.
- 尚缺中央验证: 脚本快照与门禁工具内部读取之间存在 TOCTOU 窗口; 中央需在真实 DLL 上用 Get-FileHash 对照写入值.

## 进行中

- 首条证据已落盘 (C1-C3). 下一步: 按 C1-C3 对 G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 做最小实现. 产品脚本尚未编辑.

## 未知

- 未构建, 未运行, 未部署. 行为正确性需中央验证.
- PowerShell 5.1 的 ConvertFrom-Json/ConvertTo-Json 会把非 ASCII 字符转义为 \uXXXX; 字段与值语义保持, 但 JSON 字节格式会变化. 是否满足发布记录要求待中央判定.
- 门禁工具异常退出 (非 0/2/3) 时是否留下部分 JSON 未枚举; 实现将保守跳过身份写入并保留原退出码.
- 本报告未独立核实会话模型路由元数据; 请求文件声明 global:deepseek-v4.1-flash @ gateway/wb2api.