using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Entry resources only. The stance carrier owns the next-turn exit and notifications.
/// Native Divinity's entry energy is suppressed by integration, not subtracted here.
/// </summary>
public sealed class CelestialFormPower : CustomPowerModel
{
    private sealed class Data
    {
        public bool granted;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Celestial Form",
            "#Immediately gain Energy and draw cards equal to the round number, at least 3.",
            "Gain Energy and draw cards on entry.");

    protected override object InitInternalData() => new Data();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        if (data.granted || Owner.Player == null || Owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        data.granted = true;
        int amount = Math.Max(Owner.CombatState.RoundNumber, 3);
        Flash();
        await PlayerCmd.GainEnergy(amount, Owner.Player);
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), amount, Owner.Player);
    }
}