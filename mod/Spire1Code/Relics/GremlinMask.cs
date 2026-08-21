using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Gremlin Visage (event relic, from FaceTrader). DOWNSIDE relic: start each combat with 1 Weak on
/// the PLAYER, not on enemies.
///
/// StS1 (face-relics-and-madness.json "GremlinMask"): atBattleStart() -> flash + ApplyPowerAction(player,
/// player, WeakPower(player, 1, false), 1). Every creature argument is the player — target AND source AND the
/// WeakPower owner — with amount 1 and isSourceMonster false. There is no loop over monsters, unlike RedMask.
///
/// StS2 port: mirror shipped RedMask (RedMask.cs:23-30) but retarget from combatState.HittableEnemies to the
/// owner. Applying a debuff to one's own owner is shipped precedent — FakeSneckoEye.cs:38 applies
/// ConfusedPower to itself exactly this way. The BeforeSideTurnStart + TurnNumber &lt;= 1 guard is the shipped
/// "start each combat" idiom; a relic obtained mid-combat (TurnNumber already &gt; 1) waits for the next combat,
/// matching StS1 where atBattleStart never re-fires.
/// </summary>
public class GremlinMask : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Gremlin Visage",
            "#Start each combat with !WeakPower! *Weak*.",
            "Time to run.");

    // Same hook shape as RedMask.cs:23-30, same TurnNumber <= 1 latch (PlayerCombatState.cs:37; the combat
    // manager only increments TurnNumber after the side's turn ends), retargeted from the enemies to the
    // owner: PowerCmd.Apply<WeakPower>(ctx, owner, 1, owner, null) — the single-Creature overload at
    // PowerCmd.cs:71, mirroring FakeSneckoEye.cs:38.
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState.TurnNumber <= 1)
        {
            Flash();
            await PowerCmd.Apply<WeakPower>(
                choiceContext, Owner.Creature, DynamicVars.Weak.BaseValue, Owner.Creature, null);
        }
    }
}
