## 已确认
- 首条证据（2026-10-02）：请求文件 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\native-effect-smoke-implementation-request-20261002.md:4-16` 明确唯一产品代码写入路径为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`，唯一报告写入路径为本文件，并禁止构建、测试、部署。
- 实现边界：本轮只允许修改上述 `FormNativeSmokeRunner.cs` 与本报告；不修改其它产品代码、项目文件、文档、构建脚本、游戏文件、Steam 安装、共享 `mod_configs` 或 C 盘。

## 进行中
- 正在读取工作区入口、项目约定、设计契约、现有 runner、Watcher 卡片与六个形态实现，以确定现有线程、RunManager、卡牌注入、动作状态、清理和 JSON 写入模式。

## 未知
- 尚未确认现有三场景的具体注入点、可读取的运行时证据字段及对应源码行号。

## 已确认
- 文本检查证据（2026-10-02）：项目检查器 `G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs` 已检查本轮不派发的英文模型请求占位文本，输出 `agent text accepted`；本轮不启动任何子代理，未发送其它模型请求。
- 设计契约 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:202-229`（按当前文件行号）要求真实 Watcher 入口、真实主线程和真实运行证据边界；其 `2026-10-02 P0` 段明确此前只证明三张姿态入口卡的 P0 闭环，不能替代本轮效果卡证据。
- 当前 runner `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:472-521` 已在主线程调用真实 `RunManager`、真实 `StartNewSingleplayerRun`、真实 `FormStanceModifier`、真实 `EnterRoomDebug`；`549-590` 已使用真实 `CardPileCmd.Add` 和 `PlayCardAction`。
- 当前 runner `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:109-118,1089-1100` 现有三场景只把 `WATCHER_VIGILANCE`、`WATCHER_ERUPTION_P`、`WATCHER_BLASPHEMY` 作为单张场景卡；`542-547` 与 `681-687` 只有单一 entry card 的前后快照及形态 gate。
- 请求中给出的 `G:\omp works\.tmp\watchermod\WatcherMod\WatcherStrike_P.cs` 不存在；实际可读的反编译来源为 `G:\omp works\.tmp\watcher-decomp-20260928\WatcherMod\WatcherStrike_P.cs`，已定位到真实卡片文件。请求中给出的 `CelestialFormPower.cs` 实际存在，路径校正为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs`。

## 进行中
- 正在核对 `WatcherStrike_P` 的真实费用/伤害/目标契约，以及 runner 可用的动作、卡牌、能量、生命值和形态 Power 读取方式。
- 正在设计仅在同一场景中先完成 entry action、再注入并完成 effect action 的证据结构；不改动现有 form gate、超时、清理、detached operation 和 JSON 写入路径。

## 未知
- 目标卡完成后，当前运行时可直接读取的 Doom、Strength、Echo 重放计数和 carrier 生命周期字段尚未确认。
- 现有 `Snapshot` 对目标仅记录 HP、死亡状态和 Power 类型；尚未确认是否足以区分 entry action 与 effect action 的必需效果证据。

## 已确认
- 截止 2026-10-02，本轮实现未完成，产品代码 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 未修改（git status 未显示产品文件变更，且本轮没有执行任何产品文件写入命令）。
- 已确认的源码证据：实际 Watcher 卡片来源为 G:\omp works\.tmp\watcher-decomp-20260928\WatcherMod\WatcherStrike_P.cs:13-29；卡片类型为 WatcherMod.WatcherStrike_P，构造函数基础费用为 1，基础伤害变量为 6，播放路径调用真实 DamageCmd.Attack。
- 当前 runner 的真实运行链路仍位于 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:472-521（真实 RunManager、StartNewSingleplayerRun、FormStanceModifier、EnterRoomDebug），以及 :549-590（真实 CardPileCmd.Add 与 PlayCardAction）。
- 当前 runner 仍只执行一张现有姿态入口卡：G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1089-1100 选择 WATCHER_VIGILANCE、WATCHER_ERUPTION_P、WATCHER_BLASPHEMY；542-547 和 681-687 仍只有单一 action 的前后快照，尚未加入 WatcherStrike_P effect action。
- 当前已核对的效果读取边界：Snapshot 仍在 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1203-1248，只记录能量、手牌数、回合、目标 HP/死亡状态和目标 Power 类型；尚未加入 Strength/Doom 数值、Echo 重放边界或分离的 entry/effect action 证据。
- 当前已核对的效果源码证据：VoidFormEffectPower.cs:140-170 以真实 CardPlay 身份消费免费额度；SerpentFormPower.cs:87-125 在完成卡牌后造成 3 点无源伤害；DemonFormPower.cs:255-263 读取敌方来源伤害加成；ReaperFormEffectPower.cs:24-36 以 powered attack 和实际 TotalDamage 施加 Doom；EchoFormEffectPower.cs:40-65 通过 ModifyCardPlayCount 增加一次播放；CelestialFormPower.cs:40-54 真实授予能量并抽牌。

## 进行中
- 已停止继续研究、设计和代码修改；主会话应收回实现。
- 本轮未执行构建、测试、部署，也未触碰 Steam 安装、共享 mod_configs、游戏文件或 C 盘。

## 未知
- 三场景中 entry card 与真实 WatcherStrike_P effect card 的分离 action 状态、前后能量、目标 HP、Strength、Doom、Echo 重放和形态生命周期证据尚未实现，因此不能声称目标已满足。
- 未生成 CODE_COMPLETE；本报告状态为 CODE_INCOMPLETE_PRODUCT_UNCHANGED。
