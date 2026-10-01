using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Grants successive triangular Strength deltas and adds the round number to enemy damage.
/// Scheduled deltas and accepted Strength are separate ledgers: a blocked grant is not retried.
/// </summary>
public sealed class DemonFormPower : CustomPowerModel
{
    // PowerModel.SetAmount clamps to these limits in the current engine.
    private const int StrengthLimit = 999999999;

    private sealed class Data
    {
        public decimal previousTarget;
        public decimal grantedStrength;
        public StrengthPower? strength;
        public bool initialized;
        public bool refreshing;
        public bool removed;
        public bool cleanupComplete;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Demon Form",
            "#Gain escalating *Strength*. Damage from enemies is increased by the round number.",
            "Escalating Strength and extra damage from enemies.");

    protected override object InitInternalData() => new Data();

    private int CurrentRound => Math.Max(0, Owner.CombatState?.RoundNumber ?? 1);

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        if (data.initialized)
        {
            return;
        }

        data.initialized = true;
        await RefreshStrength(new ThrowingPlayerChoiceContext());
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            await RefreshStrength(choiceContext);
        }
    }

    private async Task RefreshStrength(PlayerChoiceContext choiceContext)
    {
        Data data = GetInternalData<Data>();
        Creature owner = Owner;
        if (data.removed || data.refreshing || owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        int round = CurrentRound;
        decimal target = (decimal)round * (round + 1m) / 2m;
        decimal delta = target - data.previousTarget;
        data.previousTarget = target;
        if (delta == 0m || !owner.CanReceivePowers)
        {
            return;
        }

        ForgetPurgedStrength(owner, data);
        data.refreshing = true;
        try
        {
            Flash();
            StrengthPower? strength = owner.GetPower<StrengthPower>();
            if (strength == null)
            {
                await ApplyNewStrength(choiceContext, owner, delta, data);
            }
            else
            {
                // Attribute only the delta this form's own write actually produced. The old
                // before/after read across PowerCmd.ModifyAmount's awaited before hook also counted
                // whatever another source wrote in that window, and the exit rollback then removed
                // it. DemonFormStrengthTransaction observes the real SetAmount write instead, so a
                // before-hook write by another source never enters this ledger, and a post-write
                // failure still keeps the delta that was really stored.
                data.strength = strength;
                await DemonFormStrengthTransaction.ModifyAmountAsync(
                    choiceContext,
                    strength,
                    delta,
                    owner,
                    accepted => data.grantedStrength += accepted);
            }

            ForgetPurgedStrength(owner, data);
        }
        finally
        {
            data.refreshing = false;
            // A grant hook may remove this form while the awaited grant is still in flight.
            // AfterRemoved must not await that same grant recursively.
            if (data.removed)
            {
                await RemoveGrantedStrength(owner, data);
            }
        }
    }

    private static async Task ApplyNewStrength(PlayerChoiceContext choiceContext, Creature owner, decimal delta, Data data)
    {
        StrengthPower strength = (StrengthPower)ModelDb.Power<StrengthPower>().ToMutable();
        int acceptedAmount = 0;
        bool observed = false;
        void CaptureInitialAmount()
        {
            if (!observed)
            {
                observed = true;
                acceptedAmount = strength.Amount;
            }
        }

        // A non-generic Apply has no return value. A zeroed/blocked grant can leave the mutable
        // instance unattached. Capture the first actual SetAmount, not later reactive bonuses.
        strength.DisplayAmountChanged += CaptureInitialAmount;
        try
        {
            await PowerCmd.Apply(choiceContext, strength, owner, delta, owner, null, silent: true);
        }
        finally
        {
            strength.DisplayAmountChanged -= CaptureInitialAmount;
            if (observed && (ReferenceEquals(owner.GetPower<StrengthPower>(), strength) || strength.Amount == 0))
            {
                data.grantedStrength += acceptedAmount;
                data.strength = strength;
            }
        }
    }

    private static void ForgetPurgedStrength(Creature owner, Data data)
    {
        if (data.strength != null && data.strength.Amount != 0
            && !ReferenceEquals(owner.GetPower<StrengthPower>(), data.strength))
        {
            // Explicit removal of a nonzero aggregate also removed this form's contribution.
            // A zero-amount removal is different: other Strength may have cancelled our bonus.
            data.grantedStrength = 0m;
            data.strength = null;
        }
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // Current CreatureCmd/Hook dispatches Unpowered damage through additive hooks too.
        // Do not filter by IsPoweredAttack, and do not duplicate this in an HP-loss hook.
        return target == Owner && dealer != null && dealer.Side != Owner.Side ? CurrentRound : 0m;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Data data = GetInternalData<Data>();
        data.removed = true;
        if (!data.refreshing)
        {
            await RemoveGrantedStrength(oldOwner, data);
        }
    }

    private static async Task RemoveGrantedStrength(Creature owner, Data data)
    {
        if (data.cleanupComplete)
        {
            return;
        }

        ForgetPurgedStrength(owner, data);
        decimal granted = data.grantedStrength;
        data.cleanupComplete = true;
        data.grantedStrength = 0m;
        data.strength = null;
        if (granted == 0m || owner.IsDead || owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        StrengthPower? strength = owner.GetPower<StrengthPower>();
        int remaining = (int)Math.Clamp((strength?.Amount ?? 0) - granted, -StrengthLimit, StrengthLimit);
        // Source cleanup is not a new Strength debuff. A second Apply would let Artifact or
        // grant multipliers block/amplify the withdrawal and corrupt unrelated Strength.
        // SetAmount keeps the normal amount/UI events; zero still uses awaited PowerCmd.Remove.
        if (strength == null)
        {
            if (remaining != 0)
            {
                strength = (StrengthPower)ModelDb.Power<StrengthPower>().ToMutable();
                strength.ApplyInternal(owner, remaining, silent: true);
            }
        }
        else
        {
            strength.SetAmount(remaining, silent: true);
            if (strength.Amount == 0 && ReferenceEquals(owner.GetPower<StrengthPower>(), strength))
            {
                await PowerCmd.Remove(strength);
            }
        }
    }
}