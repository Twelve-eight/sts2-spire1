---
name: sts2-workshop-provenance
description: 修复和验证此工作区 STS2 创意工坊发布的来源一致性, 精简 Spire1 PCK, 处理过期包与缺失摘要, 仅在明确调用时使用.
---

# STS2 Workshop 来源门禁与准备

## 入口与硬边界

先读 G:\omp works\START-HERE.md, G:\omp works\docs\WORKSPACE-PROJECTS.md 和项目 DEVELOP/DEVLOG. 项目路径以迁移映射为准. 准备不等于上传授权.

- 不写 G:\steam\steamapps\common\Slay the Spire 2, 不改共享 mod_configs, 不启动/停止用户游戏.
- 构建显式 CopyToModsFolderOnBuild=false 与 Sts2Path=E:\Slay the Spire 2. 所有 .NET/NuGet/TEMP/TMP 在 G:, 先核对实际进程环境; no-restore 只用于既有已解析依赖, 不伪称一次干净还原.
- 只用当次用户指定模型与当前原生子代理. 实现和监督同批, 实现者不运行 build/test. 主会话实际 wait completed 并落盘后才激活监督. 请求模型不代替安全 session 元数据; 未暴露渠道写 Unknown.
- 每个代理有唯一增量报告, 第一条可用结论立即落盘, 按已确认/进行中/未知分段.
- 不 git add -A, 不重置已知 dirty/advice删除/research子模块. 仅用精确路径或基线生成的 producer-only patch 暂存自己的改动.

## 三种身份与错误处理

源码与 source manifest, canonical Release build, Workshop staging 是三种身份. Root .tooling\refresh-workshop-payloads.ps1 从 build单向刷新staging, 无旧包回填或source PCK fallback. 单项wrapper的 -Only 只限制上传, 正式入口的前置检查仍全量.

- PCK_STALE: 通过真正 pack 生成当前 DLL之后的PCK, 不修改mtime.
- PCK_DIGEST_MISSING / DIGEST_MISMATCH / DIGEST_STALE: 修复producer并真正pack, 不手工补 SHA256来让门禁变绿.
- REBUILD_REQUIRED: 源码晚于DLL, 真正重建后重新绑定证据.
- ALREADY_CURRENT不能只看hash: PCK hash相同而mtime不同仍须复制并回读. VerifyOnly/WhatIf不得写入.

Quick PckPacker 的 Begin先记录Utc pack起点并作废旧digest. Write仅接受成功exit0非skip, 验证存在/非空/PCK不早于pack和DLL, 从真实PCK字节计算ASCII64hex并回读. 失败/跳过不重标旧包; 禁用/inner-export不活动. 四个项目目前没有统一强制禁用inner pack, 若显式inner-export, 可能产生旧digest不匹配; consumer仍须fail-closed拒绝.

## Spire1 精简包

使用 G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1 和全新 G:\omp works\.tmp 子目录. Promote只接受Release且非PlanOnly, 不能用bin/publish fallback DLL. SkipBuild只有canonical DLL及源码身份仍可证明时才适用.

- 内容门禁先通过, payload DLL与canonical DLL同hash, 再同步canonical精简PCK/digest, 最后更新Workshop三文件.
- 无条件检查Workshop目录自身和所有祖先的reparse, 即使三个leaf均不存在. 同样验证canonical目录/临时叶/最终叶. 空junction是必要负例, 不得只检查存在leaf.
- canonical的PCK先/digest后不是双文件事务原子. 任一失败中止, 半成品须由consumer拒绝; 不能为成功声明伪造mtime或摘要.
- Spire1发布恰为DLL/JSON/PCK. 通用refresh拒绝PDB/deps/digest/其它未知文件, 不静默删除未知物; 其它项目optional PDB策略保持原契约.
- 新源码/新DLL/PCK/manifest hash不能引用旧游戏smoke. Promote前后核对源集合及字节; 有并行形态会话时尤其如此.

## 中央复验

依次执行: 旧版隔离复现, 实现后独立监督, PS7/PS5.1解析与路径负例, producer真实包工具的隔离矩阵, 各项目真实Release Rebuild, Spire1真实Promote, 全量refresh后VerifyOnly, 真正单项wrapper GuardsOnly.

producer矩阵至少success/skipped/pack-failure/disabled/inner-export/missing-dll. 合成metadata DLL不是游戏DLL, 该矩阵不能称游戏验收. 本机工厂的Fragment自动尾部是return Success, Success默认true; 不等于Log.HasLoggedErrors. 成功路径和catch都显式return会使该尾部不可达并引入CS0162. 可删除成功路径return true并保留missing/error的false, 必须用当前工厂契约和真实重建验证, 不suppress警告.

PS5.1会把native stderr包装为ErrorRecord. 测试器在预期失败的子进程调用周围临时用Continue捕获输出与LASTEXITCODE, 然后恢复Stop; 不在产品里放宽门禁. 夹具提前中断不是完整失败/通过计数, 修验证器后完整重跑.

GuardsOnly必须到达正式wrapper, 它在任何Steamcontact前退出, 不读凭据,不消耗冷却. 不把静态监督,隔离夹具,构建,payload来源一致或headless首回合写成UI/读档/多人/性能验收.

## 交付与备份

本机central原始证据保存在 G:\omp works\.tmp\workshop-prep-20261004-central; 最终报告与DEVLOG应给出当前原始命令输出和hash, 不复制漂移计数. 根.tooling不是Git仓, 改动脚本保存到 G:\omp works\Sts\sts2-spire1\tools\release\workspace-snapshots, 复核精确hash后commit/push. 版本化文本因Git换行正规化可能与根脚本不同, 所以验收时同时比较工作树精确字节与记录的SHA256, 不把Git blobhash冒充SHA256.

只在用户明确要上传时提供或执行上传命令. 不建议常规Force: 它只覆盖本地登录冷却, 不能绕来源门禁, 仅确实切换IP且用户明确选择时使用. 正常使用单项wrapper, 不直接调用SteamCMD.

## Freshness扫描与并行源码冻结

- 目录排除必须作用于repo相对路径, 而非绝对FullName. workspace .tmp只是合法隔离Root祖先, 不能吞掉全部生产输入. 相对路径加前导分隔符能保留原根级/嵌套ignored dirs语义.
- 当前七项目静态依赖核对后, 仅根docs下JSON报告/交接快照排除; docs中的cs/csproj/props与mod/.../docs runtime JSON仍扫描. 新项目若把根docs JSON引入生产依赖, 先收紧契约和过滤, 不盲用当前排除.
- 13路径隔离矩阵同时覆盖文档正例,生产输入负例和repo内tools/research/obj排除. 旧版在.tmp Root下的文档正例会被绝对路径漏查掩盖, 不能当作文档误报复现; 文档误报须看真实Sts日志.
- 原有freshness在refresh复制后的最终核验中执行, 并非整个refresh的事务式前置条件. 因REBUILD_REQUIRED退出不代表没有文件复制. 只读VerifyOnly和GuardsOnly不复制, 但regenerate失败可能已经刷新其它合法行; 必须保留复制前备份及原始日志, 不编造回滚或无写入结论.
- 并行会话改源码后, 旧DLL/PCK同hash只说明旧字节未变, 不说明对应当前源码. 不为旧产物改mtime或扩大排除目录. 本轮6/7来源通过不能替代全量门禁; -Only限制上传不豁免正式入口的全量检查.