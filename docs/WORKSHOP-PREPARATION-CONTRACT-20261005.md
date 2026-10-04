# Workshop 推送前准备契约 - 2026-10-05

## 范围和边界

本轮是用户要求的推送前准备,不是 Workshop 发布授权. 不调用 SteamCMD,不读取账号密码,不消耗登录冷却,不上传任何项目. 不写 Steam 游戏安装,不改共享 mod_configs,不启动/停止用户正在运行的游戏,不写 C: 缓存. 全量 gate 仍检查 7 个维护中的项目;停止维护的新版 autoanthonyrelics 不在范围内.

## 已复现问题

2026-10-04 中央只读 VerifyOnly exit=1: Spire1 PCK_STALE, Perfect/MpConfigSync/HeartShake/QuriousCraftingRelics PCK_DIGEST_MISSING. 证据位于 G:\omp works\.tmp\workshop-prep-20261004-central\baseline.json 和 baseline-full-verify.log. Spire1 r15 Workshop DLL/PCK 已有相同字节绑定的隔离真实运行证据,本轮不冒充新运行验收.

## 决策

1. 保持通用 provenance 的唯一来源为 Release build 目录,不增加从 Workshop staging 或未知路径反向取源的 fallback. 不取消 digest,mtime,源代码新鲜度,路径,held-back 和逐字节检查. 不改 -Only 的全量上传预检语义.
2. Spire1 的 Build-Spire1Release.ps1 在显式 -Promote 且 Configuration=Release 时,先完成 PCK 包内门禁和 DLL 三项门禁,再把本次精简 PCK 与 producer digest同步到 canonical Release build 目录,然后才更新 Workshop payload. 默认不额外同步 canonical PCK. SkipBuild 可复用现有 DLL,但必须验证 payload DLL与 canonical DLL仍是同一字节;不能使用 bin/publish fallback DLL替代 canonical DLL.
3. canonical 更新前必须验证所有目标路径和祖先链无 reparse,目标属于本仓 build 输出,不是 Steam/共享配置/C:. 新 PCK 写临时文件,回读验证,安装 PCK后安装 digest,最终验证长度/hash/mtime. 两文件更新不是事务原子,失败必须中止且不进行 Workshop promote;现有 provenance gate会拒绝半成品,不得通过改时间或手工补 hash伪装成功.
4. 通用 refresh 对 Spire1 明确不把 PDB列入允许发布文件;其它项目保持原有 optional PDB策略. Spire1 staging含 PDB/deps/digest/其它文件时 fail-closed,不会静默删除未知文件或把调试产物重新带入发布. 正式 Spire1 payload恰为 DLL/JSON/PCK三文件.
5. 另外四个项目的 quick PCK producer须在实际成功 PackPck 后生成 build目录 .pck.sha256;不能依赖 CopyToModsFolderOnBuild=true. 生产步骤读本次 PCK实际字节,断言存在/非空/mtime不早于本次 pack与 DLL,写 ASCII 64hex并回读核对. 失败/跳过/禁用 pack时不得重标旧 PCK或旧 digest为本次产物. 不调用 Godot发布,不改部署策略,不新增依赖包.
6. 实现子代理不运行 build/lint/tests;主会话集中执行. 指定唯一模型 global:deepseek-v4.1-flash,reasoning max,请求渠道 gateway/wb2api,同时运行不超过12个,按用户2026-10-05最新调整,替代本契约起草时的两个. 每个实现者同批绑定监督者,监督者实际 wait门禁通过后才审最终产物. 实际路由以安全会话元数据为证,未知细路由明确标未知.

## 中央验证与验收

- 改前/改后脚本 parse,XML parse,差异审阅和监督报告.
- Spire1 用唯一全新 G:临时目录执行 -SkipBuild -Promote,重打精简 PCK并同步 canonical. 验证最终 DLL/PCK/manifest hashes仍与 r15已测字节相同;不同则旧实机证据失去覆盖,需要重新验收.
- 四个项目依次 Release Rebuild,显式关闭 CopyToModsFolderOnBuild,固定 Sts2Path到非 Steam测试副本,所有 .NET/cache/TEMP都在 G:;只产出到仓内 build目录,不部署.
- 逐项核对 producer digest等于 PCK SHA256,PCK非空,mtime晚于 DLL,staging只来自 build目录与 source manifest.
- 在隔离夹具验证 Spire1 不带 PDB会通过,带 PDB会失败;digest缺失/错误/过期仍失败;同 hash不同 mtime的 PCK须 refresh后严格一致.
- 运行真实全量 refresh,再 VerifyOnly,必须 7/7 VERIFY RESULT: OK.
- 用真正 guarded入口的 -GuardsOnly验证 VDF配对,held-back字节,文字/大小/description漂移/格式门禁,无 Steam contact.
- 只按本轮修改清单 commit + push GitHub备份. 保留既有 advice删除,dirty research子模块与历史报告,不 git add -A. 根 .tooling不是 Git仓,变更脚本的精确快照需在 Spire1工具证据目录备份并验证与部署脚本hash一致.

