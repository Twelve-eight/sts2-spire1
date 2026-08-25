# 晨间汇总 — 2026-08-25 夜间作业（00:30–10:45）

> 本文件为夜间工作总账。终态覆盖数字与最终部署哈希见文末「Cutoff 追加」小节。

## 一、交付物全景

### 修复类（全部已构建+推送）
| 编号 | 内容 | 提交 |
|---|---|---|
| F1 | GA 池归属（红→蓝）+ 官方原文语义修正（Block 非敏捷；jar+官方 loc 双源仲裁） | af6d1d7 |
| F2 | 联机握手放行 + RitsuLib 失同步弹窗抑制（IgnoreMpModDifferences，默认开） | d0181a0 |
| F3 | 地图页跳过节点救援按钮（本地解锁+原生投票管线） | 41e7acc |
| F4 | LessonLearned 致命谓词取反修正（对照引擎默认+三张官方卡） | 3cfbcf1 |
| F5 | 商店守卫 AutoSlay 门控（恢复正常对局商店语义） | fc9ef16 |
| F6 | 跳过按钮复位+i18n（SPIRE1_UI_SKIP_NODE 双语 ui.json） | fc9ef16 |
| F7 | BaseLib 钉死 3.4.5（浮动版本+manifest 自动写隐患） | 4695124 |
| F8 | 清单版本号源头化（csproj 覆盖行为根治，0.9.1） | cb70f82 |
| F9 | 观者 77 张卡退出卡牌总览（ShouldShowInCardLibrary 门控，模型保持注册保存档兼容） | bd6c539 |
| F10 | 全角色双重"消耗"剥离（48 张，窄规则保句中语义；powers 域零同类） | 1192c01 |
| F11 | 火堆黑屏通用救援：RestSiteLightingRescuePatch（Finalizer 兜底背景+灯光注入，全幕生效） | fbff0a8 |
| F12 | zhs 补表：ancients 自译台词 + card_keywords/static_hover_tips 空镜像；settings_ui 四开关双语补盲 | be0c902/65a858f |

### 知识库
- **一卷·数据** `research/sts1-kb/`：460+ 条目四色+诅咒/状态/衍生/弃用卡、186 遗物、43 药水、54 事件，en+zhs 双语原文，字节码仲裁。
- **二卷·语义** `research/sts1-kb/mechanics/`：119 条带出处规则。用户示例裁决（draw-exhaust §6）：开局抽牌=原子块、triggerWhenDrawn 仅五类牌、消耗链整批抽完后执行——三者不交错。
- **项目 KB** `research/kb/`：engine-facts / debug-protocols / aftp-interop / loc-drift-report（318 条目对账 A/B/C 分级）。skill 已瘦身为纯方法。

### 审计双报告（独立 subagent，零上下文）
- `research/audits/critique-20260825.md`：17 条问题（P1×2 已修，P2×7 大半已修或入队列）。
- `research/audits/devlog-audit-20260825.md`：需求 27/结论 45 全证据核查，**无虚报**，5 处文档滞后 C1-C5 待清。

### 工具与管线
- `.tmp/night/night_drain.ps1`：覆盖 drain 循环器（hub 常驻，cutoff 硬停，逐局归档）。
- `.tmp/night/coverage.js`：双 id 记账（复用通道落原版 id 不再误报缺失）。
- `.tmp/night/loc_drift.js`：loc↔官方原文相似度对账器。
- `.tmp/night/pck_ls.js / pck_x.js`：Godot pck 列举/暴力提取器。

## 二、AFTP 线结论

- 许可证：主仓无 License（fork 私改合法、二进制发布需授权）；MPBalance=MIT。
- fork：Twelve-eight 名下两仓已建+克隆+构建绿；主仓路径移植已推（7416aef）。
- **火堆黑屏机制链锁定**：NRestSiteRoom._Ready L321-324 对 `%RestSiteLighting` 用非 OrNull GetNode；AFTP 三幕自定义 tscn 任一加载失败即黑屏；存档重启跳过入场转场故"重启就好"。上游 issue 英文稿待发 research/audits/aftp-upstream-issue-draft.md。我方救援层已上线，上游不修也不再卡死。
- MPBalance 洗清嫌疑：源码零火堆接触面。

## 三、验证证据索引

- 冒烟：autoslay P1SMOKE4——补丁失败 0；`character archive: 77` 与 KB 紫卡数精确对账。
- GA 实战探针（drain 夜局）：`extra=0 block=1 deck=ok → extra=1 block=2 deck=ok` 成长链活体确认。
- 部署哈希链：每轮同步后 zip 内 dll/pck 与 live md5 全等（本轮 dll `9e0bd0d9` pck `27020df2` 为准，见追加节）。
- 发布路径启动冒烟：无 autoslay 正常进主菜单，0 补丁失败，settings_ui 合并成功。

## 四、遗留与移交

| 项 | 状态 | 需要 |
|---|---|---|
| 跳过按钮真人局验证 | 待用户 | 开一局进任意房→开图→按钮→点下一节点 |
| 火堆黑屏实机复验 | 待复现 | 新协议：先拷 logs 再杀进程；我方救援层应已消除症状 |
| AFTP 上游 issue 发送 | 待用户 | 文稿已备，审阅后以本人账号提交 |
| Girya 死遗物 / Nloth 空壳事件 | 设计决策 | 用户裁定方向后实现 |
| 覆盖尾巴 | 管线自跑 | WATCHER 归档不再增长属预期；其余池 cutoff 终态见追加节 |
| CodeOpt 流 | 未动 | 建议日间专项 |

## 五、Cutoff 追加

（10:45 循环结束后填写：最终局数、终态覆盖、live/zip 终哈希。）


## 五、Cutoff 追加（22:15 实测，187 局）

- **总归档：187 局**（+1 局含 pure 模式验证局）
- **终态覆盖**：IRONCLAD 47/48（仅缺 ThunderClap，RNG 观察项）、SILENT 50/50 ✅、DEFECT 63/63 ✅、WATCHER 41/77（归档冻结预期）
- **终态基线**：live=三 zip=dll `aa8b4f33` / pck `65006e85` / json 0.9.1

## 六、晚间追加修复（21:00-22:15，提交 3a0de3d 起）

| 项 | 内容 | 状态 |
|---|---|---|
| pure 稀有度带宽 | PureSts1Pools 全稀有度注入自研实现（原仅 Common→U/R 候选空→DingyRug 全无色） | ✅ 已修待纯角色局实证 |
| Armaments 升级 | Block 5→8 补数值（原升级仅 _all） | ✅ 已修 |
| 双虚无 | Carnage/GhostlyArmor/Dazed 剥离独立行"虚无"（引擎自动渲染 Ethereal） | ✅ 已修+全表零残余校验 |
| 药水双份 | 官方同名药水（含 Fire 20 伤）经 ModHelper 追加式复用，自研类保留——机制澄清：Concat 纯追加非替换 | ✅ 机制反证完成 |
| 无色奖励"全无色" | 根因=DingyRug union 官方无色池 + pure 模式 U/R 带宽=0；Concat 反证我方未清空官方池 | ✅ 根因定案+修复 |
| **Rewind 兼容** | Mono.Cecil 补丁 5 参→6 参 attribute（引擎 Add 增 isChangingOwners）；启动异常 0 | ✅ 验证通过 |
| ⚠️ Rewind pck | 部署时误删原 json/pck；json 已重建，pck 需用户重装恢复 | 部分 |

