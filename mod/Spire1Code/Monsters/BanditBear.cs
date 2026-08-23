using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
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
/// StS1 The City — BanditBear (<c>com.megacrit.cardcrawl.monsters.city.BanditBear</c>;
/// 官方中文名「熊」). Part of the Masked Bandits trio (Leader + Pointy + Bear) referenced by
/// the StS1 event — NOT a regular encounter spawn (Main-agent bytecode audit 2026-08).
/// <para>
/// Bytecode: HP 38-42, A7 40-44; MAUL 18 (A2 20, BLUNT_HEAVY); LUNGE 9 + GainBlock(9)
/// (no ascension variants); BEAR_HUG Dexterity <c>con_reduction</c> -2 (A17 -4).
/// getMove seeds byte 2 (BEAR_HUG); takeTurn chains HUG -> SetMove(LUNGE),
/// MAUL -> SetMove(LUNGE), LUNGE -> SetMove(MAUL): so the fight is
/// HUG -> MAUL -> LUNGE -> MAUL -> LUNGE ... forever.
/// </para>
/// <para>
/// Ascension mapping: A7 HP -> ToughEnemies; A2 damage -> DeadlyEnemies; the A17
/// hug tier (-4) maps onto DeadlyEnemies like GremlinFat's A17 Frail gate (nearest higher
/// difficulty tier StS2 exposes). Cosmetic spine Hit/MAUL animation juggling and the
/// die() loop poking allies' deathReact are not ported (visual chatter only; the engine
/// drives its own Hit reactions).
/// </para>
/// <para>
/// Art: donor rig <c>brute_ruby_raider</c> — the biggest shipped brawler humanoid
/// (BruteRubyRaider, HP tier 30-34, club swings), visually the closest stand-in for a
/// hulking bandit muscleman. Its rig keeps the default idle_loop/cast/attack/hurt/die
/// tracks, so no animator remap is needed.
/// </para>
/// </summary>
public sealed class BanditBear : Spire1Monster
{
    // setHp(38, 42); ascension >= 7 -> setHp(40, 44)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

    // maulDmg = 18; ascension >= 2 -> 20
    private int MaulDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

    // lungeDmg = 9 / LUNGE_DEFENSE = 9 (no ascension variants)
    private const int LungeDamage = 9;

    private const int LungeBlock = 9;

    // con_reduction = -2; ascension >= 17 -> -4 (mapped onto DeadlyEnemies, see remarks)
    private int ConReduction => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, -4, -2);

    protected override string DonorId => "brute_ruby_raider";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Fixed chain: BEAR_HUG -> MAUL -> LUNGE -> MAUL -> LUNGE ...
        MoveState hug = new("BEAR_HUG", HugMove, new DebuffIntent(strong: true));
        MoveState maul = new("MAUL_MOVE", MaulMove, new SingleAttackIntent(MaulDamage));
        MoveState lunge = new("LUNGE_MOVE", LungeMove, new SingleAttackIntent(LungeDamage), new DefendIntent());
        hug.FollowUpState = maul;
        maul.FollowUpState = lunge;
        lunge.FollowUpState = maul;
        return new MonsterMoveStateMachine([hug, maul, lunge], hug);
    }

    private async Task HugMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), targets, ConReduction, base.Creature, null);
    }

    private async Task MaulMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MaulDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(null);
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(LungeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, LungeBlock, ValueProp.Move, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Bear",
        [
            ("BEAR_HUG", "Bear Hug"),
            ("MAUL_MOVE", "Maul"),
            ("LUNGE_MOVE", "Lunge"),
        ]);
}
