using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// <c>--autoslay</c>-only: teaches the engine's smoke-test drain loop to drive Acts From The
/// Past's custom minigame overlays (<c>NWheelSpinScreen</c>, <c>NMatchAndKeepScreen</c>).
/// Without this, any verification run that rolls these shrine events stalls ("No handler for
/// screen type: ...") and the watchdog aborts the whole run — observed at Wheel of Change,
/// Act 1 Floor 12, seed P1SMOKE1.
/// <para>
/// The minigames pre-roll their outcome at construction and expose a public parameterless
/// <c>Complete()</c> on the minigame class, so driving them is: wait out the slide-in, then
/// invoke it, then sweep any FOLLOW-UP event-option buttons the result spawns (Wheel of
/// Change's remove-card outcome opens a second "begin removal" option after the engine's
/// EventRoomHandler has already returned). Registration goes through reflection into
/// <c>AutoSlayer._screenHandlers</c> because the engine offers no extension API yet (drafted
/// upstream). <c>NPortalMapBuilderScreen</c> is NOT registered — its minigame has no public
/// completion method.
/// </para>
/// </summary>
[HarmonyPatch]
internal static class AutoSlayModdedScreenHandlersPatch
{
    [HarmonyPatch(typeof(AutoSlayer), MethodType.Constructor)]
    [HarmonyPostfix]
    static void RegisterModdedHandlers(AutoSlayer __instance)
    {
        if (!AutoSlayImmortalityPatch.Active)
        {
            return;
        }
        if (AccessTools.Field(typeof(AutoSlayer), "_screenHandlers")?.GetValue(__instance)
            is not Dictionary<Type, IScreenHandler> handlers)
        {
            return;
        }
        foreach (string name in new[]
                 {
                     "ActsFromThePast.Minigames.NWheelSpinScreen",
                     "ActsFromThePast.Minigames.NMatchAndKeepScreen",
                 })
        {
            Type? screen = AccessTools.TypeByName(name);
            if (screen != null && !handlers.ContainsKey(screen))
            {
                handlers[screen] = new AftpMinigameScreenHandler(screen);
                MainFile.Logger.Info($"[Spire1] AutoSlay handler registered for {name}");
            }
        }
    }
}

/// <summary>Drives an AFTP minigame overlay: invoke the minigame's public Complete(), then
/// sweep follow-up event-option buttons the result spawns.</summary>
internal sealed class AftpMinigameScreenHandler : IScreenHandler
{
    private readonly Type _screenType;

    public AftpMinigameScreenHandler(Type screenType) => _screenType = screenType;

    public TimeSpan Timeout => TimeSpan.FromSeconds(30);

    public Type ScreenType => _screenType;

    public async Task HandleAsync(MegaCrit.Sts2.Core.Random.Rng random, CancellationToken ct)
    {
        AutoSlayLog.EnterScreen(_screenType.Name);
        // Let the slide-in animation play so timing-sensitive code observes a natural flow.
        await Task.Delay(1500, ct);

        object? screen = GetCurrentScreen();
        (object? instance, MethodInfo complete)? target = screen == null ? null : FindCompletable(screen);
        if (target == null)
        {
            AutoSlayLog.Warn($"{_screenType.Name}: no completable minigame found (screen present: {screen != null})");
            return;
        }

        target.Value.complete.Invoke(target.Value.instance, null);
        AutoSlayLog.Action($"Drove {_screenType.Name} minigame to completion");

        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        for (int sweep = 0; sweep < 8; sweep++)
        {
            await Task.Delay(600, ct);
            List<NEventOptionButton> buttons = UiHelper.FindAll<NEventOptionButton>(root)
                .Where(b => !b.Option.IsLocked)
                .ToList();
            if (buttons.Count == 0)
            {
                break;
            }
            foreach (NEventOptionButton button in buttons)
            {
                AutoSlayLog.Action($"Clicking follow-up event option: {button.Option.Title.GetFormattedText()}");
                await UiHelper.Click(button);
                await Task.Delay(400, ct);
            }
        }

        AutoSlayLog.ExitScreen(_screenType.Name);
    }

    private object? GetCurrentScreen()
    {
        MethodInfo? generic = typeof(AutoSlayer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "GetCurrentScreen" && m.IsGenericMethodDefinition);
        return generic?.MakeGenericMethod(_screenType).Invoke(null, null);
    }

    /// <summary>Finds a field on the screen whose type carries a public parameterless Complete(),
    /// returning that field's value plus the method. Falls back to the screen itself.</summary>
    private (object? instance, MethodInfo complete)? FindCompletable(object screen)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (FieldInfo field in _screenType.GetFields(flags))
        {
            MethodInfo? complete = field.FieldType.GetMethod("Complete", Type.EmptyTypes);
            if (complete != null)
            {
                return (field.GetValue(screen), complete);
            }
        }
        MethodInfo? own = _screenType.GetMethod("Complete", Type.EmptyTypes);
        return own == null ? null : (screen, own);
    }
}

/// <summary>
/// <c>--autoslay</c>-only: widens the engine's hard-coded run length. The main loop plays
/// <c>while (runState.TotalFloor &lt; 49)</c> — tuned for vanilla's three ~16-floor acts.
/// Ecosystem runs are longer (StS1-faithful acts run 16-17 floors EACH, plus Act4Heart's
/// fourth act), so TotalFloor crosses 49 around the act 3→4 transition and AutoSlayer
/// abandons a perfectly healthy run ("Run completed (max floor reached)") — observed right
/// at Act 4's first rest site, seed P1SMOKE1. Surgical fix: rewrite only the literal that
/// follows the <c>get_TotalFloor</c> call (49 → 120); real endings still come from the
/// victory / game-over paths inside the loop, and the 25-minute run timeout is the backstop.
/// </summary>
[HarmonyPatch(typeof(AutoSlayer), "PlayRunAsync")]
internal static class AutoSlayMaxFloorPatch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> WidenMaxFloor(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [.. instructions];
        for (int i = 0; i < codes.Count - 1; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt
                && codes[i].operand is MethodInfo m && m.Name == "get_TotalFloor"
                && codes[i + 1].Is(OpCodes.Ldc_I4_S, 49))
            {
                codes[i + 1] = new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)120) { labels = codes[i + 1].labels };
                break;
            }
        }
        return codes;
    }
}
