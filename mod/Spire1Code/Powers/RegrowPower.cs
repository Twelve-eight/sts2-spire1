using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.RegrowPower</c> — Darkling's marker buff.
/// <para>
/// Display-only in vanilla too: the half-death/revive mechanic lives in
/// <c>Darkling.damage()</c> (halfDead latch), <c>getMove()</c> (REINCARNATE) and the
/// REINCARNATE takeTurn case (HealAction maxHealth/2 + ChangeState REVIVE), and the power is
/// re-applied there after each revive. This port mirrors that split: the behaviour is in
/// <see cref="Spire1.Spire1Code.Monsters.Darkling"/>, this type only shows the buff.
/// </para>
/// </summary>
public sealed class RegrowPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Regrow",
            "If this creature is Half Dead, it will revive with 50% of its Max HP.",
            "This creature will revive when Half Dead.");
}
