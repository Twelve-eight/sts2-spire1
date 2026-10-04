using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using Spire1.Spire1Code.Experimental.MenuPerformance;
using Mode = Godot.Node.ProcessModeEnum;

internal static class Program
{
    private static int _cases, _failures;
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Control Root(bool visible = true) { var n = new Control { Visible = visible }; n.Enter(); return n; }
    private static void Case(string name, Action action)
    {
        _cases++;
        try { action(); Assert(Spire1.Spire1Code.MainFile.Logger.Errors.Count == 0, "controller logged an error"); Console.WriteLine("通过: " + name); }
        catch (Exception e) { _failures++; Console.WriteLine("失败: " + name + ": " + e.Message); }
        finally { Callable.Reset(); OS.Caller = 1; Spire1.Spire1Code.MainFile.Logger.Errors.Clear(); }
    }
    public static int Main()
    {
        Case("真实 Harmony 装配合成选人节点", () => {
            var harmony = new Harmony("Spire1.background.contract.probe");
            try {
                harmony.CreateClassProcessor(typeof(CharacterSelectBackgroundPauseReadyPatch)).Patch();
                var screen = new NCharacterSelectScreen(); var bg = new Control { Name = "AnimatedBg", Visible = false };
                var leaf = new Node { ProcessMode = Mode.Always }; bg.AddChild(leaf); screen.AddChild(bg); screen.Enter(); Callable.Drain();
                Assert(screen.ReadyCalls == 1 && leaf.ProcessMode == Mode.Disabled, "postfix was not effective");
                using var owner = CharacterSelectBackgroundPause.Attach(bg);
                Assert(ReferenceEquals(owner, CharacterSelectBackgroundPause.Attach(bg)), "attach was not idempotent");
            } finally { harmony.Unpatch(typeof(NCharacterSelectScreen).GetMethod(nameof(NCharacterSelectScreen._Ready))!, HarmonyPatchType.All, harmony.Id); }
        });
        Case("隐藏和显示精确恢复全部处理模式", () => {
            var root = Root(); var nodes = Enum.GetValues<Mode>().Select(m => new Node { ProcessMode = m }).ToArray();
            foreach (var node in nodes) root.AddChild(node);
            using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain();
            root.Visible = false; Callable.Drain(); root.Visible = false; Callable.Drain();
            Assert(nodes.All(n => n.ProcessMode == Mode.Disabled), "explicit modes escaped");
            root.Visible = true; Callable.Drain();
            Assert(nodes.Select(n => n.ProcessMode).SequenceEqual(Enum.GetValues<Mode>()), "original modes lost");
        });
        Case("动态子树在 Ready 和直接 deferred 初始化后保存模式", () => {
            var root = Root(false); using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain();
            var branch = new Control(); var leaf = new Node(); branch.AddChild(leaf);
            leaf.Ready += () => { leaf.ProcessMode = Mode.WhenPaused; Callable.From(() => leaf.ProcessMode = Mode.Always).CallDeferred(); };
            root.AddChild(branch); Callable.Drain(); Assert(leaf.ProcessMode == Mode.Disabled, "new leaf was not paused");
            root.Visible = true; Callable.Drain(); Assert(leaf.ProcessMode == Mode.Always, "pre-initialization mode captured");
        });
        Case("移出隐藏背景时同步恢复而不污染新父节点", () => {
            var root = Root(false); var outside = Root(); var leaf = new Node { ProcessMode = Mode.Always }; root.AddChild(leaf);
            using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain(); outside.AddChild(leaf);
            Assert(leaf.ProcessMode == Mode.Always, "mode not restored on exit"); Callable.Drain();
            root.Visible = true; root.Visible = false; Callable.Drain(); Assert(leaf.ProcessMode == Mode.Always, "former owner still controls moved node");
        });
        Case("单独隐藏的背景分支不暂停可见背景", () => {
            var root = Root(); var hidden = new Control { Visible = false }; var shown = new Control();
            var a = new Node { ProcessMode = Mode.Always }; var b = new Node { ProcessMode = Mode.Always };
            hidden.AddChild(a); shown.AddChild(b); root.AddChild(hidden); root.AddChild(shown);
            using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain();
            Assert(a.ProcessMode == Mode.Disabled && b.ProcessMode == Mode.Always && root.ProcessMode == Mode.Inherit, "background boundaries changed");
            var decoration = new Control { Visible = false, ProcessMode = Mode.Always }; shown.AddChild(decoration); Callable.Drain();
            Assert(decoration.ProcessMode == Mode.Always, "inner decoration incorrectly treated as background");
        });
        Case("移出重入使旧 deferred 失效并重新接管", () => {
            var root = Root(false); var leaf = new Node { ProcessMode = Mode.Pausable }; root.AddChild(leaf);
            using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain();
            root.Visible = true; root.Visible = false; root.Exit(); Assert(leaf.ProcessMode == Mode.Pausable, "exit did not restore");
            root.Enter(); Callable.Drain(); Assert(leaf.ProcessMode == Mode.Disabled, "reentry not managed");
            owner.Dispose(); owner.Dispose(); Assert(leaf.ProcessMode == Mode.Pausable, "dispose did not restore");
            using var next = CharacterSelectBackgroundPause.Attach(root); Assert(!ReferenceEquals(owner, next), "disposed controller reused"); Callable.Drain();
        });
        Case("应用模式期间重入移除节点不留下暂停状态", () => {
            var root = Root(); var trigger = new Node { ProcessMode = Mode.Always }; var victim = new Node { ProcessMode = Mode.WhenPaused };
            root.AddChild(trigger); root.AddChild(victim); using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain();
            trigger.ModeChanged = mode => { if (mode == Mode.Disabled) root.RemoveChild(victim); };
            root.Visible = false; Callable.Drain(); Assert(victim.Parent == null && victim.ProcessMode == Mode.WhenPaused, "reentrant removal corrupted mode");
        });
        Case("线程与失效对象门禁及空闲无轮询", () => {
            var root = Root(); using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain(); Assert(Callable.Pending == 0, "idle still queues work");
            OS.Caller = 2; bool rejected = false; try { CharacterSelectBackgroundPause.Attach(root); } catch (InvalidOperationException) { rejected = true; }
            OS.Caller = 1; Assert(rejected, "wrong thread accepted");
            var missing = new NCharacterSelectScreen(); Assert(CharacterSelectBackgroundPause.Attach(missing) == null, "missing container not skipped");
            missing.Destroy(); Assert(CharacterSelectBackgroundPause.Attach(missing) == null, "invalid screen not skipped");
        });
        Case("确认无事件的晚期外部模式改写仍是门禁边界", () => {
            var root = Root(false); var leaf = new Node { ProcessMode = Mode.Inherit }; root.AddChild(leaf);
            using var owner = CharacterSelectBackgroundPause.Attach(root); Callable.Drain(); leaf.ProcessMode = Mode.Always; Callable.Drain();
            Assert(leaf.ProcessMode == Mode.Always, "unexpected continuous polling"); root.Visible = true; Callable.Drain();
            Assert(leaf.ProcessMode == Mode.Inherit, "first saved original was overwritten");
        });
        Console.WriteLine($"探针场景: {_cases}, 失败: {_failures}");
        Console.WriteLine("证据边界: 真实补丁源码与真实 0Harmony, Godot 树与事件为受控契约桩, 不覆盖原生引擎时序, Spine, 多人大厅或帧时间.");
        return _failures == 0 ? 0 : 1;
    }
}