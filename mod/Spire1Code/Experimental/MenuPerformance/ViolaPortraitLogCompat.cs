#if SPIRE1_MENU_PERFORMANCE_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Spire1.Spire1Code.Experimental.MenuPerformance;

// The existing MainFile attribute scan installs only this menu-ready trigger.
// The optional mod is resolved later, without loading assemblies from disk.
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class ViolaPortraitLogCompat
{
    private const string OwnerId = "Spire1.Experimental.MenuPerformance.ViolaPortraitLogCompat";
    private const string AssemblyName = "ViolaSnakeBite";
    private const string TypeName = "ViolaSnakeBite.CardModel_GetPortrait_Patch";
    private const int ExpectedPrintCalls = 5;
    private static readonly object InstallGate = new();

    // These are registration diagnostics, not proof of gameplay or wrapper behavior.
    internal static string LastStatus { get; private set; } = "not-attempted";
    internal static string? LastError { get; private set; }
    internal static int LastRewriteCount { get; private set; }

    [HarmonyPostfix]
    private static void AfterMainMenuReady()
    {
        TryInstall();
    }

    internal static bool TryInstall()
    {
        lock (InstallGate)
        {
            MethodInfo? target = null;
            Harmony? harmony = null;
            bool patchAttempted = false;
            LastError = null;
            try
            {
                Assembly[] candidates = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => string.Equals(assembly.GetName().Name, AssemblyName,
                        StringComparison.Ordinal)).ToArray();
                if (candidates.Length != 1)
                {
                    LastStatus = candidates.Length == 0 ? "mod-absent" : "ambiguous-assembly";
                    return false;
                }

                Type? type = candidates[0].GetType(TypeName, throwOnError: false, ignoreCase: false);
                target = type?.GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                    binder: null, types: new[] { typeof(CardModel), typeof(string).MakeByRefType() },
                    modifiers: null);
                if (!IsExpectedTarget(target))
                {
                    LastStatus = "unsupported-abi";
                    return false;
                }

                MethodInfo rewrite = typeof(ViolaPortraitLogCompat).GetMethod(
                    nameof(RewritePortraitLogs), BindingFlags.Static | BindingFlags.NonPublic)!;
                if (Harmony.GetPatchInfo(target!)?.Transpilers.Any(patch =>
                    patch.owner == OwnerId && patch.PatchMethod == rewrite) == true)
                {
                    LastStatus = "already-registered-unverified";
                    return true;
                }

                // Preflight the full current chain before registering anything. The
                // transpiler repeats this check because Harmony may rebuild it later.
                List<CodeInstruction> current = PatchProcessor.GetCurrentInstructions(
                    target!, int.MaxValue, null);
                if (FindPrintCalls(current) is null)
                {
                    LastStatus = "unsupported-il";
                    return false;
                }

                harmony = new Harmony(OwnerId);
                LastRewriteCount = 0;
                patchAttempted = true;
                harmony.Patch(target!, transpiler: new HarmonyMethod(rewrite)
                {
                    priority = Priority.Last
                });
                if (LastRewriteCount != ExpectedPrintCalls)
                {
                    harmony.Unpatch(target!, HarmonyPatchType.Transpiler, OwnerId);
                    patchAttempted = false;
                    LastStatus = "il-changed-during-install";
                    return false;
                }

                // Do not replace/re-register Viola's CardModel getter postfix. Its
                // already-created Harmony wrapper must be checked by the real probe.
                LastStatus = "registered-unverified";
                return true;
            }
            catch (Exception error)
            {
                LastStatus = "install-failed";
                LastError = error.GetType().FullName;
                if (patchAttempted && target is not null && harmony is not null)
                {
                    try
                    {
                        // Never remove another owner's patches or use UnpatchAll.
                        harmony.Unpatch(target, HarmonyPatchType.Transpiler, OwnerId);
                    }
                    catch (Exception rollbackError)
                    {
                        LastStatus = "rollback-failed-unverified";
                        LastError += "; " + rollbackError.GetType().FullName;
                    }
                }
                return false;
            }
        }
    }

    private static bool IsExpectedTarget(MethodBase? method)
    {
        if (method is not MethodInfo info || !info.IsPublic || !info.IsStatic
            || info.IsAbstract || info.ContainsGenericParameters || info.IsGenericMethod
            || info.ReturnType != typeof(void) || info.Name != "Postfix"
            || (info.CallingConvention & CallingConventions.VarArgs) != 0
            || info.DeclaringType?.FullName != TypeName
            || !string.Equals(info.DeclaringType.Assembly.GetName().Name, AssemblyName,
                StringComparison.Ordinal))
            return false;

        ParameterInfo[] parameters = info.GetParameters();
        return parameters.Length == 2
            && parameters[0].ParameterType == typeof(CardModel)
            && parameters[1].ParameterType == typeof(string).MakeByRefType()
            && !parameters[1].IsOut && !parameters[1].IsIn
            && info.GetMethodBody() is not null;
    }

    // Deliberately not named Transpiler and not marked HarmonyTranspiler: the
    // attribute-scanned class must not apply this rewrite to NMainMenu._Ready.
    private static IEnumerable<CodeInstruction> RewritePortraitLogs(
        IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        List<CodeInstruction> source = instructions.ToList();
        LastRewriteCount = 0;
        if (!IsExpectedTarget(original))
            return source;

        List<int>? calls = FindPrintCalls(source);
        if (calls is null)
            return source;

        // Validate everything first, then copy. Never partially mutate the input.
        // The copy constructor preserves both labels and exception boundaries;
        // Clone() would discard those. Argument evaluation is left untouched.
        List<CodeInstruction> result = source.Select(code => new CodeInstruction(code)).ToList();
        foreach (int index in calls)
        {
            result[index].opcode = OpCodes.Pop;
            result[index].operand = null;
        }
        LastRewriteCount = calls.Count;
        return result;
    }

    private static List<int>? FindPrintCalls(IReadOnlyList<CodeInstruction> instructions)
    {
        List<int> calls = new(ExpectedPrintCalls);
        for (int index = 0; index < instructions.Count; index++)
        {
            CodeInstruction code = instructions[index];
            if (code.operand is not MethodInfo method || method.Name != nameof(GD.Print)
                || method.DeclaringType?.FullName != typeof(GD).FullName)
                continue;

            // Fail closed for a different Godot assembly, indirect call, prefix,
            // non-void return, or an overload outside the local SDK contract.
            ParameterInfo[] parameters = method.GetParameters();
            if (code.opcode != OpCodes.Call || method.DeclaringType != typeof(GD)
                || !method.IsStatic || method.ReturnType != typeof(void)
                || method.ContainsGenericParameters || method.IsGenericMethod
                || (method.CallingConvention & CallingConventions.VarArgs) != 0
                || parameters.Length != 1
                || (parameters[0].ParameterType != typeof(string)
                    && parameters[0].ParameterType != typeof(object[]))
                || (index > 0 && instructions[index - 1].opcode.OpCodeType == OpCodeType.Prefix))
                return null;

            calls.Add(index);
            if (calls.Count > ExpectedPrintCalls)
                return null;
        }
        return calls.Count == ExpectedPrintCalls ? calls : null;
    }
}
#endif
