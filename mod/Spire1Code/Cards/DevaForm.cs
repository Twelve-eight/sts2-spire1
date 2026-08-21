using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Deva Form (Rare Power). Ethereal (removed on upgrade). At the start of your turn, gain Energy and
/// increase that gain by 1 every turn.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class DevaForm() : Spire1Card(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DevaFormPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<DevaFormPower>(choiceContext, this);

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
