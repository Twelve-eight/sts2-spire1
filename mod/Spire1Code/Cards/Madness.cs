using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — Madness (Uncommon Skill). Reduce the cost of a random card in your hand to 0 this combat. Exhaust.
/// 1 cost, 0 upgraded (upgradeBaseCost(0) — no other change, hence no UPGRADE_DESCRIPTION in the loc table).
///
/// Selection rule reproduced from the jar bytecode (com.megacrit.cardcrawl.actions.unique.MadnessAction, verified via
/// javap -p -c): update() first scans p.hand.group setting hasCostForTurn=true when a card has costForTurn &gt; 0,
/// otherwise hasCost=true when that card has cost &gt; 0; if either flag is set it calls findAndModifyCard(hasCostForTurn),
/// which draws p.hand.getRandomCard(AbstractDungeon.cardRandomRng) and RECURSES until it draws a card passing the same
/// test (safe because the scan already proved one qualifies). When no card qualifies the action just ticks — a no-op.
/// The winner gets cost = 0, costForTurn = 0 and isCostModified = true, persisting for the rest of the combat.
///
/// StS2 mapping (per the sts2_api_risk instruction in research/sts1data/face-relics-and-madness.json):
/// * cardRandomRng → RunState.Rng.CombatCardSelection (RunRngType.cs:10, RunRngSet.cs:62), the stream every shipped
///   in-combat card pick uses (Thrash.cs:50, TrueGrit.cs:40, Bookmark.cs:30).
/// * StS1's two cost fields collapse into one resolved cost: EnergyCost.GetResolved() (CardEnergyCost.cs:155-162) is
///   Max(0, GetWithModifiers(CostModifiers.All)) for non-X cards — the current playable cost including every local and
///   global modifier — standing in for the costForTurn-then-cost precedence. Cards already free (resolved 0) are
///   excluded so the pick never wastes itself.
/// * SetThisCombat(0) (CardEnergyCost.cs:238) is the combat-scoped absolute modifier — the exact scope StS1's persistent
///   `cost` zeroing needs. SetToFreeThisTurn → SetThisTurnOrUntilPlayed (CardModel.cs:1267-1271, CardEnergyCost.cs:197)
///   is the WRONG scope (expires WhenPlayed|EndOfTurn). SetToFreeThisCombat (CardModel.cs:1273-1276) adds an extra
///   SetStarCostThisCombat(0) (CardModel.cs:1274) that StS1's Madness never does, so the granular SetThisCombat(0) is used.
/// * No-qualifier case: Rng.NextItem returns null for an empty sequence (Rng.cs:296-298), so no qualifying hand card
///   means a clean no-op, matching MadnessAction's silent tickDuration() exit.
/// * FLAG: StS1 gold-flashes the picked card (superFlash(Color.GOLD)); CardModel exposes no Flash API (grep "Flash"
///   over .tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs returns nothing), so the reduction applies without a
///   card flash — the cost display still updates through the normal EnergyCostChanged path.
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public class Madness() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // X-cost cards never qualify in StS1 (cost == -1 fails both &gt; 0 tests). The explicit CostsX guard mirrors
        // shipped BulletTime.cs:21 and mod BulletTime.cs:14: without it, a previously played X-cost card returned to
        // hand could carry CapturedXValue &gt; 0 (GetResolved() reads it, CardEnergyCost.cs:155-162) and be picked, yet
        // GetWithModifiers returns _base early for CostsX (CardEnergyCost.cs:105-108), so SetThisCombat would be inert.
        CardModel? pick = Owner.RunState.Rng.CombatCardSelection.NextItem(
            PileType.Hand.GetPile(Owner).Cards.Where(c => !c.EnergyCost.CostsX && c.EnergyCost.GetResolved() > 0));
        if (pick == null)
            return Task.CompletedTask;
        pick.EnergyCost.SetThisCombat(0);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // vanilla Madness+ costs 0 (upgradeBaseCost(0)); -1 clamps at 0
}
