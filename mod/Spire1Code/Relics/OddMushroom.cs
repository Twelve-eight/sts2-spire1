using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Odd Mushroom (event relic, reward of the Mushrooms / Mushroom Lair fight). While Vulnerable, take
/// 25% more attack damage instead of 50%.
///
/// StS1 (relics.json "OddMushroom", VULN_EFFECTIVENESS = 1.25f, EFFECTIVENESS_STRING = 25): the relic overrides no
/// hook at all. VulnerablePower.atDamageReceive does the work — for NORMAL damage, when the power's owner is the
/// player and the player holds Odd Mushroom, it returns damage * 1.25f early instead of the usual 1.5x.
///
/// StS2 has no such opening. .tmp/dllsrc/MegaCrit.Sts2.Core.Models.Powers/VulnerablePower.cs:26-56 hard-codes the
/// only three things allowed to move its multiplier — PaperPhrog (via GetRelic&lt;PaperPhrog&gt;()), CrueltyPower and
/// DebilitatePower (via GetPower&lt;T&gt;()) — and each of those exposes its own non-virtual ModifyVulnerableMultiplier
/// (PaperPhrog.cs:17, CrueltyPower.cs:17, DebilitatePower.cs:26); PaperPhrog is `sealed`. A mod relic cannot register
/// there, so the reduction has to be emulated from the relic's own ModifyDamageMultiplicative
/// (AbstractModel.cs:1613), which Hook.ModifyDamageInternal (Hook.cs:2536-2547) folds into the same running product
/// as VulnerablePower's own factor.
/// </summary>
public class OddMushroom : Spire1Relic
{
    /// <summary>
    /// One unit in the last place of a decimal, i.e. the smallest value System.Decimal can represent (scale 28).
    /// See <see cref="ModifyDamageMultiplicative" /> for why the cancelling factor is nudged up by exactly this much.
    /// </summary>
    private const decimal _oneUlp = 1e-28m;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Odd Mushroom",
            "#When Vulnerable, take 25% more attack damage rather than 50%.",
            "\"After consuming trichella parastius I felt larger and less... susceptible.\" - Ranwid ");

    /// <summary>
    /// Cancels half of the Vulnerable damage bonus on its holder.
    ///
    /// Derivation — every number below is read out of the engine, none is guessed:
    /// * VulnerablePower.cs:29 declares CanonicalVars = { DynamicVar("DamageIncrease", 1.5m) }, and
    ///   VulnerablePower.cs:41 starts from exactly that BaseValue, so the real Vulnerable multiplier is 1.5.
    ///   VulnerablePower.cs:42-56 may then raise it (PaperPhrog +0.25, CrueltyPower +Amount/100,
    ///   DebilitatePower doubles the bonus), so instead of hard-coding 1.5 this asks the live power for the value it
    ///   is about to contribute — the same call Hook.ModifyDamageInternal will make (Hook.cs:2540). It is a pure
    ///   read of DynamicVars plus relic/power lookups, so calling it here has no side effects.
    /// * Hook.cs:2540-2541 multiplies each listener's factor into the running damage, so the two factors compose:
    ///   total = live * ours.
    /// * StS1's atDamageReceive is an EARLY RETURN, not an adjustment: for NORMAL damage on a player who holds the
    ///   relic it returns `damage * 1.25f` and ignores every other Vulnerable modifier. So the target multiplier is
    ///   a flat 1.25 regardless of what else is live:
    ///       ours = 1.25 / live               -> with live = 1.5 this is 5/6
    ///   Halving the live bonus instead (1 + (live-1)/2) was considered and REJECTED: that formula exists in neither
    ///   game, so it would be invented behavior, and it would leave the holder taking 1.5x under Debilitate while
    ///   the relic's own text promises "25% more ... rather than 50%".
    ///
    /// The +_oneUlp is a truncation guard, not a fudge factor. 5/6 has no exact decimal form: 1.25m / 1.5m rounds
    /// DOWN to 0.8333333333333333333333333333, so a 4-damage Vulnerable hit computes 4 * 1.5 * that
    /// = 4.9999999999999999999999999998, and Creature.LoseHpInternal casts the result with (int) (Creature.cs:450),
    /// which truncates — 4 damage where StS1 deals 5. Adding one ulp makes the factor the smallest decimal that is
    /// >= the exact ratio, so 6m * 0.8333333333333333333333333334 = 5.0000000000000000000000000004 truncates to 5.
    /// The introduced excess is at most damage * live * 1e-28 (below 1e-18 for any damage the engine can hold, which
    /// LoseHpInternal clamps to 999999999), while the smallest gap between a true product and the next integer is
    /// 1/6 here (and >= 0.001 for any Cruelty/Debilitate-inflated multiplier), so it can never round a hit up.
    ///
    /// Both halves of that argument were checked numerically against an exact 28-decimal-digit emulation of this
    /// pipeline (round-half-away division, then a single truncation), not just reasoned about:
    ///  * without the nudge the result is one short of StS1 for EXACTLY the damage values divisible by 4 — 500 of
    ///    the first 2000, the cases where damage * 1.25 lands on a whole number — and correct for all others;
    ///  * with the nudge it matches StS1's truncated flat 1.25 for every damage value from 1 to 2000, and never
    ///    exceeds it anywhere in 1..200000.
    ///
    /// Interaction with StS2's own Vulnerable amplifiers, stated plainly rather than flagged: because the target is a
    /// flat 1.25, an active CrueltyPower (on the attacking enemy) or DebilitatePower (on the holder) has its whole
    /// contribution cancelled — 1.25 / live exactly undoes it. That IS what StS1's early return does structurally,
    /// and neither power exists in StS1, so there is no vanilla behaviour being lost. PaperPhrog never overlaps at
    /// all: it returns early when the Vulnerable target is its own owner (PaperPhrog.cs:19-22), i.e. the holder.
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // Same two gates VulnerablePower itself applies (VulnerablePower.cs:33-40): only damage aimed at the holder,
        // and only a powered attack.
        if (target != Owner.Creature || !props.IsPoweredAttack())
            return 1m;
        VulnerablePower? vulnerable = Owner.Creature.GetPower<VulnerablePower>();
        if (vulnerable == null)
            return 1m;
        decimal live = vulnerable.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource, cardPlay);
        if (live <= 1m)
            return 1m;
        return 1.25m / live + _oneUlp;
    }

    /// <summary>
    /// Flash site. This runs only for models that actually changed the damage
    /// (Hook.cs:706-715 filters on the modifier list) and only on the real damage path
    /// (CreatureCmd.cs:283-284), never during intent/damage previews — which is why the flash lives here instead of
    /// in the multiplier hook. Same pattern as shipped SlowPower.cs:53 and IntangiblePower.cs:60.
    /// </summary>
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        return Task.CompletedTask;
    }
}
