using System.Reflection;
using BaseLib.Config;
using EventRemovalProbe;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Events;

// Both production files are source-linked. Engine and BaseLib dependencies are isolated doubles.
// This exercises TakeAndGive and static config properties, not UI or config-file persistence.
var cases = new (string Name, Func<Task> Run)[]
{
    ("默认关闭和显式设置", CheckConfig),
    ("永恒不可选不可删且普通卡可移除", MixedDeck),
    ("普通卡正常交换", OrdinaryDeck),
    ("初始仅永恒卡仍可收牌后交换", EternalOnlyDeck),
    ("初始空牌堆仍可收牌后交换", EmptyDeck),
    ("选择完成后不可移除的卡不传入删除命令", ChangedRemovability),
};
int failures = 0;
foreach (var test in cases)
{
    ProbeState.Reset();
    try
    {
        await test.Run();
        Console.WriteLine($"通过: {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"失败: {test.Name}: {ex.GetBaseException().Message}");
    }
}
Console.WriteLine($"探针场景: {cases.Length}, 失败: {failures}");
Console.WriteLine("证据边界: 链接真实事件和配置源码, 选择器与牌堆为隔离契约桩. 不覆盖游戏 UI, BaseLib 配置持久化或实机行为.");
Environment.ExitCode = failures == 0 ? 0 : 1;

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static Task CheckConfig()
{
    bool initialMaster = Spire1Config.EnableSts1Content;
    bool initialEvents = Spire1Config.EnableSts1Events;
    try
    {
        Require(initialMaster, "不得更改内容总开关的初始值");
        Require(!initialEvents, "事件注入必须默认关闭");
        Require(!Spire1Config.EventsEnabled, "默认组合开关必须关闭");
        var helper = typeof(Spire1Config).GetProperty(nameof(Spire1Config.EventsEnabled))!;
        Require(helper.IsDefined(typeof(ConfigIgnoreAttribute), inherit: true), "计算属性必须保留 ConfigIgnore");
        Require(helper.SetMethod is null, "计算属性不得变为独立配置项");

        // Assigning explicit values models the setter boundary, not BaseLib deserialization.
        Spire1Config.EnableSts1Events = true;
        Require(Spire1Config.EventsEnabled, "总开关开启时显式事件开关必须生效");
        Spire1Config.EnableSts1Content = false;
        Require(!Spire1Config.EventsEnabled, "总开关关闭必须阻断事件");
        Require(Spire1Config.EnableSts1Events, "总开关不得重置用户显式事件设置");
        Spire1Config.EnableSts1Content = true;
        Require(Spire1Config.EventsEnabled, "总开关恢复后显式事件设置必须保留");
        Spire1Config.EnableSts1Events = false;
        Require(!Spire1Config.EventsEnabled, "显式关闭必须生效");
        Require(Spire1Config.EnableSts1Content, "事件开关不得反向修改总开关");
    }
    finally
    {
        Spire1Config.EnableSts1Content = initialMaster;
        Spire1Config.EnableSts1Events = initialEvents;
    }
    return Task.CompletedTask;
}

static CardModel AddCard(NoteForYourself item, string name, bool eternal = false)
{
    var card = new CardModel { Name = name, Owner = item.Owner };
    if (eternal)
        card.Keywords.Add(CardKeyword.Eternal);
    item.Owner.Deck.Add(card);
    return card;
}

static async Task Exchange(NoteForYourself item)
{
    var method = typeof(NoteForYourself).GetMethod("TakeAndGive", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(nameof(NoteForYourself), "TakeAndGive");
    await (Task)method.Invoke(item, null)!;
    Require(CardSelectCmd.RemovalCalls == 1, "必须使用专用移除选择器且只调用一次");
    Require(ProbeState.Trace.SequenceEqual(new[] { "add", "select", "remove", "finish" }),
        "必须等待收牌完成后选牌, 等待移除完成后结束事件");
    Require(ProbeState.Added.Count == 1 && ProbeState.Added[0] is IronWave, "原有收牌奖励必须保持不变");
    Require(ProbeState.FinishedPage == "DONE", "事件必须正常结束");
}

static async Task MixedDeck()
{
    var item = new NoteForYourself();
    var eternal = AddCard(item, "eternal", eternal: true);
    var ordinary = AddCard(item, "ordinary");
    await Exchange(item);
    var received = ProbeState.Added.Single();
    Require(CardSelectCmd.LastCandidates.SequenceEqual(new[] { ordinary, received }), "只能提供普通卡及先获得的奖励卡");
    Require(!CardSelectCmd.LastCandidates.Contains(eternal), "永恒卡不得出现在候选列表");
    Require(item.Owner.Deck.Contains(eternal), "永恒卡必须保留");
    Require(!item.Owner.Deck.Contains(ordinary), "普通卡必须可移除");
    Require(ProbeState.Removed.SequenceEqual(new[] { ordinary }), "只移除被选普通卡");
    Require(item.Owner.Deck.SequenceEqual(new[] { eternal, received }), "交换不得修改无关卡牌");
}

static async Task OrdinaryDeck()
{
    var item = new NoteForYourself();
    var ordinary = AddCard(item, "ordinary");
    await Exchange(item);
    var received = ProbeState.Added.Single();
    Require(CardSelectCmd.LastCandidates.SequenceEqual(new[] { ordinary, received }), "普通交换应保留原选择顺序");
    Require(ProbeState.Removed.SequenceEqual(new[] { ordinary }), "普通卡必须成功移除");
    Require(item.Owner.Deck.SequenceEqual(new[] { received }), "普通交换只保留奖励卡");
}

static async Task EternalOnlyDeck()
{
    var item = new NoteForYourself();
    var eternal = AddCard(item, "eternal", eternal: true);
    Require(!item.Owner.Deck.Any(card => card.IsRemovable), "夹具初始不能有可移除卡");
    await Exchange(item);
    var received = ProbeState.Added.Single();
    Require(CardSelectCmd.LastCandidates.SequenceEqual(new[] { received }), "收牌后只能选新获得的奖励卡");
    Require(ProbeState.Removed.SequenceEqual(new[] { received }), "只能移除新获得的奖励卡");
    Require(item.Owner.Deck.SequenceEqual(new[] { eternal }), "不得以初始无可移除卡为由删除永恒卡");
}

static async Task EmptyDeck()
{
    var item = new NoteForYourself();
    await Exchange(item);
    var received = ProbeState.Added.Single();
    Require(CardSelectCmd.LastCandidates.SequenceEqual(new[] { received }), "空牌堆仍须先获得奖励卡再选牌");
    Require(ProbeState.Removed.SequenceEqual(new[] { received }), "空牌堆交换应移除新获得的奖励卡");
    Require(item.Owner.Deck.Count == 0, "空牌堆交换完成后不得额外留下或生成卡牌");
}

static async Task ChangedRemovability()
{
    var item = new NoteForYourself();
    var ordinary = AddCard(item, "ordinary");
    // Synthetic state change while the async selector completes; not an observed engine race.
    ProbeState.AfterSelection = card => card.Keywords.Add(CardKeyword.Eternal);
    await Exchange(item);
    Require(ReferenceEquals(CardSelectCmd.LastSelected, ordinary), "应在选牌完成前选中原普通卡");
    Require(!ordinary.IsRemovable, "夹具应模拟选择完成后的不可移除状态");
    Require(ProbeState.Removed.Count == 0, "删除命令不得接收已变为不可移除的卡");
    Require(item.Owner.Deck.SequenceEqual(new[] { ordinary, ProbeState.Added.Single() }), "二次过滤后两张卡均须保留");
}
