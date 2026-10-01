using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Forms;

/// <summary>Only the native custom-run tickbox list offers this modifier. No random pool registration.</summary>
public sealed class FormStanceModifier : CustomModifierModel
{
    public override ModifierAlignment Alignment => ModifierAlignment.None;

    protected override string IconPath => "res://Spire1/images/powers/divinity_power.png";

    protected override void AfterRunCreated(RunState runState) => FormStanceMode.RequireAvailable();

    protected override void AfterRunLoaded(RunState runState) => FormStanceMode.RequireAvailable();

    public override Task BeforeCombatStart()
    {
        FormStanceMode.RequireAvailable();
        return Task.CompletedTask;
    }
}
