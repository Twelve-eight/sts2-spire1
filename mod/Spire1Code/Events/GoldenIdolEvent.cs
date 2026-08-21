using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Golden Idol. Taking the idol from the pedestal grants the Golden Idol relic and springs a
/// boulder trap; the player then escapes by taking an Injury curse, taking damage, or losing Max HP.
/// </summary>
public class GoldenIdolEvent : Spire1Event
{
    private const float _hpLossPercent = 0.25f;

    private const float _a15HpLossPercent = 0.35f;

    private const float _maxHpLossPercent = 0.08f;

    private const float _a15MaxHpLossPercent = 0.1f;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "sunken_statue";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", 0),
        new IntVar("MaxHpLoss", 0),
    ];

    public override void CalculateVars()
    {
        int maxHp = Owner.Creature.MaxHp;
        bool ascension15 = Owner.RunState.AscensionLevel >= 15;
        // StS1: damage = (int)(maxHealth * 0.25f) — 0.35f at Ascension 15+.
        // maxHpLoss = max(1, (int)(maxHealth * 0.08f)) — 0.1f at Ascension 15+.
        DynamicVars["Damage"].BaseValue = (int)(maxHp * (ascension15 ? _a15HpLossPercent : _hpLossPercent));
        DynamicVars["MaxHpLoss"].BaseValue = Math.Max(1, (int)(maxHp * (ascension15 ? _a15MaxHpLossPercent : _maxHpLossPercent)));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 GoldenIdolEvent.<init>: setDialogOption(OPTIONS[0], new GoldenIdol()) — the [Take]
        // option previews the relic it awards. The preview is unconditionally the Golden Idol; the
        // Circlet substitution happens only at grant time, inside buttonEffect.
        return
        [
            Option(TakeTheIdol, HoverTipFactory.FromRelic<GoldenIdol>()),
            Option(Leave),
        ];
    }

    private async Task TakeTheIdol()
    {
        // StS1 GoldenIdolEvent.buttonEffect, screenNum 0 / option 0: the relic lands the instant the
        // idol is taken. spawnRelicAndObtain runs in THIS branch, before the three boulder escapes are
        // ever offered, so the reward is already banked whichever escape the player then picks — it is
        // not a payout for surviving the trap.
        // The relic is `hasRelic("Golden Idol") ? Circlet : Golden Idol`; StS2 ships Circlet as the
        // stackable RelicRarity.None consolation relic, so it is reused rather than reimplemented.
        if (Owner.GetRelic<GoldenIdol>() != null)
        {
            await RelicCmd.Obtain<Circlet>(Owner);
        }
        else
        {
            await RelicCmd.Obtain<GoldenIdol>(Owner);
        }

        SetEventState(PageDescription("BOULDER"),
        [
            Option(Outrun, HoverTipFactory.FromCardWithCardHoverTips<Injury>(), "BOULDER"),
            Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
            Option(Hide, "BOULDER").ThatDecreasesMaxHp(DynamicVars["MaxHpLoss"].BaseValue),
        ]);
    }

    private async Task Outrun()
    {
        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
        SetEventFinished(PageDescription("CHOSE_RUN"));
    }

    private async Task Smash()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["Damage"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("CHOSE_FIGHT"));
    }

    private async Task Hide()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue, isFromCard: false);
        SetEventFinished(PageDescription("CHOSE_FLAT"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("IGNORE"));
    }
}
