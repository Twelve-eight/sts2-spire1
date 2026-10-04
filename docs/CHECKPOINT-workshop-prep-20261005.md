# Workshop推送前准备断点 - 2026-10-05

本文件供零上下文接续, 不是可上传证明. 时间使用本机Asia/Shanghai UTC+8; 2026-10-05早晨对应UTC的2026-10-04晚间, 不是时间戳问题.

## 已确认

- 用户授权准备, 未授权本轮实际上传. 不调用SteamCMD,不读凭据,不耗登录冷却,不写Steam安装/共享mod_configs,不操作游戏. 输出与缓存均在G:.
- 子代理唯一global:deepseek-v4.1-flash/max, 原生Codex multi_agent_v1, 当前上限12. 实测安全metadata为同model/effort及provider gateway, 更细wb2api路线Unknown. r4 Euler和Socrates完成并关闭, 完成门禁和路由记录位于G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004.
- Spire1 release脚本Promote先验证内容与DLL身份, 同步canonical精简PCK/digest后再更新Workshop三文件; 空Workshop junction无条件拒绝. release脚本SHA256 F38EC7F5E3E03061AC708903B8BCF537ECF7C81BA86F03AA70404A2FABC7D8DA.
- 四项目成功PackPck生成真实digest, 失败/跳过不重标旧包, 不依赖部署开关. CS0162已按实际工厂return Success契约消除. producer隔离矩阵24/24,最终真实Release Rebuild4/4且0 warnings/0 errors, 不部署. 原始结果four-mods-rebuild-results.json与producer-fixtures-20261005-045406/results.json.
- refresh修复同hash不同PCK mtime的错误ALREADY_CURRENT,Spire1不发布PDB,root docs JSON误报和外层.tmp祖先漏查. 保留repo内ignored dirs,docs源码及mod/.../docs runtime JSON. r4独立监督PASS, PS7和PS5.1各13/13新freshness与11/11负例, 四入口parse均通过. 最终refresh SHA256 35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD; 精确Git备份路径tools/release/workspace-snapshots/refresh-workshop-payloads.ps1.
- 真实全量refresh已更新Perfect,ChaosBridge,RegentFXFastBoot,MpConfigSync,HeartShake,QuriousCraftingRelics六行staging, 单项VerifyOnly六项exit0. 全量VerifyOnly exit1仅Spire1 REBUILD_REQUIRED. 正式Spire1 wrapper GuardsOnly exit12在全量provenance停止, 未触发Steam.
- refresh不是全局事务: 原有freshness在复制后核验. 首个中央验证器的无写入断言不成立, 已标invalid并保留22文件真实更新证据. 七行25文件复制前备份r4-staging-backup.json, 不回填旧包或改mtime来假装成功.
- r15 Spire1 staging仍三文件: DLL 900608 bytes SHA256 8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06; PCK 19669354 bytes SHA256 70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79; manifest 548 bytes SHA256 CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305.
- 四项目producer前序GitHub备份: Perfect 4625967, MpConfigSync 142d33c, HeartShake d1acbd6, QuriousCraftingRelics 5807b16. 本轮发布暂存/脚本/文档备份提交以同目录backup-completion-r4.json为最终证据.

## 进行中

- 当前唯一真实发布阻塞是另一个Forms独立化会话已改Spire1生产源码,项目文件和本地化, 而canonical/staging仍旧r15. 当前freshness指向mod/Spire1/localization/zhs/modifiers.json晚于DLL, 不是docs报告误报. 不覆盖并行修改, 不用旧r15游戏smoke证明新代码.
- 先等Forms独立监督和中央验收完成并冻结源码, 再以独占新输出重建Spire1及精简PCK, 绑定新hash和必要运行证据, 审核后Promote. 全量refresh/VerifyOnly和正式wrapper GuardsOnly必须重新跑, 不能用单项六行通过替代.
- -Only只限制正式上传范围, 不豁免全量安全门禁. 因Spire1阻塞, 目前所有使用统一入口的Workshop上传均不就绪. 本轮不给绕过命令, 不建议常规Force, 不使用SkipRefresh.
- 各仓已有dirty源码,路径清理,advice删除,research子模块与并行Forms报告保留. 只精确提交本轮路径/文档增量, 禁止git add -A或reset.

## 未知

- 新Forms切分源码的最新实机覆盖由另一个开发会话取得, 本文件不代它宣称通过. 旧r15仅覆盖此前三姿态首回合及九组合可选桥启动, 不覆盖当前新源码.
- 可见UI,长战斗,战中读档,重连,多人,性能和平衡均未在本轮新增验证. 六项目来源一致不等于新增游戏验收, GuardsOnly提前拒绝不等于后续门禁通过.
- 290项保护清单长度/UTC ticks与登录marker无变化, 不等于全Steam安装逐字节审计. 本轮无Workshop发布结果.

## 原始证据入口

G:/omp works/.tmp/workshop-prep-20261004-central

优先读取r4-real-final-verification-results.json,r4-central-test-runs.json,r4-final-seven-payloads.json,r4-source-and-staging-current.json,protected-paths-after-r4-final.json,r4-no-upload-and-spire-staging-unchanged.json. 验证器错误断言在r4-full-refusal-staging-no-writes.json标AssertionValid=false, 不当验收成功. 契约G:/omp works/Sts/sts2-spire1/docs/WORKSHOP-PREPARATION-CONTRACT-20261005.md.