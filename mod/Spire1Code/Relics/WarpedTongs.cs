using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Warped Tongs (event relic, from the Accursed Blacksmith / Ominous Forge "Rummage" branch).
/// At the start of your turn, Upgrade a random card in your hand for the rest of combat.
///
/// StS1 (relics.json "WarpedTongs", no numeric constants): atTurnStartPostDraw() flashes and queues an
/// UpgradeRandomCardAction, which upgrades one randomly chosen upgradable card in the player's hand. Because it
/// upgrades the in-combat card instance, the upgrade lasts only for the rest of that combat.
///
/// StS2 port:
/// * Hook — AfterPlayerTurnStart (AbstractModel.cs:1320) is literally atTurnStartPostDraw: CombatManager runs
///   CardPileCmd.Draw at CombatManager.cs:924 and only then fires Hook.AfterPlayerTurnStart at CombatManager.cs:926.
/// * Eligibility — CardModel.IsUpgradable (CardModel.cs:786-796, "CurrentUpgradeLevel < MaxUpgradeLevel") is the
///   engine's own version of StS1's `!upgraded && canUpgrade()`; curses/statuses opt out via MaxUpgradeLevel.
///   CardCmd.Upgrade re-checks it anyway (CardCmd.cs:275-278), so an ineligible pick would silently do nothing —
///   hence the filter before the roll, so the roll only ever picks a card that will really upgrade.
/// * Randomness — Rng.CombatCardSelection, the stream every shipped in-combat card pick uses
///   (Bookmark.cs:30, JeweledMask.cs:32, MummifiedHand.cs:30, StoneCracker.cs:25).
/// * "For the rest of combat" is automatic, not a deviation: Player.PopulateCombatState (Player.cs:806-815) clones
///   every deck card into the draw pile and only points the clone's DeckVersion back at the deck card, so the hand
///   holds combat-scoped clones. CardCmd.Upgrade also skips its deck bookkeeping and deck VFX for any pile that is
///   not PileType.Deck (CardCmd.cs:279-283, 290-314). Upgrading a hand card therefore cannot touch the run deck —
///   the same guarantee shipped Armaments relies on (Armaments.cs:27-38).
/// </summary>
public class WarpedTongs : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Warped Tongs",
            "#At the start of your turn, Upgrade a random card in your hand for the rest of combat.",
            "The cursed tongs emit a strong desire to return to where they were stolen from.");

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;
        CardModel? pick = Owner.RunState.Rng.CombatCardSelection.NextItem(
            PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable));
        if (pick == null)
            return Task.CompletedTask;
        Flash();
        CardCmd.Upgrade(pick);
        return Task.CompletedTask;
    }
}
