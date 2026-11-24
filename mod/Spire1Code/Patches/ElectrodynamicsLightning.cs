using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// StS1 Defect - Electrodynamics. While the orb owner has <see cref="ElectrodynamicsPower"/>,
/// Lightning passive and evoke damage hits ALL eligible enemies instead of one random target.
/// <para>
/// ENGINE FACT: host <c>LightningOrb.Passive</c> and <c>LightningOrb.Evoke</c> both funnel into
/// the private <c>ApplyLightningDamage</c>, which builds the hittable-opponent list, rolls the
/// random target (only when no explicit target is given), fires the evoke activation, VFX and
/// SFX, then calls <c>CreatureCmd.Damage</c>. The prefix below replaces exactly that method and
/// reuses the same list predicate, activation and presentation order with every target; damage
/// stays <see cref="ValueProp.Unpowered"/> like the host. For every ordinary call (no power) it
/// returns true before touching the target, value or RNG.
/// </para>
/// <para>
/// INSTALL SEAM: <c>MainFile.Initialize</c> installs this once after the existing
/// pre-registration gate chain succeeds and before Phase2 content registration; a throw goes
/// through the existing <c>MarkDeterministicUnavailable</c> path. <see cref="Install"/> is
/// idempotent and re-validates the exact owned patch set on every entry.
/// </para>
/// <para>
/// SAFETY: an owner that has the power but loses the AoE prefix must fail loudly rather than
/// silently fall back to single-target lightning. The safety prefixes on the LightningOrb,
/// OrbCmd and PowerCmd consumer seams call <see cref="RequireHealthy"/> only when the power is
/// actually attached, and the health check re-proves both owned patches via GetPatchInfo.
/// </para>
/// </summary>
internal static class ElectrodynamicsLightning
{
    private const string HarmonyId = "Spire1.Electrodynamics.Lightning";
    private const string SafetyId = HarmonyId + ".Safety";
    private const string GateOwner = MainFile.ModId;
    private const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    private static readonly object Sync = new();
    private static readonly Harmony Patcher = new(HarmonyId);
    private static readonly Harmony Safety = new(SafetyId);

