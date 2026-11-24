using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Random;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(Spire1LegacyPool))]
public class Chaos() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/defect/chaos.png";

    public override string PortraitPath => "res://images/atlases/card_atlas.sprites/defect/chaos.tres";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Channeling)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        bool upgraded = IsUpgraded;
        var rng = Owner.RunState.Rng.CombatOrbGeneration;
        // StS1 Chaos.use @0-37 selects every orb before any ChannelAction executes.
        OrbModel first = CreateRandomOrb(rng);
        OrbModel? second = upgraded ? CreateRandomOrb(rng) : null;

        await OrbCmd.Channel(choiceContext, first, Owner);
        if (second != null)
            await OrbCmd.Channel(choiceContext, second, Owner);
    }

    private static OrbModel CreateRandomOrb(Rng rng) => rng.NextInt(0, 4) switch
    {
        0 => ModelDb.Orb<DarkOrb>().ToMutable(),
        1 => ModelDb.Orb<FrostOrb>().ToMutable(),
        2 => ModelDb.Orb<LightningOrb>().ToMutable(),
        _ => ModelDb.Orb<PlasmaOrb>().ToMutable()
    };

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
