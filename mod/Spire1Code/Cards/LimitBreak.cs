using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Limit Break (Uncommon Skill). Double your Strength. Exhaust (removed when upgraded).</summary>
public class LimitBreak() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    private bool _exhaust = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => _exhaust ? [CardKeyword.Exhaust] : [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var str = Owner.Creature.GetPowerAmount<StrengthPower>();
        if (str != 0)
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, str, Owner.Creature, this);
    }

    protected override void OnUpgrade() => _exhaust = false;
}
