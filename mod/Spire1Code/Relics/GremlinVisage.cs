using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - Gremlin Visage (event face relic; StS1 class <c>com.megacrit.cardcrawl.relics.GremlinMask</c>,
/// RelicTier.SPECIAL, LandingSound.CLINK).
/// <para>
/// DOWNSIDE relic: at the start of every combat the PLAYER gets 1 Weak. The decompiled
/// <c>atBattleStart()</c> passes <c>AbstractDungeon.player</c> as target, source AND WeakPower owner,
/// with amount iconst_1, isSourceMonster iconst_0 and ApplyPowerAction stack amount iconst_1. There is
/// no loop over monsters - it is not a RedMask clone.
/// </para>
/// </summary>
public class GremlinVisage : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    // Localization is JSON-only on purpose: this relic does not override Localization, so the text
    // lives only in localization/{eng,zhs}/relics.json (SPIRE1-GREMLIN_VISAGE.title/.description/.flavor).
    // A C# RelicLoc is a single (English) string and BaseLib's ModelLocPatch postfix on ModelDb.Init
    // writes it into the table of the language active at startup (3.4.8 Patches/Localization/ModelLocPatch.cs:34-57);
    // that would replace the zhs JSON text until the next SetLanguage reload.
    //
    // StS1's sprite files use the StS1 class/id name (GremlinMask -> gremlin_mask*), not this model's
    // entry (gremlin_visage*), so the three sprite slots point at the shipped StS1-named art.
    public override string PackedIconPath => "gremlin_mask.png".RelicImagePath();

    protected override string PackedIconOutlinePath => "gremlin_mask_outline.png".RelicImagePath();

    protected override string BigIconPath => "gremlin_mask.png".BigRelicImagePath();

    // Hook shape copied from the shipped RedMask (At the start of combat, apply Weak to enemies),
    // retargeted from combatState.HittableEnemies to base.Owner.Creature: same participants guard,
    // same TurnNumber <= 1 first-turn gate, same single-Creature PowerCmd.Apply overload RedMask's
    // cousin FakeSneckoEye uses to debuff its own owner.
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner.Creature))
            return;
        if (base.Owner.PlayerCombatState.TurnNumber > 1)
            return;

        Flash();
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars["WeakPower"].BaseValue,
            base.Owner.Creature,
            null);
    }
}
