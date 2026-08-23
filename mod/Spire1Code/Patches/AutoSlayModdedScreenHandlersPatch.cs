using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;

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
/// invoke it. Registration goes through reflection into <c>AutoSlayer._screenHandlers</c>
/// because the engine offers no extension API yet (drafted upstream).
/// <c>NPortalMapBuilderScreen</c> is NOT registered — its minigame has no public completion.
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

/// <summary>Drives an AFTP minigame overlay by invoking its minigame's public Complete().</summary>
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

        // Give the outcome application a beat before the drain loop re-polls overlays.
        await Task.Delay(500, ct);
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
