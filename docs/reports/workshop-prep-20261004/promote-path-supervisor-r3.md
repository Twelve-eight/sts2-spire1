# promote-path-supervisor-r3 监督报告

- 角色: 同批监督审查员 (实现者 `promote-path-worker-r3` 精确 id `01a108a4-47b1-79a3-b836-d814ffa24557`)
- 审查对象 (激活后实测冻结): `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` SHA256 `F38EC7F5E3E03061AC708903B8BCF537ECF7C81BA86F03AA70404A2FABC7D8DA`, 24809 bytes, CRLF 439 / 仅 LF 0, 无 BOM, 全文件非 ASCII 0
- 基线: `G:\omp works\.tmp\workshop-prep-20261004-central\Build-Spire1Release.pre-junction-r3.ps1` SHA256 `5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279`, 24710 bytes
- 门禁记录: `promote-path-worker-r3-wait-gate.json` (CheckedAt 2026-10-05T04:41:51.4919151+08:00, Tool=multi_agent_v1.wait_agent, Target=01a108a4-47b1-79a3-b836-d814ffa24557, Status=completed, TimedOut=false), 与主会话激活消息一致
- 约束执行: 只静态审查; 未运行测试/build/lint/parse/pack/部署/提交; 未改产品代码; 未再委派; 唯一写入为本报告
- 结论: SUPERVISION_PASS (0 项 finding; 实机/动态验收面见"未知", 由主会话集中执行)

## 已确认

### C1 差异为纯单行插入, 与基线逐字节一致 (证据已独立复算)
- 独立字节核对: 取最终文件 L322 (`    Assert-SafeWriteTarget $workshopRoot (Join-Path $RepoRoot 'workshop\content') 'workshop-root'`, 前置 4 空格), 删除该行及其 CRLF 后重建得到 24710 bytes, SHA256=`5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279`, 与基线文件逐字节 `byteIdenticalToBaseline=True`。
- 文本行级核对: 最终 439 行 = 基线 438 行在 L322 处插入 1 行; 逐行 `-cne` 比较 `textMismatchCount=0`。
- 计数不变式: `Assert-SafeWriteTarget` 8->9 (仅新增 L322), `Assert-NoReparse` 5->5, `Assert-Under` 7->7, `ShouldProcess` 2->2, `PairAtomic` 1->1, `InstalledOrder` 1->1, `Remove-Item` 2->2。
- 可复现命令 (只读): `(Get-FileHash -LiteralPath '<final>' -Algorithm SHA256).Hash`; 行号定位 `Get-Content -LiteralPath '<final>' | Select-Object -Skip 315 -First 12`。
- 尚缺的实机证据: 无 (本项为字节级静态事实)。

### C2 新增门禁位置满足"任何 canonical/Workshop 写入之前且无条件目录级"契约
- 位置: 最终 L322 位于 L319 `$workshopRoot` 解析、L320 `Assert-Under`、L321 既有 `Assert-NoReparse workshop\content` 之后; L323 `ShouldProcess` 与 L324 注释起为 canonical sync 段。
- 所有写入点均在 L322 之后: canonical 写入 L367/L373/L377/L378/L398, Workshop 写入 L412/L414/L418; L322 之前无可写调用 (全文件写调用清单: L94-95, L136, L243, L280, L308 均为 staging/evidence 面, 不属于 canonical/Workshop)。
- 无条件性: 新增调用不在任何 `if`/循环内 (直接位于 L322 语句), 不依赖 leaf 是否存在; 既有条件 leaf 检查 (L408-410) 保留。
- 语义: `Assert-SafeWriteTarget` (L42-67) 先 `Assert-Under` (L47), 再拒绝 C:/steamapps/mod_configs (L49-51), 再从目标向上逐层检查 reparse (L53-58) 与 steam_appid 标记 (L60-62)。目录不存在时逐层跳过不存在的层, 新建目录后必然非 reparse; 目录存在时无条件检查目录自身与全部祖先。
- 契约对照: `docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md` 决策 3 "canonical 更新前必须验证所有目标路径和祖先链无 reparse, 目标属于本仓 build 输出, 不是 Steam/共享配置/C:" 与本实现一致; 工作区硬边界 (AGENTS.md 2b) 一致。
- 可复现命令 (只读): `Select-String -LiteralPath '<final>' -Pattern 'Assert-SafeWriteTarget' -Encoding UTF8`; `Get-Content -LiteralPath '<final>' | Select-Object -Skip 41 -First 26`。
- 尚缺的实机证据: 真实仓库路径上的 `-SkipBuild -Promote` 演练与 7/7 VerifyOnly (主会话集中执行)。

