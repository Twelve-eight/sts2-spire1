# 心脏战心跳机制 (heartbeat) - StS1 尖塔之心

回答"一代心脏战的心跳(屏幕震动+音效)如何驱动,是否与 BGM 节拍同步"类问题.
为 StS2 HeartShake mod (sts2-heartshake) 的还原精度提供基线.

置信度图例同 [README.md](../README.md): 高=javap 字节码直接可证; 中=字节码+调用链推断;
低=间接证据待证. 本卷另含**音频信号分析证据**(工具链: soundfile/libsndfile 解码 +
numpy 频谱/自相关, 无 ffmpeg; 复现命令见文末).

## **R01** 心跳由 spine 动画事件驱动,非游戏逻辑计时器 [高]

`CorruptHeart.<init>` (字节码 51-80): 加载 `images/npcs/heart/skeleton.json`,
`AnimationState.setAnimation(0, "idle", loop=true)`, `TrackEntry.setTimeScale(1.5f)`,
`AnimationState.addListener(new HeartAnimListener())`.

skeleton.json 实测数据(解析自 jar 内原文件):
- 唯一动画 `idle`, bone 时间轴全长 **2.0s** -> 播放周期 2.0/1.5 = **1.3333s**.
- 事件: `maxbeat` @ t=0.3666, `smallbeat` @ t=0.9333, `smallbeat` @ t=1.3.

## **R02** 只有 maxbeat 触发音效+震动; smallbeat 无任何处理 [高]

`HeartAnimListener.event(int, Event)` 全量逻辑(仅 11 条字节码):
```
if (!AbstractDungeon.isScreenUp && "maxbeat".equals(event.getData().getName())) {
    CardCrawlGame.sound.playAV("HEART_SIMPLE", MathUtils.random(-0.05f, 0.05f), 0.75f);
    CardCrawlGame.screenShake.shake(ShakeIntensity.LOW, ShakeDur.SHORT, false);
}
```
两个 smallbeat 事件被忽略 -> **每周期(1.3333s)恰好一次脉冲**.
(注: `event()` 是 spine AnimationStateListener 回调, `complete/start/end` 均为空实现.)

## **R03** 震动与音效参数 [高]

- `ScreenShake.getIntensity`: LOW=20.0f*Settings.scale, MED=50, HIGH=100 (ScreenShake$ShakeIntensity 枚举序 LOW/MED/HIGH, lookupswitch 1->20, 2->50, default->100).
- `ScreenShake.getDuration`: SHORT=0.3f, MED=0.5, LONG=1.0, XLONG=3.0.
- `shake(LOW, SHORT, false)`: 第三参 vertical=false -> 水平震动, intervalSpeed=0.3.
- `playAV("HEART_SIMPLE", pitchMin, volume)`: 音量 0.75, pitch 随机 [-0.05, +0.05].
- `SoundMaster` 加载表: `HEART_SIMPLE -> audio/sound/SLS_SFX_HeartBeat_Simple_v1.ogg` (27.5KB, 1.252s, 双音节 lub-dub: 主脉冲 @0.02s, 次脉冲 @0.67s, 尾部余响至 1.02s).
- 另有 `HEART_BEAT -> SLS_SFX_HeartBeat_Resonant_v1.ogg` 注册,但 CorruptHeart 战斗未使用.

## **R04** 前置条件与生命周期 [高]

- 触发门槛: `AbstractDungeon.isScreenUp == false`(有全屏界面盖住时静默).
- `CorruptHeart.die()`: `AnimationState.removeListener(animListener)` -> 心脏死亡瞬间心跳停止.
- **不存在** update() 重写 / 动态 setTimeScale / 随 HP 变速逻辑(CorruptHeart 方法全集:
  `<init>/usePreBattleAction/takeTurn/getMove/die/static{}`, javap -p 全量核对).
  网传"心跳随玩家 HP 降低而加快"不成立 - 节拍恒定 1.3333s 直到死亡.

## **R05** 心跳与 BGM 无代码层同步 [高]

- 战斗 BGM: `usePreBattleAction` -> `getCurrRoom().playBgmInstantly("BOSS_ENDING")` ->
  `MusicMaster.playTempBgmInstantly` -> `TempMusic.<init>` -> `Gdx.audio.newMusic(...)` + `Music.play()`.
- `TempMusic`/`MainMusic`/`MusicMaster` 全量字节码 **零 `setPosition`/`setPan` 调用**,
  BGM 永远从 0 开始播放, 无节拍相位输出, 无任何"对齐到拍"API.
- 心跳计时(spine TrackEntry timeScale)与 BGM 播放(libGDX Music)是两个互不通信的系统;
  事件战斗界面(SpireHeart event)的过场 AnimatedNpc 也挂同一个 HeartAnimListener(构造器 96-122),
  但进入战斗房间后是 MonsterRoomBoss 重新 new 的 CorruptHeart 实例, 动画计时从战斗加载帧重新起算.
- 结论: **"心跳总落在 BGM 第三拍"在代码层不存在任何实现机制**;两系统起点独立
  (BGM 起点=房间加载帧, 心跳起点=心脏 spine 就绪帧), 相位差逐次漂移.

