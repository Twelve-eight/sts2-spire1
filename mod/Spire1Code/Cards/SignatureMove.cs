using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Signature Move (Uncommon Attack). Playable only while it is the only Attack in your hand;
/// deal 30 damage (40 upgraded). Playability shape from the mod's Clash.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class SignatureMove() : Spire1Card(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30, ValueProp.Move)];

    // Same hand-refresh hot path as Clash.cs: read the hand's backing List directly instead of paying a
    // params PileType[] plus a SelectMany enumerator per CanPlay evaluation.
    protected override bool IsPlayable =>
        !PileType.Hand.GetPile(Owner).Cards.Any(c => c != this && c.Type == CardType.Attack);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10m);
}
