# resume-script-supervisor-r2 监督报告

- 角色: 两脚本监督审查者 (与接续实现者 Darwin 同批派发)
- 实现者精确 id: `01a10873-550a-7690-8e8a-e17bdd04f80e`
- 指定模型: `global:deepseek-v4.1-flash`, reasoning max; 请求渠道 gateway/wb2api; 更细路由本会话工具未暴露, 不宣称
- 审查对象 (最终冻结, 本审查时点实测):
  - `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` SHA256 `5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279`, 24710 bytes
  - `G:\omp works\.tooling\refresh-workshop-payloads.ps1` SHA256 `EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C`, 83057 bytes
- 约束执行: 只静态审查; 未执行 parse/build/lint/test/pack/预处理/部署/Steam/游戏; 未改代码; 未再委派; 唯一写入为本报告
- 结论: NEEDS_REWORK (1 项 P2, 见下)

## 已确认

### 门禁证据与 hash 绑定
- 本会话对精确 id `01a10873-550a-7690-8e8a-e17bdd04f80e` 的真实 `wait_threads` 实测返回: `timedOut=false`, `wake.reason=turnCompleted`, `wake.turnId=01a10873-556a-77a0-8336-55798ca4b85e`, `thread.status=idle`, `latestTurn.status=completed`, `completedAt=1791143529`, `error=null`.
- 主会话激活文件 `resume-supervision-r2-activation.txt` 与 `resume-finalizer-r2-wait-gate.json` (CheckedAt 2026-10-05T03:55:48.4699581+08:00) 与本会话实测一致; 实现者声明零代码改动; 两文件 hash 与本报告头部逐一相同.
- 以上仅为门禁与静态审查证据, 不代表任何实机通过.

### [P2] Workshop promote 目录 reparse 检查盲区 (NEEDS_REWORK)
- 优先级: P2
- 文件与行号: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` (hash 5CBD6F25...) L407-411 为检查点 (盲区在 L409 的条件), L412-414 为写入点.
- 触发条件: 以 Configuration=Release 且非 PlanOnly 运行 `-Promote` 且 canonical 各门禁通过; `$workshopRoot` (`<RepoRoot>\workshop\content\Spire1`) 已存在且为 reparse point (junction/symlink), 同时 `Spire1.dll`/`Spire1.json`/`Spire1.pck` 三个 leaf 在该目录中均不存在.
- 权威契约: `docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md` 决策 3 "canonical 更新前必须验证所有目标路径和祖先链无 reparse,目标属于本仓 build 输出,不是 Steam/共享配置/C:."; 工作区硬边界 (AGENTS.md 2b): 不写 Steam 安装/共享 mod_configs/C:; 本请求审查面 "path/ancestor/reparse".
- 当前控制流: L319-321 仅校验 `$workshopRoot` 的字符串归属与 `workshop\content` 根自身非 reparse; L407-410 仅当某个 leaf 已存在时才对其调用 `Assert-SafeWriteTarget` (该调用会向上走祖先链, 所以 leaf 存在时可捕获 Spire1 目录的 reparse); L411 `New-Item -ItemType Directory -Force` 对已存在的 reparse 目录不重建; L412-414 `Copy-Item` 跟随 reparse 写入其目标; L419-430 的 leftovers 与 hash 回读均在 reparse 目标内进行, 不会重新检查 reparse.
- 影响: 三文件会被写入 junction/symlink 目标 (可位于 Steam 安装/C:/共享配置), 违反硬边界; 且当目标恰好只接收三文件时不会被后续检查发现.
- 对照: refresh 脚本的同类门禁是无条件目录级 (`refresh-workshop-payloads.ps1` L987-989 与 L1277 对目录路径直接 `Test-ReparseChain`), Build 脚本 Workshop 侧是条件 leaf 级, 存在不对称.
- 可复现命令 (静态定位, 只读): `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1' | Select-Object -Skip 406 -First 25`. 动态复现 (未执行, 留给中央): 在隔离副本把 `workshop\content\Spire1` 替换为指向空目录的 junction, 以 `-Promote` 运行, 观察写入 junction 目标而非拒绝.
- 最小修复范围: 在 L411 之前增加一行 `Assert-SafeWriteTarget $workshopRoot (Join-Path $RepoRoot 'workshop\content') 'workshop-root'` (目录不存在时该函数只检查已存在的祖先层级, 随后新建目录非 reparse; 目录已存在时无条件检查目录自身与祖先). 不改其它控制流.
- 尚缺的实机证据: 修复后的 junction 夹具动态验证; 本项为静态源码证据, 未做动态复现.

