using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Nightmare (Rare Skill). Choose a card; next turn add 3 copies of it into your hand (2 cost upgraded). Exhaust. Uses an authored delayed power so only the Spire1 card receives the lifecycle guards.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Nightmare() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NightmarePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate: the delayed power adds cards directly to a combat pile and
        // therefore bypasses pool filtering. The card still pays and exhausts when this returns.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Nightmare delayed copy grant skipped: cards content group is off");
            return;
        }
        // Keep this gate before selection and application. The authored power is Spire1-gated;
        // the membership check below also rejects a gate-produced unattached phantom.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        var selected = await CommonActions.SelectSingleCard(this, SelectionScreenPrompt, choiceContext, PileType.Hand);
        if (selected != null)
        {
            var power = await PowerCmd.Apply<LegacyNightmarePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Power<NightmarePower>().BaseValue,
                Owner.Creature,
                this);
            if (power is null || !Owner.Creature.Powers.Contains(power))
            {
                MainFile.Logger.Warn("[Spire1] Nightmare delayed copy grant skipped: power was not mounted");
                return;
            }
            power.SetSelectedCard(selected);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
