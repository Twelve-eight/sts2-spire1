using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Echo Form: the first Amount ordinary card plays each turn are echoed as purge copies.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class EchoForm() : Spire1Card(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<Spire1EchoFormPower>("EchoForm", 1)];

    public override string CustomPortraitPath => "res://images/packed/card_portraits/defect/echo_form.png";

    public override string PortraitPath => "res://images/atlases/card_atlas.sprites/defect/echo_form.tres";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
            return;

        Spire1EchoReplay.RequireInstalled();
        await CommonActions.ApplySelf<Spire1EchoFormPower>(choiceContext, this, DynamicVars["EchoForm"].BaseValue);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