## **R06** BGM 音频实测: ~157 BPM, 与心跳周期不可通约 [中-音频分析]

`STS_Boss4_v6.ogg` (TempMusic.getSong 映射 `BOSS_ENDING -> audio/music/STS_Boss4_v6.ogg`,
178.3s, 44.1kHz, 无 BPM 元数据标签, 仅 Lavf 编码器标记):

- 谱流(spectral flux) onset 检测 + comb-filter tempo 估计(60-200 BPM, 0.1 步进):
  **top 157.2 BPM**(拍长 0.3817s), 次峰 78.7(半频), 161.8/153.2 陪跑.
- 分段稳定性(30s 窗/15s 步进): 0-165s 内 10 窗中 9 窗 = 157.0-157.5 BPM,
  仅 60-90s 窗检出 104.8(疑似慢乐段或半频误判) -> tempo 基本恒定, 无 rubato 对齐空间.
- 小节结构: 低频带(<150Hz) onset 的 mod-3 / mod-4 相位分布均匀(Counter: strong mod3 = 16/11/11),
  自相关 2/3/4 拍周期无显著峰 -> **未检出可靠的小节(3/4 vs 4/4)强拍模式**
  (管弦乐渐强型配器, 无稳定鼓组, 拍号判定置信度不足).
- 数学关系: 心跳周期 1.3333s / 拍 0.3817s = **3.493 拍** - 非整数, 不可通约.
  即使 BGM 确为三拍子, 心跳也不可能"总落第三拍": 每心跳漂移 0.493 拍
  (约 0.188s), 6-7 拍后即跨过整个节拍相位, 周而复始.
- **用户实机验证 (2026-09-11, 一代原版直接开测)**: "不是对齐每个小节的第三拍,
  也不是 4/4 或 3/4 对应关系; 大多数心跳落在无法精准感知的两拍之间, 少部分
  听起来重合." 与上述不可通约分析完全吻合 - 3.493 拍相位漂移的听觉特征正是
  "多数在拍间游走, 偶发踩拍". 代码证据(R05) + 信号分析(R06) + 实机听感三方
  一致, 结论关闭: **心跳与 BGM 节拍无同步机制, 重合是相位漂移中的偶合**.
  解释 (a) 的"连续多拍几乎不动"与"少部分重合"相符, 保留为主因假设.

## **R07** StS2 还原基线(HeartShake mod 采用值)

| StS1 原版 | HeartShake StS2 实现 | 依据 |
|---|---|---|
| 周期 1.3333s (idle 2.0s / timeScale 1.5) | 同值自建 Node._Process 计时 | R01 |
| maxbeat 一次脉冲/周期 | 每拍 ScreenShake(Weak, Short) + ogg 一次 | R02 |
| LOW(20/100) SHORT(0.3s) 水平 | StS2 Weak(5/80) Short(0.3s) - 两代强度表不同, 取档位对应 | R03 |
| 音量 0.75, pitch ±0.05 | VolumeLinear 0.75, PitchScale 1±0.05 | R03 |
| isScreenUp 门 | 省略(StS2 震动只作用于战斗 SceneContainer, 无 UI 破坏) | R04 |
| 心脏死亡即停 | creature.IsDead -> SetProcess(false) | R04 |
| 与 BGM 无同步 | 不做任何拍对齐(忠实原版解耦行为) | R05/R06 |

## 证据复现入口

```bash
# R01-R05 字节码(只读 jar)
cd "G:/steam/steamapps/common/SlayTheSpire"
unzip -o -q desktop-1.0.jar "com/megacrit/cardcrawl/monsters/ending/CorruptHeart.class" \
  "com/megacrit/cardcrawl/helpers/HeartAnimListener.class" \
  "com/megacrit/cardcrawl/helpers/ScreenShake*.class" \
  "com/megacrit/cardcrawl/audio/TempMusic.class" "com/megacrit/cardcrawl/audio/MainMusic.class" \
  "com/megacrit/cardcrawl/audio/MusicMaster.class" "com/megacrit/cardcrawl/rooms/MonsterRoomBoss.class" -d .tmp/jcls
javap -c -p .tmp/jcls/com/megacrit/cardcrawl/monsters/ending/CorruptHeart.class

# R01 skeleton 数据
unzip -o -q desktop-1.0.jar "images/npcs/heart/skeleton.json" -d .tmp/jcls
python -c "import json; d=json.load(open('.tmp/jcls/images/npcs/heart/skeleton.json')); print(d['animations']['idle']['events'])"

# R06 音频分析(soundfile+numpy 安装在 G:/omp works/.tmp/pylibs, PIP 缓存 G 盘)
PYTHONPATH="G:/omp works/.tmp/pylibs" python <脚本>   # 谱流+comb-filter, 见 sts2-heartshake/DEVLOG.md Session 2
```

音频分析工具链落盘: `G:/omp works/.tmp/pylibs` (numpy 2.5.3 + soundfile 0.14.0,
pip --target 安装, libsndfile 原生解码 ogg, 不写 C 盘).
