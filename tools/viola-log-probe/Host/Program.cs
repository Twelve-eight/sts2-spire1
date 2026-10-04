using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Experimental.MenuPerformance;

internal static class Program
{
    private static int _cases, _failures, _evaluations;
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Case(string name, Action check)
    {
        _cases++;
        try { check(); Console.WriteLine("通过: " + name); }
        catch (Exception ex) { _failures++; Console.WriteLine("失败: " + name + ": " + ex.Message); }
    }
    private static string Argument() { _evaluations++; return "evaluated"; }
    private sealed record Observation(string Value, int ResourceChecks, int LogLines);
    private static Observation Observe(MethodInfo getter, MethodInfo postfix, CardModel? card, bool present)
    {
        GD.Messages.Clear(); ResourceLoader.Checks = 0; ResourceLoader.Present = present;
        string value;
        if (card == null) { object?[] parameters = { null, "unchanged" }; postfix.Invoke(null, parameters); value = (string)parameters[1]!; }
        else value = (string)getter.Invoke(card, null)!;
        return new Observation(value, ResourceLoader.Checks, GD.Messages.Count);
    }
    private static List<CodeInstruction> Rewrite(List<CodeInstruction> source, MethodBase target)
    {
        var method = typeof(ViolaPortraitLogCompat).GetMethod("RewritePortraitLogs", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IEnumerable<CodeInstruction>)method.Invoke(null, new object[] { source, target })!).ToList();
    }
    private static List<CodeInstruction> Make(int count)
    {
        var print = typeof(GD).GetMethod(nameof(GD.Print), new[] { typeof(string) })!;
        var argument = typeof(Program).GetMethod(nameof(Argument), BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = new List<CodeInstruction>();
        for (int i = 0; i < count; i++) { result.Add(new CodeInstruction(OpCodes.Call, argument)); result.Add(new CodeInstruction(OpCodes.Call, print)); }
        result.Add(new CodeInstruction(OpCodes.Ret)); return result;
    }
    public static int Main(string[] args)
    {
        Case("缺少目标程序集时不安装", () => Assert(!ViolaPortraitLogCompat.TryInstall() && ViolaPortraitLogCompat.LastStatus == "mod-absent", "mod-absent gate failed"));
        var fixture = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        var target = fixture.GetType("ViolaSnakeBite.CardModel_GetPortrait_Patch", true)!.GetMethod("Postfix")!;
        var getter = typeof(CardModel).GetProperty(nameof(CardModel.PortraitPath))!.GetMethod!;
        const string foreignOwner = "probe.original.Viola";
        var original = new Harmony(foreignOwner);
        string owner = (string)typeof(ViolaPortraitLogCompat).GetField("OwnerId", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        var scenarios = new (string Name, CardModel? Card, bool Present)[] {
            ("空对象", null, false), ("非目标卡", new CardModel(), true),
            ("目标卡资源缺失", new SnakeBite(), false), ("目标卡资源存在", new SnakeBite(), true),
            ("大小写不敏感", new SNAKEBITE(), true)
        };
        try
        {
            original.Patch(getter, postfix: new HarmonyMethod(target));
            var before = scenarios.Select(s => Observe(getter, target, s.Card, s.Present)).ToArray();
            Case("原第三方包装调用确实产生日志", () => Assert(before.All(o => o.LogLines > 0), "baseline was already quiet"));
            Case("安装兼容层并实际完成五处变换", () => Assert(ViolaPortraitLogCompat.TryInstall() && ViolaPortraitLogCompat.LastRewriteCount == 5, "install status=" + ViolaPortraitLogCompat.LastStatus + ", error=" + ViolaPortraitLogCompat.LastError));
            for (int index = 0; index < scenarios.Length; index++)
            {
                int i = index;
                Case("已包装调用保持语义并静音: " + scenarios[i].Name, () => {
                    var after = Observe(getter, target, scenarios[i].Card, scenarios[i].Present);
                    Assert(after.Value == before[i].Value && after.ResourceChecks == before[i].ResourceChecks, "portrait/resource semantics changed");
                    Assert(after.LogLines == 0, "already-wrapped getter still logs " + after.LogLines);
                });
            }
            Case("重复安装幂等且保留原第三方 owner", () => {
                Assert(ViolaPortraitLogCompat.TryInstall(), "second install rejected");
                Assert(Harmony.GetPatchInfo(target)?.Transpilers.Count(p => p.owner == owner) == 1, "own transpiler duplicated");
                Assert(Harmony.GetPatchInfo(getter)?.Postfixes.Count(p => p.owner == foreignOwner) == 1, "foreign postfix modified");
            });
            Case("其它 GD.Print 没有被全局屏蔽", () => { GD.Messages.Clear(); GD.Print("outside target"); Assert(GD.Messages.Count == 1, "global print changed"); });
            foreach (int count in new[] { 4, 6 })
                Case("调用数量漂移整段退让: " + count, () => {
                    var input = Make(count); var output = Rewrite(input, target);
                    Assert(input.Count == output.Count && input.Zip(output).All(p => ReferenceEquals(p.First, p.Second)), "partial mutation on drift");
                });
            Case("错误目标和调用前缀整段退让", () => {
                var input = Make(5); var output = Rewrite(input, getter); Assert(input.Zip(output).All(p => ReferenceEquals(p.First, p.Second)), "wrong target rewritten");
                input.Insert(1, new CodeInstruction(OpCodes.Tailcall)); output = Rewrite(input, target);
                Assert(input.Zip(output).All(p => ReferenceEquals(p.First, p.Second)), "prefixed call partially rewritten");
            });
            Case("保留 labels 和 exception blocks 且不原地修改输入", () => {
                var input = Make(5); var dm = new DynamicMethod("labels", typeof(void), Type.EmptyTypes); var label = dm.GetILGenerator().DefineLabel();
                input[1].labels.Add(label); input[1].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                var output = Rewrite(input, target); Assert(input[1].opcode == OpCodes.Call && output[1].opcode == OpCodes.Pop, "input mutated");
                Assert(output[1].labels.SequenceEqual(input[1].labels) && output[1].blocks.SequenceEqual(input[1].blocks), "metadata lost");
            });
            Case("变换后的真实动态方法仍求值参数且 IL 栈有效", () => {
                var output = Rewrite(Make(5), target); var dm = new DynamicMethod("argument_evaluation", typeof(void), Type.EmptyTypes, typeof(Program), true); var il = dm.GetILGenerator();
                foreach (var code in output) { if (code.operand is MethodInfo mi) il.Emit(code.opcode, mi); else if (code.operand == null) il.Emit(code.opcode); else throw new Exception("unexpected operand"); }
                _evaluations = 0; GD.Messages.Clear(); dm.CreateDelegate<Action>()();
                Assert(_evaluations == 5 && GD.Messages.Count == 0, "argument evaluation or print changed");
            });
        }
        finally
        {
            new Harmony(owner).Unpatch(target, HarmonyPatchType.Transpiler, owner);
            original.Unpatch(getter, HarmonyPatchType.Postfix, foreignOwner);
        }
        Console.WriteLine($"探针场景: {_cases}, 失败: {_failures}");
        Console.WriteLine("证据边界: 真实游戏 0Harmony, 链接实际兼容源码和反编译目标的隔离重编译副本; Godot 资源与卡牌为契约桩, 不代表原始安装 DLL 或游戏实机已验收.");
        return _failures == 0 ? 0 : 1;
    }
}