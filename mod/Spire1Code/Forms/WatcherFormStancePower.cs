using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Owns only the two form effects of one real Watcher marker on one creature. The marker remains the
/// source of truth for external cards and relics. Internal data is recreated by PowerModel cloning.
/// </summary>
public abstract class WatcherFormStancePower : StancePower
{
    private sealed class Data
    {
        public PowerModel? NativeMarker;
        public PowerModel? FirstEffect;
        public PowerModel? SecondEffect;
        public int EnteredRound;
        public int EnteredTurnNumber;
        public bool Applied;
        public bool Detaching;
    }

    public abstract FormStanceKind Kind { get; }
    public sealed override string StanceName => FormStanceMode.StanceName(Kind);
    public sealed override PowerType Type => PowerType.Buff;
    public int EnteredRound => GetInternalData<Data>().EnteredRound;
    public int EnteredTurnNumber => GetInternalData<Data>().EnteredTurnNumber;
    internal PowerModel? NativeMarker => GetInternalData<Data>().NativeMarker;

    private string IconFile => Kind switch
    {
        FormStanceKind.Calm => "calm_power.png",
        FormStanceKind.Wrath => "wrath_power.png",
        FormStanceKind.Divinity => "divinity_power.png",
        _ => throw new InvalidOperationException("Unknown form stance")
    };

    public sealed override string CustomPackedIconPath => "res://Spire1/images/powers/" + IconFile;
    public sealed override string CustomBigIconPath => "res://Spire1/images/powers/big/" + IconFile;
    protected sealed override object InitInternalData() => new Data();

    internal void BindNativeMarker(PowerModel marker)
    {
        AssertMutable();
        Data data = GetInternalData<Data>();
        if (data.Applied || data.NativeMarker != null)
            throw new InvalidOperationException("A form carrier cannot be rebound to another stance entry");
        data.NativeMarker = marker;
    }

    public sealed override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!FormStanceMode.IsEnabled(target.Player))
            throw new InvalidOperationException("Form stances require the custom-run modifier");
        PowerModel? marker = NativeMarker;
        if (marker == null || !target.Powers.Contains(marker)
            || FormStanceWatcherBridge.KindOfMarker(marker.GetType()) != Kind)
            throw new InvalidOperationException("Form stances must enter through the real Watcher stance command");
        return Task.CompletedTask;
    }

    public sealed override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        if (data.Applied)
            return;
        data.Applied = true;
        Creature owner = Owner;
        data.EnteredRound = owner.CombatState?.RoundNumber ?? 0;
        // IncrementTurnNumber precedes all start hooks. It also distinguishes extra turns in one round.
        data.EnteredTurnNumber = owner.Player?.PlayerCombatState?.TurnNumber ?? 0;
        (data.FirstEffect, data.SecondEffect) = CreateEffects();
        try
        {
            if (!await ApplyEffect(data.FirstEffect, owner, cardSource))
            {
                await RemoveEffects(owner, data);
                return;
            }
            if (!await ApplyEffect(data.SecondEffect, owner, cardSource))
                await RemoveEffects(owner, data);
        }
        catch
        {
            await RemoveEffects(owner, data);
            throw;
        }
    }

    private (PowerModel First, PowerModel Second) CreateEffects()
    {
        switch (Kind)
        {
            case FormStanceKind.Calm:
                var serpent = (SerpentFormPower)ModelDb.Power<SerpentFormPower>().ToMutable();
                // Native Calm.AfterRemoved owns the sole +2 payment. Configure BEFORE applying.
                serpent.GrantExitEnergy = false;
                return (ModelDb.Power<VoidFormEffectPower>().ToMutable(), serpent);
            case FormStanceKind.Wrath:
                return (ModelDb.Power<DemonFormPower>().ToMutable(), ModelDb.Power<ReaperFormEffectPower>().ToMutable());
            case FormStanceKind.Divinity:
                return (ModelDb.Power<EchoFormEffectPower>().ToMutable(), ModelDb.Power<CelestialFormPower>().ToMutable());
            default:
                throw new InvalidOperationException("Unknown form stance");
        }
    }

    private async Task<bool> ApplyEffect(PowerModel effect, Creature owner, CardModel? source)
    {
        if (!IsCurrentEntry(owner) || CombatManager.Instance.IsEnding)
            return false;
        try
        {
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), effect, owner, 1m, owner, source, silent: true);
        }
        catch
        {
            if (owner.Powers.Contains(effect))
                await PowerCmd.Remove(effect);
            throw;
        }
        // AfterRemoved may already have cleared the data references while Apply was suspended.
        // Clean the local exact instance as well, so a late attachment cannot escape ownership.
        if (!IsCurrentEntry(owner) || CombatManager.Instance.IsEnding)
        {
            if (owner.Powers.Contains(effect))
                await PowerCmd.Remove(effect);
            return false;
        }
        if (!owner.Powers.Contains(effect))
            throw new InvalidOperationException("A power hook rejected a required form effect: " + effect.GetType().FullName);
        return true;
    }

    private bool IsCurrentEntry(Creature owner)
    {
        Data data = GetInternalData<Data>();
        return !data.Detaching && owner.Powers.Contains(this) && data.NativeMarker != null
            && owner.Powers.Contains(data.NativeMarker);
    }

    public sealed override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        Data data = GetInternalData<Data>();
        if (Kind != FormStanceKind.Divinity || player != Owner.Player || !data.Applied
            || !IsCurrentEntry(Owner) || CombatManager.Instance.IsOverOrEnding
            || player.PlayerCombatState == null
            || player.PlayerCombatState.TurnNumber <= data.EnteredTurnNumber)
            return;
        // Never directly remove the carrier: native ExitStance dispatches all Watcher consumers.
        await StanceCmd.Exit(choiceContext, player, null);
    }

    public sealed override async Task AfterRemoved(Creature oldOwner)
    {
        Data data = GetInternalData<Data>();
        if (data.Detaching)
            return;
        data.Detaching = true;
        await RemoveEffects(oldOwner, data);
        // An explicit removal of the carrier must not leave an untracked native marker behind.
        // Normal native removal has already removed that marker, so this cannot recurse.
        if (!CombatManager.Instance.IsOverOrEnding && oldOwner.Player != null && !oldOwner.IsDead
            && data.NativeMarker != null && oldOwner.Powers.Contains(data.NativeMarker)
            && FormStanceMode.IsEnabled(oldOwner.Player))
            await FormStanceWatcherBridge.Exit(oldOwner.Player);
    }

    private static async Task RemoveEffects(Creature owner, Data data)
    {
        // Keep exact instance ownership: effects are instanced, and another entry may exist by now.
        PowerModel? first = data.FirstEffect;
        PowerModel? second = data.SecondEffect;
        data.FirstEffect = null;
        data.SecondEffect = null;
        try
        {
            if (first != null && owner.Powers.Contains(first))
                await PowerCmd.Remove(first);
        }
        finally
        {
            if (second != null && owner.Powers.Contains(second))
                await PowerCmd.Remove(second);
        }
    }
}