## 不外推的结论

payload门禁通过不等于已发布. r15同字节真实运行只覆盖隔离 headless三形态首回合和可选桥启动矩阵,不覆盖可见 UI,完整资产消费者,运行历史/旧存档,长战斗,多人,重连,性能和平衡. 另外四个 mod本轮构建/刷新不等于新增实机游戏验收. 未关闭边界必须写入DEVLOG和最终交接.

## 全量复验新增发现: 文档JSON与生产输入区分

首个真实全量VerifyOnly已揭示: freshness扫描把仓库根docs下审查门禁与源码快照JSON当成生产输入, 造成Spire1 REBUILD_REQUIRED, 即使源码/DLL/PCK仍与r15绑定. 不通过重建后再写报告的循环,改mtime,移走报告或只检查Spire1来规避.

最小修复仅排除相对仓库根docs下经核对不参与生产构建的JSON. 保留该树的cs/csproj/props扫描, 因ChaosBridge等项目csproj位于根, 文档目录中的代码可能是默认Compile输入. 保留mod/.../docs下runtime JSON, 以及真实源manifest,localization和其它生产JSON的freshness检查. 不将任何路径段docs一概排除, 不改变其它ignored dirs/held-back/字节/摘要/mtime/全量门禁策略. 实现者应静态核对七项目csproj/资源和AdditionalFiles配置, 若发现根docs JSON实际是生产依赖, 必须报告并收紧匹配而非强行排除.

中央验证应先用旧脚本在隔离七行夹具复现两类根docs JSON误报, 再确认修复后不误报且七种真实/保守输入仍REBUILD_REQUIRED. PS7和PS5.1都跑, 之后重跑既有11项负例及真实全量refresh/VerifyOnly/GuardsOnly. 此变更需同批DeepSeek实现和独立监督, 主会话实际wait completed后激活, 不跳过门禁.


追加已复现漏查: 原ignored dirs检查绝对FullName, 合法.tmp隔离Root的所有输入都被外层.tmp祖先排除. 首次source-freshness-before-ps7矩阵的docs正例不能作误报复现, 但生产cs/csproj/props/json晚于DLL仍Exit0是漏查证据. 本次还需把原excludes应用在相对repo路径, 保留repo内根级/嵌套excluded dirs. 文档误报仍以真实Sts全量只读证据为准. 后续13项矩阵同时验证两个缺陷, 不为测试修改生产时间戳.

## 当前跨会话发布阻塞与refresh写入边界

中央2026-10-05 UTC+8接续检查确认, Forms独立化会话已修改Spire1生产源码,项目文件和本地化; canonical/Workshop仍是旧r15字节. 修复后的freshness正确报告mod/Spire1/localization/zhs/modifiers.json晚于DLL, 不再是根docs JSON误报. 不覆盖并行源码,不调整mtime,不用旧r15运行证据覆盖新Forms切分.

真实全量refresh已更新其它六项目staging, 最终exit1仅剩Spire1 REBUILD_REQUIRED. freshness属于复制后的最终核验, 整个refresh不具备无写入事务性. 中央验证器曾错误假定任意exit1均无写入, 该断言已明确标invalid; 实际22个staging文件更新有备份和逐字节证据, 不恢复旧包来假装未写. 六项目单项VerifyOnly exit0, 正式Spire1 wrapper GuardsOnly在全量provenance处exit12, 未继续后续门禁或Steam contact.

接续顺序: Forms会话完成独立监督和中央构建验收并冻结源码 -> 在独占新输出重建Spire1 Release和精简PCK并绑定新hash的必要运行证据 -> 审核通过后Promote -> 全量refresh/VerifyOnly -> 真正wrapper GuardsOnly. 没有全量通过前, 不能给任何统一入口项目贴可上传标签. 用户需另行授权实际Workshop上传; 本轮不上传.