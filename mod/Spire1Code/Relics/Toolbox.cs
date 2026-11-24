using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 - Toolbox (Shop). At the start of combat, choose 1 of 3 random Colorless cards to add to your hand.</summary>
public class Toolbox : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        var playerCombatState = player.PlayerCombatState;
        if (player != Owner || playerCombatState?.TurnNumber != 1)
            return;

        var manager = CombatManager.Instance;
        CombatId? combatId = manager.CurrentCombatId;
        if (!manager.IsCurrentLiveCombat(combatId) || manager.IsEnding || Owner.Creature.CombatState != combatState)
            return;

        // Keep the existing Cards content gate and Colorless pool; do not construct cards while gated off.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Toolbox card grant skipped: cards content group is off");
            return;
        }

        var pool = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);
        var cards = CardFactory.GetDistinctForCombat(Owner, pool, DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (cards.Count == 0)
            return;

        Flash();
        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, Owner, canSkip: false);

        // A choice can resume after combat ends or after the owner's combat state is replaced.
        if (chosen == null || !manager.IsCurrentLiveCombat(combatId) || manager.IsEnding
            || Owner.Creature.CombatState != combatState
            || !ReferenceEquals(Owner.PlayerCombatState, playerCombatState)
            || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
            return;

        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
    }
}
