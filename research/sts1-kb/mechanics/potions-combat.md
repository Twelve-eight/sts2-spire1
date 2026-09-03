# 战斗中药水管线（Potions in Combat）— StS1 战斗语义知识库

## 本卷范围
药水从点击到生效的完整路径与关键仲裁：**药水 use() 是 UI 点击帧同步直调、不经过动作队列**（与卡牌 use 的队列化根本差异）、PotionPopUp 的结算序、canUse 门、SmokeBomb 的同步标志、妖精药水自动使用交叉引用、药水槽销毁与 onUsePotion 通知。
依赖引用：妖精拦截链 → `death-arbitration.md` R02/R03；回合尾药水相关钩子不在本卷（战斗外使用=无动作队列环境，仅登记使用）。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 使用路径（PotionPopUp 主链）

**R01 点击确认后的固定序** — 出处 `PotionPopUp`（use 分支 offset 211-318，瞄准型分支）。置信度：**高**
```
① 登记 metricData.potions_floor_usage += floorNum（遥测）
② potion.use(hoveredMonster / player)      ← 同步直调，点击帧立即生效
③ 若 room.phase == COMBAT：addToBottom(HandCheckAction)   ← 队列里唯一的常规动作
④ 全 relic 逐个 onUsePotion()
⑤ TopPanel.destroyPotion(slot)（槽位立即腾空）
⑥ 退出瞄准态 / 显示光标
```
非瞄准型药水走同构分支（offset 728 起的 `potion.use` 调用，无 hoveredMonster）。
**仲裁核心**：药水效果本体在**点击的那一帧**完成——`use()` 内部 `addToBot/addToTop` 的动作照常入队（排在当前动作流之后），但**非队列化的状态置位（标志、能量直改等）立即生效**。⇒ 敌方回合动画期间若 UI 允许点药，其同步部分照样生效（开放问题 1）。

**R02 canUse() 是多闸门** — 出处 `PotionPopUp` 多处 `canUse()` 调用（offset 435/498/702/713/978/986：悬停、确认、可点态刷新各查一次）。置信度：**高**
`AbstractPotion#canUse` 基类默认 true；FairyPotion 恒 false（death-arbitration.md R03，只能自动喝）。⇒ "能否喝"是药水类自身语义 + UI 状态机，无统一门禁函数。

---

## 2. 同步置位型药水：SmokeBomb

**R03 SmokeBomb.use 全同步部分** — 出处 `SmokeBomb#use` offset 0-71+。置信度：**高**
```
if (room.phase == COMBAT):
    room.smoked = true                      ← 点击帧立即置位（不经队列）
    addToBot(VFXAction(SmokeBombEffect))    ← 视觉入队
    player.hideHealthBar()
    player.isEscaping = true                ← 立即置位
    （尾部：跳过敌方回合相关收尾，未逐字节展开，见开放问题 2）
```
`room.smoked` / `player.isEscaping` 的消费方在敌方回合调度（`turn-phase.md` R12 的怪物排队与 `MonsterGroup` 死亡/逃跑态判定）——烟雾弹的"跳过敌方回合"= 玩家侧逃跑态 + 房间 smoked 标记的组合，而非一个新的动作类型。

**R04 EntropicBrew 等纯效果药水** — 出处 `EntropicBrew#use`（存在性确认；逐瓶生成随机药水槽位的逻辑未逐字节展开）。置信度：**低**（本体细节）
典型的"全 addToBot"型药水：use() 只把动作入队，效果在动作流排到时结算——与 R03 的同步型相对。移植时必须逐药水分类：**同步置位型 vs 队列效果型**。

---

## 3. 与其他系统的交叉

**R05 妖精药水：唯一从 damage() 内部直调 use() 的路径** — 出处 `AbstractPlayer#damage` 拦截链（death-arbitration.md R02②）+ `FairyPotion#use`。置信度：**高**
自动使用同样**同步直调**（flash → currentHealth=0 → use() → heal 30% → destroyPotion），不经过 PotionPopUp，因此不触发 relics.onUsePotion（R01④ 只在点击链里）。⇒ 依赖 onUsePotion 的遗物（Toy Ornithopter 类：引用扫描中唯一 vanilla 实现者）**不吃妖精自动使用**。

**R06 药水槽销毁即腾位** — 出处 `TopPanel#destroyPotion(slot)`（R01⑤ 与 R05）。置信度：**高**
无"使用中锁定"状态；同一槽位在同帧内理论可再拾取新药水（UI 层竞态，未取证）。

---

## 4. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 动作流运行中喝 Blood Potion | use() 同步、加血动作入队排在当前动作之后（heal 经队列） | R01 |
| 敌方回合点击烟雾弹（UI 允许时） | smoked/isEscaping 立即真；跳回合逻辑按逃跑态消费 | R03 |
| 妖精药水自动救场 | 不触发 onUsePotion 遗物钩子 | R05 |
| 玩家在 COMBAT 喝药后立刻结束回合 | HandCheckAction 在队列中，弃牌阶段前的动作序不受影响 | R01 |
| Sozu（无药水）与药水相关成就 | 掉落层问题，不在战斗管线 | — |

---

## 5. 开放问题 / 低置信项

1. **敌方回合 / actions 运行中能否打开药水 UI**：PotionPopUp 的时序门（isPlayerTurn/hasControl 检查）未逐字节取证；静态看使用链无队列互斥。置信度：**中**。
2. SmokeBomb.use 尾部（skipMonsterTurn/escape 收尾）与 `EscapeAction` 的关系未展开。置信度：**中**。
3. EntropicBrew/药水转化类（Entropic 逐槽生成）本体未取证。置信度：**低**。
4. onUsePotion 的 vanilla 实现者仅 Toy Ornithopter（常量池扫描），Test1 为测试残留。置信度：**高**（名单）/ 名单完整性依赖全 jar 扫描。
