using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Run;

// 隔离验证 P3: 每局存档级"内容登记"闩锁的语义。链接真实的 Spire1Config 与 Spire1RunContent
// 源码; BaseLib.Config 用隔离替身。不覆盖引擎 SerializableRun 往返/Harmony/实机(那由集中构建 +
// 实机矩阵覆盖), 只锁定闩锁本身的真值表与三个 gate helper 的 AND 组合。
int failures = 0;
void Check(string name, System.Func<bool> f)
{
    bool ok; string err = "";
    try { ok = f(); } catch (System.Exception ex) { ok = false; err = ex.GetBaseException().Message; }
    if (ok) System.Console.WriteLine($"通过: {name}");
    else { failures++; System.Console.Error.WriteLine($"失败: {name} {err}"); }
}

// 基线: 全局内容/卡牌/遗物/事件均开启, 便于隔离出闩锁这一维度。
Spire1Config.EnableSts1Content = true;
Spire1Config.EnableSts1Cards = true;
Spire1Config.EnableSts1Relics = true;
Spire1Config.EnableSts1Events = true;

Check("默认闩锁为开(无对局/旧存档回落 true)", () => Spire1RunContent.ContentActiveThisRun);

Check("新设置项默认开启", () => Spire1Config.RegisterContentNextRun);

Check("新对局锁存 false 时三 gate 全关", () =>
{
    Spire1RunContent.LatchForNewRun(false);
    return !Spire1RunContent.ContentActiveThisRun
        && !Spire1Config.CardsEnabled && !Spire1Config.RelicsEnabled && !Spire1Config.EventsEnabled;
});

Check("新对局锁存 true 时三 gate 恢复(全局开关都开)", () =>
{
    Spire1RunContent.LatchForNewRun(true);
    return Spire1RunContent.ContentActiveThisRun
        && Spire1Config.CardsEnabled && Spire1Config.RelicsEnabled && Spire1Config.EventsEnabled;
});

Check("读档快照 false 覆盖当前 true 闩锁(存档设置优先)", () =>
{
    Spire1RunContent.LatchForNewRun(true);   // 上一局是开
    Spire1RunContent.RestoreFromSave(false); // 载入一个当初关掉的存档
    return !Spire1RunContent.ContentActiveThisRun && !Spire1Config.CardsEnabled;
});

Check("读档快照 true 覆盖当前 false 闩锁", () =>
{
    Spire1RunContent.LatchForNewRun(false);
    Spire1RunContent.RestoreFromSave(true);
    return Spire1RunContent.ContentActiveThisRun && Spire1Config.CardsEnabled;
});

Check("闩锁为开但全局事件关 -> EventsEnabled 仍关(与其它开关正交)", () =>
{
    Spire1RunContent.RestoreFromSave(true);
    Spire1Config.EnableSts1Events = false;
    bool ev = Spire1Config.EventsEnabled;
    Spire1Config.EnableSts1Events = true;
    return !ev && Spire1Config.CardsEnabled;
});

Check("全局总开关关 -> 即便闩锁开也全关(闩锁不越过总开关)", () =>
{
    Spire1RunContent.RestoreFromSave(true);
    Spire1Config.EnableSts1Content = false;
    bool any = Spire1Config.CardsEnabled || Spire1Config.RelicsEnabled || Spire1Config.EventsEnabled;
    Spire1Config.EnableSts1Content = true;
    return !any;
});

System.Console.WriteLine($"闩锁探针场景: 8, 失败: {failures}");
System.Console.WriteLine("证据边界: 链接真实 Spire1Config + Spire1RunContent 源码; BaseLib.Config 为隔离替身。不覆盖 SerializableRun 往返/Harmony 落点/实机。");
System.Environment.ExitCode = failures == 0 ? 0 : 1;
