# Astra advice - Spire1

日期: 2026-09-12. 主会话单线. 本轮审查当前互操作, 状态/升级, 配置, 发布契约, 并抽查玩法路径. 不是对全部 230 张卡逐张完成实机验证.

当前方向: 三个一代角色的补充内容层, 与 AFTP/Act4Heart/AutoAnthony 互操作. 用户已删除自建地牢和观者角色; 不因旧文档/根目录遗留源码再次恢复它们.

构建隔离副本 exit 0, 61 警告, 0 错误; 既有 semantics-audit exit 0. 这些不覆盖以下行为问题. 证据: `../astra-advice-evidence/2026-09-12/`.

## P1 SP1-1: 清单放行绕过了关闭状态的哈希放行开关

位置: `mod/Spire1Code/Patches/MpIgnoreModDiffPatch.cs:32-65`.

引擎 HandshakeManager.TryReadHandshakeMessage:114-129 顺序:

1. 游戏版本不一致 -> return VersionMismatch.
2. gameplay mod 列表不一致 -> return ModMismatch.
3. 模型 ID 哈希不一致 -> return VersionMismatch.

我们的 postfix 在 ModMismatch 时直接返回 Success. 因为引擎此前已早退, 第 3 步根本没有跑. 所以清单和哈希都不同时, IgnoreMpHashMismatch=false 也挡不住.

REPRO: 精确补丁源码, only logger seam. 本地 hash=111/远端=222, IgnoreMpModDifferences=true, IgnoreMpHashMismatch=false, 输入 ModMismatch, 输出 Success.

建议在清单放行分支重新保留后续安全条件, 用已解析的 local/remote info 检查真实版本和模型布局哈希. 不允许关闭的哈希放行被另一路放行覆盖. 修正注释: ModelIdSerializationCache.Hash 来自模型 category/entry, SavedProperty 名和 epoch 标识, 不是整个玩法 DLL 的密码学哈希; 相同 hash 也不能证明玩法代码/配置一致.

验收组合至少包含: 仅清单差, 仅 hash 差, 两者都差, 真游戏版本差, 缺失 remote info; 哈希开关开/关分别验证. 不再只测两种单独的 mismatch 状态.

## P2 SP1-2: ResetKeywordCache 会删除其他系统附加的本地关键词

位置: `Cards/Spire1Card.cs:21-28`; 调用者 `Cards/LimitBreak.cs:27`, `Cards/Discovery.cs:61`.

CardModel._keywords 不是纯派生缓存, 它就是持久的本地关键词集合: 初始 CanonicalKeywords + 后续 AddKeyword - RemoveKeyword. 引擎克隆也复制这个集合.

REPRO: 在真实 LimitBreak 模型的最小状态上 AddKeyword(Retain), 调用真实 OnUpgrade. Retain 从 true 变 false. 原因是把整个 _keywords 设 null, 重建只读 CanonicalKeywords.

建议升级只移除/增加该升级本来负责的关键词, 例如 RemoveKeyword(Exhaust), 不清空其他来源状态. CardCmd.Upgrade 的完整路径还要验证, 但不能用没有额外关键词的单卡升级通过替代它.

验收: LimitBreak/Discovery 带 Retain/Ethereal/其他本地关键词升级, 仅 Exhaust 按规则变化; 克隆与存读档不变. 永久/本战/本回合关键词分开, 不替外部系统重置.

## P1 SP1-3: SpireHeart 终局剧情仍注册为普通第三幕事件

位置: `Events/SpireHeart.cs:25-29,62-72`; `Events/Spire1Event.cs:54-61`.

SOURCE: SpireHeart Acts=>Act3, Act3=>Hive. CustomEventModel 默认 autoAdd=true. 事件分支最后调用 CreatureCmd.Kill(Owner.Creature), 没有钥匙门也没有其他结束分支.

这是原一代终局心脏剧情, 不是第三幕普通问号房的等价物. 自建地牢已删除后, 它仍可能在官方 Hive 作为随机事件进入; 仅需一路 Continue/Attack/Continue/Sleep 就结束玩家生命. 本轮未实机抽到这个事件, 所以发生频率/生态补丁最终池状态未量化.

建议先追踪实际 GenerateRooms 的最终候选, 明确它是否应继续存在. 按当前三角色补充层方向, 最保守是退出普通事件注册, 保留必要的旧存档载入语义或明确迁移. 不要 "加奖励选项" 把错误事件改成另一种产品; 不要重建已删除的一代地牢来合理化它.

验收: 官方三幕及 AFTP 的普通事件候选均不包含终局剧情; 真实终局仍由 Act4Heart 处理; 旧存档遇到旧事件不被静默误解释.

## P2 SP1-4: 四个设置开关没有消费者

位置: `Config/Spire1Config.cs:13-23,65-67`.

