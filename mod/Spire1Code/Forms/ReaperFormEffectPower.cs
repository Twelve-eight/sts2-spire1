using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// "Reaper form" half of the Demon-Reaper stance. Direct port of the shipped ReaperFormPower
/// (research engine-dllsrc ReaperFormPower.cs): whenever the owner (or their pet, e.g. Osty) lands a
/// powered attack that deals &gt; 0 total damage, apply Doom to the target equal to the damage dealt.
/// Trigger judgement is confirmed in MECH-reaperform-doom-20260926 (IsPoweredAttack + dealer in
/// {player, player pet} + TotalDamage &gt; 0). Amount is 1 so Doom == damage dealt.
/// </summary>
public sealed class ReaperFormEffectPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Reaper Form",
            "#Attacks inflict *Doom* equal to damage dealt.",
            "Attacks inflict Doom equal to damage dealt.");

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != null
            && (dealer == Owner || dealer.PetOwner?.Creature == Owner)
            && props.IsPoweredAttack()
            && result.TotalDamage > 0)
        {
            Flash();
            await PowerCmd.Apply<DoomPower>(choiceContext, target, result.TotalDamage, Owner, null);
        }
    }
}
