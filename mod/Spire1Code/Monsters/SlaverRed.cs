using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — Red Slaver (<c>com.megacrit.cardcrawl.monsters.exordium.SlaverRed</c>).
/// <para>
/// Bytecode: HP 46-50, A2 48-52; STAB_DMG 13 (A2 14), SCRAPE_DMG 8 (A2 9), VULN_AMT 1.
/// getMove: first turn always STAB; r&gt;=75 &amp;&amp; !usedEntangle -&gt; ENTANGLE (once per combat);
/// r&lt;55 &amp;&amp; usedEntangle &amp;&amp; !lastTwo(STAB) -&gt; STAB; else SCRAPE unless lastTwo(SCRAPE) -&gt; STAB.
/// takeTurn ENTANGLE = EntanglePower on the player (cannot play Attacks next turn);
/// SCRAPE = attack(scrape) + Vulnerable(VULN_AMT) [A17: +1].
/// </para>
/// <para>
/// Entangle maps to the engine's shipped <see cref="TangledPower"/> debuff: it afflicts every
/// Attack card in the player's deck with the shipped <c>Entangled</c> affliction, which raises
/// the card's energy cost by 1 — StS2's own translation of "can't play Attacks", used by the
/// shipped Tangled/Entangled content. No custom power is created.
/// </para>
/// </summary>
public sealed class SlaverRed : Spire1Monster
{
    // setHp(46, 50); ascension >= 7 -> setHp(48, 52)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 50);

    // STAB_DMG = 13; ascension >= 2 -> 14
    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

    // SCRAPE_DMG = 8; ascension >= 2 -> 9
    private int ScrapeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    // VULN_AMT = 1 (A17 applies VULN_AMT+1 = 2)
    private int VulnerableAmount => 1; // vanilla A17 tier (2) unreachable in StS2; base value kept.

    private bool _usedEntangle;

    protected override string DonorId => "stabbot";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState stab = new("STAB_MOVE", StabMove, new SingleAttackIntent(StabDamage));
        MoveState entangle = new("ENTANGLE_MOVE", EntangleMove, new DebuffIntent());
        MoveState scrape = new("SCRAPE_MOVE", ScrapeMove, new SingleAttackIntent(ScrapeDamage), new DebuffIntent());
        ConditionalBranchState branch = new("SLAVER_BRANCH");
        stab.FollowUpState = branch;
        entangle.FollowUpState = branch;
        scrape.FollowUpState = branch;
        // getMove priority chain (slaverred.txt), evaluated in vanilla order:
        branch.AddState(stab, () =>
        {
            // first turn always STAB
            return !_everMoved;
        });
        branch.AddState(entangle, () =>
        {
            // roll >= 75 && !usedEntangle: a 25% check every turn from turn 2 on
            return _everMoved && !_usedEntangle && RollHundred() >= 75;
        });
        branch.AddState(stab, () =>
        {
            // roll >= 55 && usedEntangle && !lastTwoMoves(STAB)
            return _usedEntangle && _stabRun < 2 && RollHundred() >= 55;
        });
        branch.AddState(scrape, () =>
        {
            // base game: !lastTwoMoves(SCRAPE) -> SCRAPE (two scrapes in a row allowed);
            // the A17+ single lastMove guard is unreachable in StS2 (max A10) and dropped.
            return _scrapeRun < 2;
        });
        branch.AddState(stab, () => true);
        return new MonsterMoveStateMachine([stab, entangle, scrape, branch], stab);
    }


    // One stable 0-99 draw per move selection (vanilla passes the same aiRng roll through the
    // whole getMove chain); cached per round so multiple predicates see one value.
    private int? _roll;
    private int _rollTurn = -1;
    private int RollHundred()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_roll == null || _rollTurn != turn)
        {
            _roll = base.Rng.NextInt(100);
            _rollTurn = turn;
        }
        return _roll.Value;
    }

    private bool _everMoved;

    private int _scrapeRun;

    private int _stabRun;

    private async Task StabMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        _scrapeRun = 0;
        _stabRun++;
        await DamageCmd.Attack(StabDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task EntangleMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        _scrapeRun = 0;
        _stabRun = 0;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<TangledPower>(new ThrowingPlayerChoiceContext(), targets, 1m, base.Creature, null);
        _usedEntangle = true;
    }

    private async Task ScrapeMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        _scrapeRun++;
        _stabRun = 0;
        await DamageCmd.Attack(ScrapeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, VulnerableAmount, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Red Slaver",
        [
            ("STAB_MOVE", "Stab"),
            ("ENTANGLE_MOVE", "Entangle"),
            ("SCRAPE_MOVE", "Scrape"),
        ]);
}
