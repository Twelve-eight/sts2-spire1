using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
/// StS1 Exordium — Cultist (<c>com.megacrit.cardcrawl.monsters.exordium.Cultist</c>).
/// <para>
/// Bytecode: HP_MIN 48 / HP_MAX 54, A2 50/56; RITUAL_AMT 3, A_2_RITUAL_AMT 4; ATTACK_DMG 6.
/// getMove: first turn always INCANTATION (BUFF), then DARK_STRIKE forever.
/// Mirrors the shipped <c>DampCultist</c> structure with vanilla numbers.
/// </para>
/// </summary>
public sealed class Cultist : Spire1Monster
{
    // setHp(48, 54); ascension >= 7 -> setHp(50, 56)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 56, 54);

    // ATTACK_DMG = 6 (no ascension variant)
    private int DarkStrikeDamage => 6;

    // RITUAL_AMT = 3; ascension >= 2 -> 4
    private int IncantationAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    protected override string DonorId => "damp_cultist";

    // Donor DampCultist overrides SetupSkins (builds a composite skin from its "slug" part),
    // so the borrowed scene needs the same call or it renders unskinned.
    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton)
    {
        MegaSkin megaSkin = spine.NewSkin("custom-skin");
        MegaSkeletonDataResource data = skeleton.GetData();
        megaSkin.AddSkin(data.FindSkin("slug"));
        skeleton.SetSkin(megaSkin);
        skeleton.SetSlotsToSetupPose();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // getMove: firstMove -> INCANTATION, else DARK_STRIKE every turn.
        MoveState incantation = new("INCANTATION_MOVE", IncantationMove, new BuffIntent());
        MoveState darkStrike = new("DARK_STRIKE_MOVE", DarkStrikeMove, new SingleAttackIntent(DarkStrikeDamage));
        incantation.FollowUpState = darkStrike;
        darkStrike.FollowUpState = darkStrike;
        return new MonsterMoveStateMachine([incantation, darkStrike], incantation);
    }

    private async Task IncantationMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.45f);
        await Cmd.CustomScaledWait(0.25f, 0.5f);
        await PowerCmd.Apply<RitualPower>(new ThrowingPlayerChoiceContext(), base.Creature, IncantationAmount, base.Creature, null);
    }

    private async Task DarkStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DarkStrikeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Cultist",
        [
            ("INCANTATION_MOVE", "Incantation"),
            ("DARK_STRIKE_MOVE", "Dark Strike"),
        ]);
}
