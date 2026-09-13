using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Limit Break (Uncommon Skill). Double your Strength. Exhaust (removed when upgraded).</summary>
public class LimitBreak() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var str = Owner.Creature.GetPowerAmount<StrengthPower>();
        if (str != 0)
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, str, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // SP1-2 fix (astra-advice 2026-09-12): the upgrade only removes Exhaust.
        // The old ResetKeywordCache() nulled CardModel._keywords, wiping keywords
        // other systems added (Retain/Ethereal survive upgrade in StS1/StS2).
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
