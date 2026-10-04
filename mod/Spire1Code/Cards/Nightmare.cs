using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Nightmare (Rare Skill). Choose a card; next turn add 3 copies of it into your hand (2 cost upgraded). Exhaust. Reuses the game's NightmarePower.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Nightmare() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NightmarePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate (C18, 2026-10-03): the engine NightmarePower clones the stored card in
        // BeforeHandDraw and adds it with AddGeneratedCardToCombat, bypassing the pool filters that carry
        // the cards gate. Fail closed before the selection/power is created; the card itself stays played
        // and exhausts normally, so the play cost is not refunded.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Nightmare delayed copy grant skipped: cards content group is off");
            return;
        }
        // C12 r5: NightmarePower is the vanilla power that performs the delayed grant; with the
        // powers group off the card must not open the selection screen at all.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        var selected = await CommonActions.SelectSingleCard(this, SelectionScreenPrompt, choiceContext, PileType.Hand);
        if (selected != null)
        {
            var power = await CommonActions.ApplySelf<NightmarePower>(choiceContext, this);
            power?.SetSelectedCard(selected);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
