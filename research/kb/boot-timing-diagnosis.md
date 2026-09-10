# Boot-phase timing: how to measure and what NOT to do

User question that spawned this (2026-09-11): 启动游戏有一段显著的黑屏静止期/间歇性未响应,数秒到数十秒.其他人装了我们的 mod 后也出现.

## 方法论教训(三次失败一次成功)

1. **外部日志观察器轮换检测要只用"size 缩小"判定**.第一版 boot-timeline.cjs 用
   `mtime < curMtime` 判定日志轮换,结果每 100ms 的 tick 都误触发(轮换后新文件
   mtime 持续小于旧文件 mtime,直到追上),30 秒里打了 300 行假"BOOT DETECTED",
   产出全部作废.正确判定: `st.size < curSize - 4096`(size 回退)或 inode 变化;
   mtime 单独不可靠.
2. **mod 代码里 Console.WriteLine 在 Steam 启动的游戏里是黑洞**(stdout 无依附).
   诊断输出必须走 `MegaCrit.Sts2.Core.Logging.Log.Info` - 与其他 mod 的
   [INFO] 行同通道,进 godot.log.
3. **mod 的诊断打点受加载顺序限制**: TryLoadMod 的 Harmony patch 在自己的
   initializer 里才挂上,只能覆盖**晚于自己加载**的 mod.BootTimer 拓扑序排第
   33/39,头部 13 秒(真正的卡死段)完全不可见.修复:让用户在游戏 mod 设置里把
   BootTimer 拖到列表最前(无依赖 mod 可任意排),patch 即覆盖全部 39 个.

## 实测数据(2026-09-11 04:14 boot, 39 mods, engine 19.7s to menu)

| UTC 时刻(本地) | 阶段 | 段耗时 |
|---|---|---|
| 04:14:44.0 | 进程启动(日志轮换文件名) | - |
| 04:14:45 | OS info dump(窗口初始化) | ~1s |
| 04:14:45-57.4 | **头部: 引擎init+工坊扫描+前33个mod装载** | **~13s(卡死段)** |
| 04:14:57.4-57.6 | 后 6 个 mod 装载(BootTimer 视角) | 0.2s |
| 04:14:57.6-15:00.2 | LocManager.Initialize(112 张表合并) | 2.6s |
| 04:15:00.2-15:04.2 | 预加载+菜单构建 | 3.9s |
| 04:15:04.2 | NMainMenu._Ready = 主菜单可见 | 总计 20.2s |

关键结论: mod 装载本身(可见部分)是毫秒级;**大头在头部 13 秒**,需 BootTimer
置顶后一次启动即可拆解.

## 已排除的假说(证据)

- **网络/代理**: 用户关 verge-mihomo 代理启动,无差别.网络快照(106 连接)无异常.
- **RitsuLib 工坊自动更新协调器**: 确实在每个 boot 触发(Watcher 3747526116 的
  Steam 安装时间戳卡死,LocalTimestamp 1789010124 < RemoteUpdated 1789010227,
  本地文件实际已是新版),但用户退订 Watcher 后(当次 boot NeedsUpdate=0)黑屏
  照旧.是共犯不是主因.
- **游戏本体更新**: appmanifest StateFlags=4, BytesToDownload=0.
- **重资产 mod 的装载耗时**(RegentFemPortraits 46MB/AFTP 106MB): 可见装载段
  毫秒级,头部才是大头(待置顶确认头部内部结构).

## 诊断工具

- BootTimer mod: G:/omp works/sts2-boottimer/(TryLoadMod prefix/postfix +
  LocManager.Initialize postfix + NMainMenu._Ready postfix, 全部 Log.Info).
  诊断完删除 mods/BootTimer/.


## 最终定位 (2026-09-11 04:24 boot, BootTimer 置顶后全量打点)

mod_list 顺序可直接编辑 settings.save (mod_settings.mod_list 数组顺序 = 手动排序,
GUI 里无显眼入口). BootTimer 置首后覆盖全部 39 个 mod.

每 mod 装载耗时 Top3(占头部 ~13s 中的 8.3s):
- **RegentFX(万象辉星) 4.10s** - 初始化里同步预加载 32 个资源(58MB mod)
- **RitsuLib 2.90s** - 自审: patchAll 1.6s + settingsStore 0.8s
- **BaseLib 1.31s** - 280 个 Harmony patch

其余 36 个 mod 合计 <2s(我方全部 mod: Spire1 110ms / AutoAnthony 545ms /
Perfect 16ms / MpConfigSync 6ms / HeartShake 9ms / AutoAnthonyRelics 20ms).

后续固定成本(与 mod 无关): LocManager 合并 112 表 2.8s + 预加载/菜单 3.7s.

结论: '间歇性未响应' = 每个重型 initializer 阻塞主线程的脉冲叠加.
跨机器复现的合理机制: 通用成本(loc 合并/预加载/重型前置)随 mod 总数增长,
加装任何 mod 都可能把邻居机器推过感知阈值; 我方 mod 本身均为毫秒级.
