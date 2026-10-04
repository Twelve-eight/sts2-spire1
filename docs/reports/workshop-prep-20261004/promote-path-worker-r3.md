# promote-path-worker-r3 实现报告

- 任务: 修复同批监督报告 `resume-script-supervisor-r2.md` 的 P2 - `workshop\content\Spire1` 为空 junction 时条件 leaf 检查漏过.
- 唯一允许修改的产品文件: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
- 指定模型: `global:deepseek-v4.1-flash`, reasoning max, 请求渠道 gateway/wb2api; 更细实际路由以安全会话元数据为证, 本报告不宣称.
- 约束: 只写本报告与上述产品文件; 不构建, 不 parse, 不 lint, 不 pack, 不测试, 不部署, 不提交; 不写 C:.
- 修改前 hash: `5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279`, 24710 bytes, CRLF 438 / 仅 LF 0, 无 BOM.
- 修改后 hash: `F38EC7F5E3E03061AC708903B8BCF537ECF7C81BA86F03AA70404A2FABC7D8DA`, 24809 bytes, CRLF 439 / 仅 LF 0, 无 BOM.

## 已确认

### C1 [P2] 目标盲区与最小修复位置 (静态证据, 第一条结论)
- 优先级: P2
- 绝对路径与准确行号 (修改前): `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
  - L319-321: `$workshopRoot` 仅做字符串归属检查与 `workshop\content` 根自身的 `Assert-NoReparse`; 未检查 `$workshopRoot` 自身.
  - L407-410: 仅当 leaf (`Spire1.dll`/`Spire1.json`/`Spire1.pck`) 已存在时才调用 `Assert-SafeWriteTarget $leaf ... 'workshop-payload-file'` (L409 为条件调用).
  - L411: `New-Item -ItemType Directory -Force -Path $workshopRoot` 对已存在的 reparse 目录不会重建/拒绝.
  - L412-414: `Copy-Item -Destination $workshopRoot\<name>` 会跟随 reparse 写入其目标.
  - L419-430: leftovers 与 hash 回读都在 reparse 目标内进行, 不会重新检查 reparse.
- 触发条件: `-Promote` 且 `Configuration=Release` 且非 `PlanOnly`, canonical 各门禁通过; `$workshopRoot` 已存在且为 reparse point (junction/symlink), 且三个 leaf 均不存在 (空 junction).
- 权威契约: `G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md` 决策 3 - canonical 更新前必须验证所有目标路径和祖先链无 reparse, 目标属于本仓 build 输出, 不是 Steam/共享配置/C:.
- 当前控制流: L407 循环无 leaf -> 无任何 `Assert-SafeWriteTarget` 调用 -> L411 `New-Item -Force` 对已存在 reparse 目录返回成功 -> L412-414 跟随 reparse 写入. 监督报告已给出 G: 隔离 AST 夹具结果 `empty-junction: Passed=False Exit=0 TargetUnchanged=False`, 证据位于 `G:\omp works\.tmp\workshop-prep-20261004-central` 下 `promote-path-before-ps7-*\results.json` 与 `run.log`.
- 可复现命令 (静态, 只读): `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1' | Select-Object -Skip 406 -First 25`
- 最小修复范围: 在 L321 现有 `workshop-content-root` 检查之后, 任何 canonical/Workshop 写入之前, 对 `$workshopRoot` 无条件调用 `Assert-SafeWriteTarget $workshopRoot (Join-Path $RepoRoot 'workshop\content') 'workshop-root'`. 不重构, 不更改其余门禁/mtime/非原子顺序/默认行为.
- 尚缺的实机证据: 修复后的 empty-junction 夹具动态复验 (由主会话集中执行, 本实现者不运行).

### C2 修复已写入 (单行插入, 字节级自检)
- 修改后精确行号: 新增行位于 `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` L322, 原文如下:
  `    Assert-SafeWriteTarget $workshopRoot (Join-Path $RepoRoot 'workshop\content') 'workshop-root'`
- 位置上下文: L321 为既有 `Assert-NoReparse (Join-Path $RepoRoot 'workshop\content') 'workshop-content-root'`; L322 为新增无条件检查; L323 起为 `ShouldProcess`; canonical sync 与 Workshop 写入均在其后, 违规路径早拒绝.
- 未改动的原有控制流: L319-321 与 L408-433 (原 L407-431, 因插入行整体后移 1 行) 内容不变; 条件 leaf 检查仍保留, 仅新增目录级无条件门禁. 全文件 `Assert-SafeWriteTarget` 调用由 8 处变为 9 处 (新增 L322; 其余位置整体后移 1 行).
- 字节级自检 (只读, 未执行脚本): 删除新增行后以 UTF-8 无 BOM 重新编码, 得到 24710 bytes 且 SHA256 = `5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279`, 与修改前逐字节一致; 新文件 24809 bytes, CRLF 439 / 仅 LF 0, 首 3 字节 `35,32,66` (无 BOM); 全文件非 ASCII 字符数 0.
- 修改后精确 SHA256: `F38EC7F5E3E03061AC708903B8BCF537ECF7C81BA86F03AA70404A2FABC7D8DA`.
- 可复现命令 (静态, 只读): `(Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1' -Algorithm SHA256).Hash` 与 `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1' | Select-Object -Skip 315 -First 12`.
- 尚缺的实机证据: 修复后 empty-junction 夹具动态复验, 真实 `-SkipBuild -Promote` 隔离运行, 全量 refresh/VerifyOnly (按约束全部由主会话集中执行).

CODE_COMPLETE

## 进行中

- 无. 本实现者范围内的单行修复与静态字节自检已完成; 未决验证项全部属于主会话集中执行面.

## 未知

- 修复后的夹具行为与真实 `-Promote` 运行结果 (按约束不由实现者验证): empty-junction 是否 Passed=True 且 Exit 非 0, 非 reparse 正常目录是否仍能 promote, leaf 存在时条件检查与新增目录检查是否协同.
- 细路由元数据 (gateway/wb2api 之外的 provider 细节), 本会话工具未暴露.
- TOCTOU: 新增 L322 检查与 L412 写入之间的并发替换未覆盖, 本次为静态控制流修复, 未做并发测试.