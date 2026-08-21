using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Mutagenic Strength (event relic, from The Augmenter). Start each combat with 3 Strength, then lose
/// 3 Strength at the end of your first turn.
///
/// StS1 (relics.json "MutagenicStrength", STR_AMT = 3): atBattleStart() adds an ApplyPowerAction of
/// StrengthPower(player, 3) plus an ApplyPowerAction of LoseStrengthPower(player, 3); LoseStrengthPower strips the
/// 3 Strength at the end of the player's first turn, so the net effect is +3 Strength for turn 1 only, every combat.
///
/// StS2 port: the +3 goes on in BeforeCombatStart (AbstractModel.cs:498), the shipped idiom for a relic that applies
/// a power at combat start (SneckoEye.cs:33, BeltBuckle.cs:47). The -3 goes off in AfterSideTurnEnd
/// (AbstractModel.cs:1406), which is the same hook StS2's own TemporaryStrengthPower uses to undo its Strength
/// (TemporaryStrengthPower.cs:140-148), i.e. exactly LoseStrengthPower's timing.
///
/// "First turn only" is latched on PlayerCombatState.TurnNumber rather than a counter field: it starts at 1
/// (PlayerCombatState.cs:37), lives on the per-combat PlayerCombatState (rebuilt by Player.ResetCombatState,
/// Player.cs:797) so it resets itself every combat, and CombatManager only increments it in SwitchSides
/// (CombatManager.cs:1882) *after* Hook.AfterSideTurnEnd has run (CombatManager.cs:1774 runs inside
/// EndPlayerTurnPhaseTwoInternal, which CombatManager.cs:1717-1718 completes before SwitchFromPlayerToEnemySide).
/// So the player's first turn end is observed with TurnNumber still == 1. Extra turns increment it too
/// (CombatManager.cs:1871-1883), so the relic never fires twice.
/// </summary>
public class MutagenicStrength : Spire1Relic
{
    /// <summary>
    /// True between "this combat's +3 Strength was granted" and "it was taken back".
    /// Deliberately a plain per-combat field and NOT a [SavedProperty]: it is re-armed by BeforeCombatStart at the
    /// top of every combat, and it exists only so a relic obtained mid-combat (RelicModel grants can land during
    /// combat, cf. BeltBuckle.cs:41-45) never subtracts Strength it never granted — matching StS1, where a relic
    /// picked up after atBattleStart applies no LoseStrengthPower either.
    /// </summary>
    private bool _strengthGranted;

    /// <summary>
    /// Writes go through AssertMutable() so that a stray write on the CANONICAL model throws
    /// CanonicalModelException instead of silently corrupting state shared by every player's clone
    /// (AbstractModel.MutableClone is MemberwiseClone, AbstractModel.cs:159-187). Shipped relics with
    /// per-combat latches do exactly this — see ThrowingAxe.cs:11-21.
    /// </summary>
    private bool StrengthGranted
    {
        get => _strengthGranted;
        set
        {
            AssertMutable();
            _strengthGranted = value;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Mutagenic Strength",
            "#Start each combat with !StrengthPower! Strength. At the end of your first turn, lose !StrengthPower! Strength.",
            "\"The results seem fleeting, triggering when the subject is in danger.\" - Unknown");

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
        StrengthGranted = true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // participants is the side that just ended its turn, so this also filters out enemy turn ends.
        if (!StrengthGranted || !participants.Contains(Owner.Creature))
            return;
        if (Owner.PlayerCombatState?.TurnNumber != 1)
            return;
        StrengthGranted = false;
        Flash();
        // StrengthPower.AllowNegative is true (StrengthPower.cs:12), so this is a real -3, exactly like
        // LoseStrengthPower, and can push the player below 0 Strength if they started the combat debuffed.
        await PowerCmd.Apply<StrengthPower>(
            choiceContext, Owner.Creature, -DynamicVars.Strength.BaseValue, Owner.Creature, null);
    }
}