### 逐面核对 (通过项, 均绑定上述 hash 与行号)
1. 通过 - promote 门禁与 canonical DLL 绑定: L316-318 (`-Promote -PlanOnly` 与 `-Promote -Configuration Debug` 在写入前 throw); L333-354 (canonical 固定 `mod\.godot\mono\temp\bin\Release\Spire1.dll`, 缺失即拒 L342-344, `$dll` 必须等于 canonical L345-347, canonical 与 payload hash 必须相同 L348-354); bin/publish fallback 不可 promote. 证据命令: `git diff --no-index --numstat` 与逐行阅读 L316-354.
2. 通过 - 顺序与路径门禁: PCK 结构门禁 L285-292, DLL 门禁 L310-314, canonical 路径门禁 L338-341/L363-364, canonical 同步 L365-405, Workshop 写入 L407-431; canonicalDir 必须已存在 (L337) 且其自身与祖先被无条件 `Assert-SafeWriteTarget` 检查 (L338), 无同类盲区.
3. 通过 - 字节/mtime/临时文件/失败闭合/回读: temp PCK 长度/hash/mtime>=DLL 校验 L369-371; 安装后回读长度/hash/mtime/digest L378-384; 时间戳不伪造 L331-332/L371/L382; try/finally 清理 tempPck/tempDigest L365-405 (清理 L399-404); `PairAtomic=$false` 与 PCK 先/digest 后安装顺序 L389/L376-377, 失败 throw 不进入 Workshop promote; 半成品对会被 refresh provenance 门禁拒绝 (refresh L695-696 DIGEST_MISMATCH, L704-705 DIGEST_STALE); 三文件回读 L424-430 与 leftovers 检查 L419-423.
4. 通过 - refresh PDB 策略与 mtime 刷新: Spire1 `PublishPdb=$false` (L134) 与条件 allowlist (L564-568); staged PDB 触发 STAGED_FILE_NOT_ALLOWLISTED (L591-592, L1322-1325), fail-closed 不静默删除 (全脚本无 Remove-Item); 其它 6 行无该 key 保持默认 (L564-567). 同 hash 不同 mtime 走复制: L1339-1361 (role=pck 且 mtime 不同 -> `$sameHashNeedsCopy=true` -> L1367-1377 Copy-Item), mtime 读失败 -> PATH_UNREADABLE (L1353-1355) 并计入硬失败 (L1436, L1508).
5. 通过 - VerifyOnly/WhatIf 只读与门禁不弱化: L1263 `if (-not $VerifyOnly)` 包住全部三个 Copy 点 (L1368, L1399, L1423), WhatIf 在每个 Copy 前分支 (L1363-1365, L1395-1396, L1418-1420), 全脚本无其它写调用; Build 对 .original 为 123 行纯插入/0 删除 (`git diff --no-index --numstat -- '<orig>' '<new>'`), refresh 7 处删除全部为规格内替换 (注释行, Spire1 row, pdb entry, ALREADY_CURRENT+continue, badStatuses, resultFailures), 逐条核对无检查被移除或放宽.

## 进行中

- 无. 两脚本的静态审查面已全部完成; 未决项只有上述 P2 finding (需修复与夹具复验).

## 未知

- 动态验证面 (按约束未执行, 由中央负责): 四项目真实 Release Rebuild 与 producer digest 行为; Spire1 `-SkipBuild -Promote` 隔离演练; 全量 refresh 后 7/7 VerifyOnly; `-GuardsOnly`; r15 字节覆盖; junction 夹具 (含上述 P2 修复后).
- 外部并发写入 (TOCTOU) 未覆盖: 例如 canonical DLL 在 L348-354 校验后被外部进程替换; 本审查为静态控制流审查, 未做并发测试.
- 细路由元数据: 本会话工具未暴露 provider 细节, 不宣称超出用户指定的 gateway/wb2api 之外的路由.