using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Powers;

public sealed class WrathPower : StancePower
{
    public override PowerType Type => PowerType.Buff;

    public override string StanceName => "Wrath";

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Wrath",
            "#You deal and receive double damage.",
            "You deal and receive double damage.");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // StS1 gate (javap com.megacrit.cardcrawl.stances.WrathStance, authoritative jar):
        // both atDamageGive and atDamageReceive only multiply when
        // DamageType == NORMAL, returning the amount unchanged otherwise. HP_LOSS and
        // THORNS therefore pass through untouched in either direction.
        // StS2 equivalent: props.IsPoweredAttack() (ValueProp.Move without Unpowered),
        // the same gate the shipped VulnerablePower/DoubleDamagePower use.
        if (!props.IsPoweredAttack())
        {
            return 1m;
        }

        decimal multiplier = 1m;
        if (dealer == Owner)
        {
            multiplier *= 2m;
        }
        if (target == Owner)
        {
            multiplier *= 2m;
        }
        return multiplier;
    }
}
