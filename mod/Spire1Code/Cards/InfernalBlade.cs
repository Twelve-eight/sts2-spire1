using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Infernal Blade (Uncommon Skill). Add a random Attack to your hand; it costs 0 this turn. Exhaust (0 cost upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class InfernalBlade() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate (C18 r2, 2026-10-03): CommonActions.GenerateSingleCard is invoked directly by this Spire1 card and is not filtered by the card pool gate.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Infernal Blade card grant skipped: cards content group is off");
            return;
        }

        var attack = CommonActions.GenerateSingleCard(this, c => c.Type == CardType.Attack);
        if (attack != null)
        {
            var result = await CardPileCmd.AddGeneratedCardToCombat(attack, PileType.Hand, Owner);
            result.cardAdded.EnergyCost.SetThisTurn(0);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