源码扫描结果: EnableSts1Content/Characters/Cards/Relics 以及 CharactersEnabled/CardsEnabled/RelicsEnabled 只出现在该配置文件. 角色选择实际只读 CharacterGate 的 character.txt; 卡/遗物池直接注册.

这是已在历史审查提过但仍存在的承诺落空, 不作为新的复杂模块. 决策应是:

- 真正实现设置承诺, 并明确何时生效, 如何保持已存档模型身份; 或
- 删除无效设置/UI/说明, 不再向用户展示假开关.

不要把同一批死开关通过 MpConfigSync 同步后称为玩法统一. 若实现 runtime gate, 只过滤获得/显示而非把已经在局里的模型删掉.

## P2 SP1-5: 初始化期消费与全局缓存会影响别的模组

位置: `MainFile.cs:33-36`, `Character/SharedCardReuse.cs:167-193,197-220`.

- PureSts1Pools 在 mod initializer 读取, 后期配置 SetValue 无法撤销注入. 属性名叫 runtime toggle 不会改变事实.
- 非 pure 分支 LogPoolCensus 会在 mod initializer 物化 ColorlessCardPool 和自定义池. 引擎/ModHelper 有 lazy/freeze 语义, 诊断读取本身可能提前冻结后加载 mod 的内容. 需要对实际 BaseLib/引擎验证, 本轮不把它当已实机复现.
- AA bridge 的可选依赖经 .tmp/interop-refs 条件编译, 旧副本构建可以绿但运行 DLL 已升级. 把被加载 AA 的 SHA256/接口版本纳入发布证据, 不要只以 public 方法仍能编译作为验收.
- Third-party Watcher 的 "AutoAnthonyWatcher 存在则让位" 只在 Apply 时观察程序集. 如果 Watcher/Addon 后加载, 早期一次检查未必表达最终 mod 集合. 当前历史日志是 Spire1 在 Watcher 之前加载, 本次未证明该顺序下所有兼容分支生效.

建议必要时把内容冻结/互操作决策放在全部模型/全部 mod 已就绪的明确阶段. 不因日志统计提前物化内容.

## 状态文档与范围控制

- manifest 1.1.0 描述三角色, 230 cards/22 relics/6 unique events. DEVELOP 顶部仍有 4 角色, 306 卡, 53 事件, 自建怪物/幕里程碑. 旧 DEVLOG STATUS 也早于删内容. 当前设计摘要必须更新, 历史章节保留而非反向当计划.
- 根目录 `G:/omp works/mod/Spire1Code/Acts` 是另一个遗留目录, 不在本 csproj, 不要编辑错地方.
- Watcher 已删, 但 powers/tokens/stance 工具仍有残留. 是否删要从引用/旧存档/注册可达性判断; 不可 grep Watcher 就全删.
- Art fallback 会隐藏资源缺失. 历史真实日志曾缺 apotheosis/discovery/the_bomb/slimed 图, 本轮不声称当前全量图已补齐. 发布验收要检查实际 PCK 路径, 而非只看 PNG 文件在任意目录存在.

## 值得保留的实现, 避免误报

- Kunai/Shuriken 的未 await warning 是 TaskHelper.RunSafely(DoActivateVisuals); 数值 PowerCmd 已 await. 不要为了清 CS4014 把纯视觉等待串到游戏动作里, 也别把该 warning 直接当数值失同步.
- CardModel 的 CanonicalVars 是每实例 lazy 数据. 不要为了省分配把 DynamicVar 做 static, 会共享可变数值/owner.
- Fission 使用 count 快照, 基础清球保槽, 升级逐球 evoke 的设计已有明确来源. 需要真实球/能量/抽牌路径验证, 不要因 "不够简洁" 换成错误 Focus 加成.
- AutoSlay 的 immortality patch 会改变掉血路径. 它通过不代表 Rupture/失血/反伤/死亡相关语义正确, 这类场景要禁该辅助再验.

## 推荐顺序

1. SP1-1 联机安全条件, 与 MpConfigSync 同时列出完整失败场景.
2. SP1-3 正常事件池不得包含致死终局剧情.
3. SP1-2 升级保留非本次升级负责的状态.
4. SP1-4 开关真语义; SP1-5 冻结时机/互操作依赖.
5. 更新当前 DEVELOP/版本说明, 再按已变更内容做精确牌/奖励/跨模组验收.

## 交付门

真实三角色普通局/混沌局, Pandora 变形, 关键词附加后升级, 第三幕 boss 有钥匙/无钥匙/三幕-only, 普通事件候选, 配置不同的双端与重连. 每项独立记录 seed, mod 集合/顺序/哈希, 是否开启 autoslay immortality, 实际触发路径.

本轮未运行游戏. 证据等级不可升级为 "所有卡均已实测". 新建议提交也不能把别人的产品修改自动推送上线.
