using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect - Electrodynamics (Rare Power, cost 2, Self). While the power is attached,
/// Lightning passive and evoke damage hits ALL enemies; on play, Channel 2 Lightning
/// (3 upgraded).
/// <para>
/// StS1 FACT: the original card's <c>hasPower("Electrodynamics")</c> guard can never match
/// <c>ElectroPower</c> (its ID is "Electro"), so every play schedules the power application
/// and then the channels. This port keeps that behaviour instead of "fixing" the guard:
/// reapplication is a no-op through the Single stack type plus the received-amount clamp in
/// <see cref="ElectrodynamicsAmount"/>, and applying the power first keeps the channel
/// resolves ordered after it exactly like the StS1 action queue.
/// </para>
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class Electrodynamics() : Spire1Card(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Orbs", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // The all-enemies rewrite is a Harmony patch installed before content registration.
        // A missing patch set must fail loudly here rather than silently shipping channels
        // without the Lightning behaviour the card text promises.
        ElectrodynamicsLightning.RequireInstalled();
        int count = DynamicVars["Orbs"].IntValue;
        if (ElectrodynamicsLightning.ContentEnabled)
        {
            await PowerCmd.Apply<ElectrodynamicsPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
        for (int i = 0; i < count; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Orbs"].UpgradeValueBy(1m);
}
