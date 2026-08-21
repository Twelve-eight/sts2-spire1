using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — J.A.X. (SPECIAL Skill). Lose 3 HP, gain 2 Strength (3 upgraded). 0 cost.
/// Granted only by the Augmenter event (mod/Spire1Code/Events/DrugDealer.cs).
///
/// Verified against the jar bytecode (com.megacrit.cardcrawl.cards.colorless.JAX): cost 0, baseMagicNumber 2,
/// upgradeMagicNumber(1), no flags; use() queues LoseHPAction(player, player, 3) — a hard-coded 3, unrelated
/// to the magic number — followed by ApplyPowerAction(StrengthPower, magicNumber).
///
/// StS1's LoseHPAction is unblockable, unbuffable self damage; the mod already models that as
/// CreatureCmd.Damage with Unblockable|Unpowered|Move (see Cards/Bloodletting.cs, the other "Lose 3 HP" card).
///
/// The class name is deliberately the bare acronym: the analyzer splits at every case boundary, so JAX yields
/// the localization key SPIRE1-J_A_X (same rule that turns FTL into SPIRE1-F_T_L).
///
/// SPECIAL rarity maps to CardRarity.Ancient + EventCardPool, matching the shipped Apparition
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.CardPools/EventCardPool.cs:24).
/// </summary>
[Pool(typeof(EventCardPool))]
public class JAX() : Spire1Card(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(3),
        new PowerVar<StrengthPower>(2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, play);
        await CommonActions.ApplySelf<StrengthPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Power<StrengthPower>().UpgradeValueBy(1m);
}