    private static readonly MethodInfo PrefixMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ReceivedPostfix = ElectrodynamicsAmount.ReceivedPostfix;
    private static readonly MethodInfo GuardOrbMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardOrb), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GuardPowerMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardPower), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GuardEvokeMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardEvoke), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GuardPassiveMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardPassive), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GuardNextMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardNext), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GuardLastMethod = typeof(ElectrodynamicsLightning)
        .GetMethod(nameof(GuardLast), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// The native received-amount hook that carries the nonstacking clamp. Exposed so the health
    /// check can prove the owned postfix (and only it) rewrites the returned decimal; the existing
    /// fallback gate is the only accepted prefix on the same hook.
    /// </summary>
    internal static MethodInfo ReceivedConsumer => ElectrodynamicsAmount.ReceivedMethod;

    private static readonly MethodInfo ExistingGatePrefix = Spire1PowersFallbackReceivedPatch.PrefixMethod
        ?? throw new MissingMemberException("Spire1PowersFallbackReceivedPatch.Prefix");

    /// <summary>Every consumer seam that must fail loudly if the owned patch set is missing.</summary>
    private static readonly MethodInfo[] SafetyTargets =
    [
        typeof(LightningOrb).GetMethod(nameof(LightningOrb.Passive))!,
        typeof(LightningOrb).GetMethod(nameof(LightningOrb.Evoke))!,
        typeof(LightningOrb).GetMethod("ApplyLightningDamage", Methods)!,
        ElectrodynamicsAmount.ModifyMethod,
        typeof(PowerCmd).GetMethod(nameof(PowerCmd.Apply),
            [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])!,
        typeof(OrbCmd).GetMethod("Evoke", BindingFlags.Static | BindingFlags.NonPublic)!,
        typeof(OrbCmd).GetMethod(nameof(OrbCmd.Passive))!,
        typeof(OrbCmd).GetMethod(nameof(OrbCmd.EvokeNext))!,
        typeof(OrbCmd).GetMethod(nameof(OrbCmd.EvokeLast))!
    ];

    /// <summary>The private host damage seam; this prefix owns the all-enemies rewrite.</summary>
    private static readonly MethodInfo Consumer = typeof(LightningOrb).GetMethod("ApplyLightningDamage",
        BindingFlags.Instance | BindingFlags.NonPublic, null,
        [typeof(decimal), typeof(Creature), typeof(PlayerChoiceContext), typeof(bool)], null)
        ?? throw new MissingMethodException(typeof(LightningOrb).FullName, "ApplyLightningDamage");

    private static readonly Action<OrbModel> PlayEvokeSfx = (typeof(OrbModel)
        .GetMethod("PlayEvokeSfx", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(OrbModel).FullName, "PlayEvokeSfx"))
        .CreateDelegate<Action<OrbModel>>();

    private static volatile bool _installed;
    private static volatile bool _failed;

    /// <summary>Content gate: the powers group must be open and the mod must not be hard-closed.</summary>
    internal static bool ContentEnabled => !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers);

    /// <summary>
    /// Installs the safety guards and the two owned patches (damage prefix + received-amount
    /// postfix). Idempotent: a repeated call only re-validates the exact owned patch set and
    /// returns. Throws on any signature or patch-shape failure after removing the two owned
    /// patches; the caller routes that into the existing deterministic-unavailable state.
    /// <para>
    /// Deliberately no transpiler on <c>PowerCmd.ModifyAmount</c>: <c>Spire1FlexLease</c> already
    /// owns that state machine's received-hook call site (and asserts the original single site),
    /// so the nonstacking clamp is a postfix on the hook itself and composes with the Flex wrapper.
    /// </para>
    /// </summary>
    internal static void Install()
    {
        lock (Sync)
        {
            try
            {
                EnsureSafety();
                ElectrodynamicsAmount.ValidateShape();
                if (Consumer.ReturnType != typeof(Task<IEnumerable<Creature>>) || ReceivedConsumer.ReturnType != typeof(decimal))
                {
                    throw new InvalidOperationException("Unsupported Electrodynamics consumer signature.");
                }
                ValidateTransforms(_installed);
                if (_installed)
                {
                    _failed = false;
                    return;
                }
                Patcher.Patch(Consumer, prefix: new HarmonyMethod(PrefixMethod));
                Patcher.Patch(ReceivedConsumer, postfix: new HarmonyMethod(ReceivedPostfix));
                ValidateTransforms(true);
                _failed = false;
                _installed = true;
            }
            catch (Exception original)
            {
                _installed = false;
                _failed = true;
                List<Exception>? cleanup = null;
                foreach (var (target, patch) in new[] { (Consumer, PrefixMethod), (ReceivedConsumer, ReceivedPostfix) })
                {
                    try { Patcher.Unpatch(target, patch); }
                    catch (Exception error) { (cleanup ??= new()).Add(error); }
                }
                if (cleanup != null)
                {
                    throw new AggregateException("Electrodynamics install and cleanup failed", new[] { original }.Concat(cleanup));
                }
                throw;
            }
        }
    }

    /// <summary>Damage seam: exactly one owned prefix, and nothing else patching that method.</summary>
    private static void RequireDamagePrefix(bool installed)
    {
        var info = Harmony.GetPatchInfo(Consumer);
        if (info == null)
        {
            if (installed)
            {
                throw new InvalidOperationException("Electrodynamics required prefix missing: " + Consumer.Name);
            }
            return;
        }
        var own = info.Prefixes.Where(p => p.owner != SafetyId).ToArray();
        bool conflict = info.Postfixes.Any() || info.Finalizers.Any() || info.Transpilers.Any()
            || own.Any(p => p.owner != HarmonyId || p.PatchMethod != PrefixMethod)
            || own.Length != (installed ? 1 : 0);
        if (conflict)
        {
            throw new InvalidOperationException("Electrodynamics unsupported damage patch set: " + Consumer.Name);
        }
    }

    /// <summary>
    /// Received-amount seam: exactly one owned postfix when installed. The existing gate prefix is
    /// the only accepted prefix (it may be absent until the fallback layer is reconciled), and a
    /// foreign prefix/postfix/transpiler/finalizer fails closed.
    /// </summary>
    private static void RequireReceivedPostfix(bool installed)
    {
        var info = Harmony.GetPatchInfo(ReceivedConsumer);
        if (info == null)
        {
            if (installed)
            {
                throw new InvalidOperationException("Electrodynamics required postfix missing: " + ReceivedConsumer.Name);
            }
            return;
        }
        bool foreignPostfix = info.Postfixes.Any(p => p.owner != HarmonyId);
        bool ownExact = info.Postfixes.Count(p => p.owner == HarmonyId && p.PatchMethod == ReceivedPostfix)
            == (installed ? 1 : 0);
        bool conflict = info.Finalizers.Any() || info.Transpilers.Any()
            || info.Prefixes.Any(p => p.owner != GateOwner || p.PatchMethod != ExistingGatePrefix)
            || foreignPostfix || !ownExact;
        if (conflict)
        {
            throw new InvalidOperationException("Electrodynamics unsupported received hook patch set");
        }
    }

    private static void ValidateTransforms(bool installed)
    {
        RequireDamagePrefix(installed);
        RequireReceivedPostfix(installed);
    }

    private static void EnsureSafety()
    {
        foreach (MethodInfo target in SafetyTargets)
        {
            MethodInfo guard = target.DeclaringType == typeof(LightningOrb) ? GuardOrbMethod
                : target.DeclaringType == typeof(OrbCmd) ? target.Name switch
                {
                    "Passive" => GuardPassiveMethod,
                    "EvokeNext" => GuardNextMethod,
                    "EvokeLast" => GuardLastMethod,
                    _ => GuardEvokeMethod
                }
                : GuardPowerMethod;
            var info = Harmony.GetPatchInfo(target);
            int count = info?.Prefixes.Count(p => p.owner == SafetyId && p.PatchMethod == guard) ?? 0;
            if (count == 0)
            {
                Safety.Patch(target, prefix: new HarmonyMethod(guard) { priority = Priority.First });
            }
            else if (count != 1)
            {
                throw new InvalidOperationException("Duplicate Electrodynamics safety guard");
            }
            if (Harmony.GetPatchInfo(target)?.Prefixes.Count(p => p.owner == SafetyId && p.PatchMethod == guard) != 1)
            {
                throw new InvalidOperationException("Electrodynamics safety installation incomplete");
            }
        }
    }

    /// <summary>
    /// Health check used by the card and by every safety guard: the owned patch set must still
    /// be exactly what <see cref="Install"/> applied. A failed check uninstalls and throws so no
    /// consumer keeps running on a half-mounted rewrite.
    /// </summary>
    internal static void RequireHealthy()
    {
        if (_failed || !_installed)
        {
            throw new InvalidOperationException("Electrodynamics is unavailable; explicit successful Install required");
        }
        try
        {
            ValidateTransforms(true);
        }
        catch
        {
            Uninstall();
            throw;
        }
    }

    private static void GuardOrb(LightningOrb __instance)
    {
        if (__instance.Owner.Creature.GetPower<ElectrodynamicsPower>() is { Amount: > 0 })
        {
            RequireHealthy();
        }
    }

    private static void GuardPower(PowerModel power)
    {
        if (power is ElectrodynamicsPower)
        {
            RequireHealthy();
        }
    }

    private static void GuardEvoke(OrbModel evokedOrb)
    {
        if (evokedOrb is LightningOrb lightning && lightning.Owner.Creature.GetPower<ElectrodynamicsPower>() is { Amount: > 0 })
        {
            RequireHealthy();
        }
    }

    private static void GuardPassive(OrbModel orb)
    {
        if (orb is LightningOrb lightning && lightning.Owner.Creature.GetPower<ElectrodynamicsPower>() is { Amount: > 0 })
        {
            RequireHealthy();
        }
    }

    private static void GuardNext(Player player)
    {
        var orbs = player.PlayerCombatState!.OrbQueue.Orbs;
        if (orbs.Count > 0)
        {
            GuardEvoke(orbs[0]);
        }
    }

    private static void GuardLast(Player player)
    {
        var orbs = player.PlayerCombatState!.OrbQueue.Orbs;
        if (orbs.Count > 0)
        {
            GuardEvoke(orbs[orbs.Count - 1]);
        }
    }

    /// <summary>Card entry: the rewrite must be mounted before the card can promise it.</summary>
    internal static void RequireInstalled() => RequireHealthy();

    private static void Uninstall()
    {
        lock (Sync)
        {
            _installed = false;
            _failed = true;
            Patcher.Unpatch(Consumer, PrefixMethod);
            Patcher.Unpatch(ReceivedConsumer, ReceivedPostfix);
        }
    }

    /// <summary>
    /// True only while the owner still has a live own power, the patch set is healthy and the
    /// powers content group is open. This is the runtime-mutation seam the carrier mutates.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool AppliesTo(LightningOrb orb)
    {
        ElectrodynamicsPower? power = orb.Owner.Creature.GetPower<ElectrodynamicsPower>();
        if (power == null || power.Amount <= 0 || !ReferenceEquals(power.Owner, orb.Owner.Creature))
        {
            return false;
        }
        RequireHealthy();
        return ContentEnabled;
    }

    private static bool Prefix(LightningOrb __instance, decimal value, Creature? target,
        PlayerChoiceContext choiceContext, bool isEvoke, ref Task<IEnumerable<Creature>> __result)
    {
        // Return true without touching target/RNG/value for every ordinary call.
        if (!AppliesTo(__instance))
        {
            return true;
        }
        __result = DamageAll(__instance, value, choiceContext, isEvoke);
        return false;
    }

    private static async Task<IEnumerable<Creature>> DamageAll(LightningOrb orb, decimal value,
        PlayerChoiceContext context, bool isEvoke)
    {
        // Capture owner/value/targets before any activation callback or await.
        // No ambient flag survives nesting, cancellation or an exception.
        Creature dealer = orb.Owner.Creature;
        Creature[] targets = orb.CombatState.GetOpponentsOf(dealer).Where(e => e.IsHittable).ToArray();
        if (targets.Length == 0)
        {
            return Array.Empty<Creature>();
        }
        // Native Lightning passes a separate activation array; callbacks must not be able to
        // rewrite the damage snapshot by modifying that event payload.
        if (isEvoke)
        {
            orb.ActivateEvoke(targets.ToArray());
        }
        foreach (Creature creature in targets)
        {
            VfxCmd.PlayOnCreature(creature, "vfx/vfx_attack_lightning");
        }
        PlayEvokeSfx(orb);
        await CreatureCmd.Damage(context, targets, value, ValueProp.Unpowered, dealer);
        return targets;
    }
}
