using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — Bite (SPECIAL Attack). Deal 7 damage, heal 2 HP (8 damage / 3 HP upgraded). 1 cost.
/// Granted only by the Vampires event, which replaces every Strike with five of these.
///
/// Verified against the jar bytecode (com.megacrit.cardcrawl.cards.colorless.Bite): cost 1, baseDamage 7,
/// baseMagicNumber 2, upgradeDamage(1) + upgradeMagicNumber(1), no exhaust/ethereal flags; use() queues a
/// DamageAction followed by HealAction(player, player, magicNumber), so the heal is unconditional and does
/// not depend on the damage landing.
///
/// StS1 also tags the card CardTags.HEALING. StS2's CardTag enum
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Entities.Cards/CardTag.cs) only has Strike/Defend/Minion/OstyAttack/Shiv,
/// and nothing in the shipped content reads a "healing" tag, so the behaviour-less tag is simply dropped.
///
/// StS1's SPECIAL rarity (never offered in card rewards, only handed out by events) maps onto StS2's
/// CardRarity.Ancient + EventCardPool: that is exactly how the shipped Apparition — the other StS1 colorless
/// SPECIAL card — is registered (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.CardPools/EventCardPool.cs:24).
/// ColorlessCardPool would instead expose the card to Toolbox and the colorless shop, which StS1 never does.
/// </summary>
[Pool(typeof(EventCardPool))]
public class Bite() : Spire1Card(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new HealVar(2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}