### C3 修复针对的失败模式已有前后对照的动态夹具证据 (只读复核, 非本会话运行)
- 修复前 (SourceSha256=5CBD6F25..., 与基线一致): `promote-path-before-ps7-20261005-043711\results.json` 中 `empty-junction: Passed=False Exit=0 TargetUnchanged=False`; 该夹具 `redirect-target\` 出现 `Spire1.dll/Spire1.json/Spire1.pck` 三文件, 证明旧版确实跟随 junction 写入目标目录 (即前轮 P2 复现)。
- 修复后 (SourceSha256=F38EC7F5..., 与本轮最终 hash 一致): `promote-path-after-ps7-20261005-044237\results.json` 七模式全部 `Passed=true` — `empty-junction` Exit=1 且 TargetUnchanged=True (拒绝信息为 `PATH-DENY [workshop-root]: reparse point ... Spire1 is not allowed.`); `ancestor-junction` Exit=1; `existing-leaf-junction` Exit=1 且目标字节未变; `normal-empty`/`normal-missing` Exit=0 且写出恰 3 文件 (正常路径未被误拒); `debug`/`plan-only` 仍按原门禁拒绝。
- 夹具构造: `run-promote-path-fixtures.ps1` 以 AST 从真实脚本提取 5 个 helper 函数与 `$Promote` 块生成 driver, `results.json` 记录 `EvidenceBoundary='Actual promote AST and helper functions, synthetic DLL/PCK; no product gates or game run'`。仅证明路径门禁行为, 不替代产品门禁或实机验收。
- 宿主边界 (诚实性): 上述前后夹具动态证据均在 PowerShell 7.6.5 下产生 (`promote-path-*-ps7-*`); PS5.1 侧只有解析证据 (`central-script-parse-ps51.json`, PSVersion 5.1.19041.6328, Errors 空), `promote-path-after-ps51-20261005-044236` 目录显示一次未完成的运行 (run.log 0 bytes, 无 results.json)。新增 L322 复用既有 helper 且形式与 L339-342 既有调用相同, 静态上无 PS 版本特化分支; 但"修复在 PS5.1 下的动态行为"未经证据覆盖, 列为未知。
- 可复现命令 (只读): `Get-Content -LiteralPath 'G:\omp works\.tmp\workshop-prep-20261004-central\promote-path-after-ps7-20261005-044237\results.json' -Raw -Encoding UTF8`; `Get-Content -LiteralPath '<同目录>\empty-junction\run.log' -Raw -Encoding UTF8`。
- 尚缺的实机证据: 产品级 PCK/DLL 门禁参与下的真实 promote; 本会话未执行, 不宣称。

### C4 既有门禁未弱化, 编码与换行保持
- 双文件非原子顺序: 仍为 PCK 先 (L377) / digest 后 (L378), `PairAtomic=$false` (L390), `InstalledOrder=@('Spire1.pck','Spire1.pck.sha256')` (L391) 未变。
- 失败闭合: try/finally 清理 temp 文件 (L400-405) 未变; 长度/hash/mtime 回读 (L370-385) 未变; Workshop leftovers/dirs/hash 回读 (L420-431) 未变。
- 编码: 无 BOM (首 3 字节 35,32,66), CRLF 439 / 仅 LF 0, 全文件非 ASCII 0, 与实现者自检及基线风格一致; 相对基线仅新增 1 行 CRLF (438->439), 其余字节一致。
- 仓库路径祖先链: 当前实测 `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1` 至 `G:\` 各级 `reparse=False`, `steam_appid.txt` 标记不存在, 因此新增门禁不会误拒本仓正常 promote 路径。
- 可复现命令 (只读): 逐层 `Get-Item -Force | Select-Object Attributes` 与 `Test-Path (Join-Path <level> 'steam_appid.txt') -PathType Leaf`; 编码检查 `[System.IO.File]::ReadAllBytes('<final>')`。
- 尚缺的实机证据: 主会话集中执行的旧版复现/修复后回归; 本报告不把源码审查称为实机验收。

### 逐面核对结论 (均绑定最终 hash F38EC7F5...)
1. PASS - 最小补丁范围: 仅新增 L322 一行, 无重构、无删除、无别名/stub。
2. PASS - 无条件目录级门禁: L322 不受 leaf 存在性影响, 覆盖 `$workshopRoot` 自身与祖先链, 含 junction/symlink 祖先场景 (夹具 ancestor-junction Exit=1)。
3. PASS - 写入前序: L322 早于 L367/L373/L377/L378/L398 canonical 写入与 L412/L414/L418 Workshop 写入, 亦早于 `ShouldProcess` 确认后的所有副作用。
4. PASS - 正常路径不回归: 夹具 normal-empty/normal-missing 均 Exit=0 且恰写 3 文件; 真实仓库祖先链无 reparse/Steam 标记。
5. PASS - 失败闭合与既有门禁: leaf 条件检查保留, canonical 双文件顺序/回读/清理未变, 无检查被放宽或移除。
6. PASS - 编码与换行: 无 BOM, CRLF 439/仅 LF 0, 非 ASCII 0。

## 进行中

- 无. 本监督范围内静态审查面全部完成; 未决项只有"未知"中列出的主会话集中动态/实机面。

## 未知

- 主会话集中执行的动态/实机验收尚未在本会话发生: 修复后 empty-junction 夹具在真实产品门禁链上的复验、真实 `-SkipBuild -Promote` 隔离运行、全量 refresh 后 7/7 VerifyOnly、`-GuardsOnly`、r15 字节覆盖; 本报告不宣称这些结果。
- 生产级行为边界: 夹具使用合成 DLL/PCK 且不含产品 PCK/DLL 门禁; 因此只证明路径门禁的控制流, 不证明完整 promote 语义。
- TOCTOU 并发面: L322 检查与 L412 写入之间若被外部进程替换为 reparse, 仍可能竞态; 与修复前同等暴露, 不在本轮最小补丁范围 (实现者报告已列为未知)。
- 细路由元数据: 本会话工具未暴露 provider 细节, 不宣称超出用户指定的 gateway/wb2api 之外的路由。
- PS5.1 动态行为: 修复后的夹具在 PS5.1 下未产生完整动态结果 (该侧目录仅 run.log 0 bytes); PS5.1 只有解析证据 (Errors 空)。主会话集中回归若在 PS5.1 下运行, 应补此覆盖。


