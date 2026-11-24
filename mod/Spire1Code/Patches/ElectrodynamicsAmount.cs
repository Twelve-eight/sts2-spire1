using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Nonstacking half of Electrodynamics. <see cref="ElectrodynamicsPower"/> is
/// <see cref="MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Single"/>, so a repeated play
/// must not add amount. The clamp is a postfix on
/// <see cref="Hook.ModifyPowerAmountReceived"/> itself - not a transpiler on
/// <see cref="PowerCmd.ModifyAmount"/>.
/// <para>
/// COMPOSITION FACT: <c>Spire1FlexLease</c> already transpiles the <c>PowerCmd.ModifyAmount</c>
/// state machine and rewrites this exact received-hook call site to its own wrapper, which calls
/// <see cref="Hook.ModifyPowerAmountReceived"/> and returns that result unchanged. A second
/// transpiler on the same MoveNext cannot compose (each side asserts the original single call
/// site), so the clamp lives inside the hook invocation instead: it runs for the plain call and
/// for the Flex wrapper, after all modifiers, and only rewrites the returned decimal.
/// </para>
/// <para>
/// SEMANTICS: every native Given/Received modifier still executes. Only the final positive offset
/// of the already-attached own instance becomes 0 (repeated plays do not stack); a negative final
/// offset passes through so the power can still be reduced and removed. First application has no
/// attached instance, so the same postfix is a no-op there.
/// </para>
/// </summary>
internal static class ElectrodynamicsAmount
{
    /// <summary>
    /// Non-generic <c>PowerCmd.ModifyAmount</c>. Used only as a safety-guard target; this module
    /// never patches its instructions.
    /// </summary>
    internal static readonly MethodInfo ModifyMethod = typeof(PowerCmd).GetMethod(nameof(PowerCmd.ModifyAmount),
        [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])!;

    internal static readonly MethodInfo ReceivedMethod = typeof(Hook).GetMethod(nameof(Hook.ModifyPowerAmountReceived),
        [typeof(ICombatState), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature),
            typeof(IEnumerable<AbstractModel>).MakeByRefType()])!;

    /// <summary>The postfix that clamps the final received offset; installed on <see cref="ReceivedMethod"/>.</summary>
    internal static readonly MethodInfo ReceivedPostfix = typeof(ElectrodynamicsAmount).GetMethod(nameof(ClampReceived),
        BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Install-time shape check: the hook must still return decimal with the exact parameter list
    /// the gate already assumes, and the postfix may only bind parameters that hook actually has.
    /// A host rename would otherwise be a silent no-injection instead of a failed install.
    /// </summary>
    internal static void ValidateShape()
    {
        if (ReceivedMethod.ReturnType != typeof(decimal))
        {
            throw new InvalidOperationException("Hook.ModifyPowerAmountReceived no longer returns decimal");
        }
        ParameterInfo[] hook = ReceivedMethod.GetParameters();
        if (hook.Length != 6
            || hook[0].ParameterType != typeof(ICombatState) || hook[0].Name != "combatState"
            || hook[1].ParameterType != typeof(PowerModel) || hook[1].Name != "canonicalPower"
            || hook[2].ParameterType != typeof(Creature) || hook[2].Name != "target"
            || hook[3].ParameterType != typeof(decimal) || hook[3].Name != "amount"
            || hook[4].ParameterType != typeof(Creature) || hook[4].Name != "giver"
            || hook[5].ParameterType != typeof(IEnumerable<AbstractModel>).MakeByRefType())
        {
            throw new InvalidOperationException("Hook.ModifyPowerAmountReceived signature changed");
        }
        ParameterInfo[] postfix = ReceivedPostfix.GetParameters();
        if (postfix.Length != 3
            || !Binds(postfix[0], hook, "canonicalPower", typeof(PowerModel))
            || !Binds(postfix[1], hook, "target", typeof(Creature))
            || postfix[2].Name != "__result" || postfix[2].ParameterType != typeof(decimal).MakeByRefType())
        {
            throw new InvalidOperationException("Electrodynamics received-amount postfix shape changed");
        }
    }

    private static bool Binds(ParameterInfo patch, ParameterInfo[] hook, string name, Type type) =>
        patch.Name == name && patch.ParameterType == type
        && hook.Any(p => p.Name == name && p.ParameterType == type);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClampReceived(PowerModel canonicalPower, Creature target, ref decimal __result)
    {
        // Foreign powers keep the hook result untouched (no health read, no allocation).
        if (canonicalPower is not ElectrodynamicsPower)
        {
            return;
        }
        ElectrodynamicsLightning.RequireHealthy();
        __result = ClampPositiveOffset(canonicalPower, target, __result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static decimal ClampPositiveOffset(PowerModel power, Creature target, decimal offset)
    {
        ElectrodynamicsPower? attached = target.GetPower<ElectrodynamicsPower>();
        return offset > 0m && attached != null && ReferenceEquals(attached, power)
            && attached.Amount > 0 && ReferenceEquals(attached.Owner, target)
            ? 0m
            : offset;
    }
}
