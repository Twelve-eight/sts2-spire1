using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 FiendFireAction: capture hand size once, randomly exhaust from the live hand N times,
/// then execute N independent DamageActions against the original target. Only FNP/DE's queued
/// payments wait until after those actions; all other exhaust listeners run at each real exhaust.
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class FiendFire() : Spire1Card(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedHitCountVar("CalculatedHits").WithMultiplier(static (card, _) =>
        {
            var hand = PileType.Hand.GetPile(card.Owner).Cards;
            return hand.Count - (hand.Contains(card) ? 1 : 0);
        })
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<string> ExtraRunAssetPaths => NGroundFireVfx.AssetPaths;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        using var transaction = new Spire1ExhaustTransaction(this, play, choiceContext);
        int count = transaction.Hand.Cards.Count;
        for (int i = 0; i < count; i++)
        {
            CardModel? card = transaction.PickFromLiveHand();
            if (card == null) break;
            await transaction.Exhaust(card);
        }

        float scale = 0.8f;
        await transaction.Attack(count, () =>
        {
            // Keep the shipped fire effect, preload and per-hit growth on the independent hits.
            var vfx = NGroundFireVfx.Create(transaction.Target);
            if (vfx == null) return Task.CompletedTask;
            SfxCmd.Play("event:/sfx/characters/attack_fire");
            vfx.Scale = Vector2.One * scale;
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
            scale += 0.1f;
            return Task.CompletedTask;
        });
        await transaction.Complete();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
