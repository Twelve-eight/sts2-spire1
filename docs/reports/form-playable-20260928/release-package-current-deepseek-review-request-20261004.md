你是只读发布包审查员. 审查当前 Spire1 源码驱动 Release payload 和未使用资产清理结果, 不修改产品代码.

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway/wb2api`, 思考层级 `max`. 只准使用当前 Codex harness 原生子代理设施, 不得换模型, 不得启动其它代理运行时, 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-package-current-deepseek-review-20261004.md`.
不得修改产品代码, 构建, 测试, 部署, 启动游戏, 写 Steam, 写 shared mod_configs 或 C:.

## 输入

发布契约: `G:\omp works\Sts\sts2-spire1\docs\RELEASE-PACKAGE-CONTRACT-20261003.md`.
重建脚本: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`.
PCK verifier: `G:\omp works\Sts\sts2-spire1\tools\release\Verify-Spire1Pck.ps1`.
当前输出目录: `G:\omp works\.tmp\spire1-release-r12-20261004-central`.
当前隔离 smoke: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun`.
历史包报告仅作上下文, 不直接采信:
- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\beta-package-audit-20261003.md`
- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\beta-package-r2-20261003.md`
协议: `G:\omp works\.tooling\subagent-report-protocol.md`.

## 审查目标

1. 当前 payload 是否恰有 `Spire1.dll`, `Spire1.json`, `Spire1.pck`, 且 hash 与 evidence 对应.
2. 当前 asset-manifest 的 kept/excluded 数量和路径是否遵守发布契约, 排除历史/模板/omega 资产是否有源码驱动理由.
3. PCK verifier 的当前输出是否通过, PCK entries/sourceFiles/digest 是否一致, 排除资产没有对应 ctex/import/json.
4. AssemblyRef, manifest, TypeDef gate 的当前 JSON 是否全 PASS, 与当前 DLL hash 绑定.
5. 当前 workshop payload 是否仍为历史字节或含 stale PDB/deps/旧 zip; 只读记录, 不清理, 不 Promote.
6. 区分当前 r12 证据和旧包字节; 旧 DLL hash 不匹配不得采信.

首条证据立即追加报告, 每完成一个检查面追加一次. 报告固定包含 `## 已确认`, `## 进行中`, `## 未知`. 给出准确路径和复现命令. 最终写 `SUPERVISION_PASS` 或 `SUPERVISION_REWORK`. 不把静态包审查写成玩家实机全路径通过.