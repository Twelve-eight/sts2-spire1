using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Forgotten Altar.
/// Offer: Golden Idol: give up the Golden Idol for the Bloody Idol, which takes over the Golden
/// Idol's inventory slot. Offered only while the Golden Idol is owned.
/// Sacrifice: lose round(25% of Max HP) (35% at Ascension 15+) to gain 5 Max HP.
/// Desecrate: gain the Decay curse.
///
/// StS1 constants: HP_LOSS_PERCENT = 0.25 (A_2: 0.35), MAX_HP_GAIN = 5.
/// </summary>
public class ForgottenAltar : Spire1Event
{
    private const string _hpLossKey = "HpLoss";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "abyssal_baths";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_hpLossKey, 0)];

    public override void CalculateVars()
    {
        // StS1: MathUtils.round(maxHealth * (Ascension >= 15 ? 0.35f : 0.25f)) — round half up.
        decimal loss = Owner.Creature.MaxHp * (Owner.RunState.AscensionLevel >= 15 ? 0.35m : 0.25m);
        DynamicVars[_hpLossKey].BaseValue = (int)System.Math.Round(loss, System.MidpointRounding.AwayFromZero);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options = [];
        // StS1 ForgottenAltar.<init> writes the idol choice into dialog slot 0 either way: OPTIONS[0]
        // enabled when hasRelic("Golden Idol"), OPTIONS[1] disabled when not. So it always precedes
        // [Sacrifice] (slot 1) and [Desecrate] (slot 2), and it is never hidden outright.
        // Both variants get `new BloodyIdol()` as the option's relic preview, so both carry its tip.
        if (Owner.GetRelic<GoldenIdol>() != null)
        {
            options.Add(Option(OfferIdol, HoverTipFactory.FromRelic<BloodyIdol>()));
        }
        else
        {
            // LockedOption takes `params IHoverTip[]` after the pageKey, so FromRelic's
            // IEnumerable<IHoverTip> has to be materialised and placed last.
            options.Add(LockedOption("OFFER_IDOL_LOCKED", "INITIAL", HoverTipFactory.FromRelic<BloodyIdol>().ToArray()));
        }

        options.Add(Option(Sacrifice).ThatDoesDamage(DynamicVars[_hpLossKey].BaseValue));
        options.Add(Option(Desecrate, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Decay>().ToArray()));
        return options;
    }

    private async Task OfferIdol()
    {
        // StS1 ForgottenAltar.gainChalice(). The two paths are deliberately NOT symmetric:
        //   * Bloody Idol already owned -> spawnRelicAndObtain(Circlet) and nothing else. That path
        //     never calls onUnequip and never touches player.relics, so the Golden Idol is KEPT.
        //     (Its logMetricRelicSwap call is misleading: no swap actually happens.)
        //   * otherwise -> golden.onUnequip(), then bloody.instantObtain(player, idx, false), whose
        //     bytecode is `player.relics.set(idx, bloody)` — the Bloody Idol takes over the Golden
        //     Idol's inventory slot. RelicCmd.Replace is precisely that: Remove(original) then
        //     Obtain(replace, player, indexOfOriginal) (RelicCmd.cs:74-82).
        // Neither path touches HP or gold: buttonEffect only plays HEAL_1 and shows DIALOG_2.
        // Circlet is StS2's shipped stackable RelicRarity.None consolation relic, reused as-is.
        if (Owner.GetRelic<BloodyIdol>() != null)
        {
            await RelicCmd.Obtain<Circlet>(Owner);
        }
        else
        {
            // Non-null: this option is only offered when the Golden Idol is owned.
            await RelicCmd.Replace(Owner.GetRelic<GoldenIdol>()!, ModelDb.Relic<BloodyIdol>().ToMutable());
        }

        SetEventFinished(PageDescription("IDOL"));
    }

    private async Task Sacrifice()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, 5);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars[_hpLossKey].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("SACRIFICE"));
    }

    private async Task Desecrate()
    {
        await CardPileCmd.AddCurseToDeck<Decay>(Owner);
        SetEventFinished(PageDescription("DESECRATE"));
    }
}
