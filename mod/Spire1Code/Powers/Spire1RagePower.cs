using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.RagePower</c> - "Whenever you play an Attack this turn,
/// gain <see cref="PowerModel.Amount"/> Block."
/// <para>
/// Owned replacement for the shipped <c>MegaCrit.Sts2.Core.Models.Powers.RagePower</c>. The shipped
/// power grants block in <c>AfterCardPlayed</c> with the live amount for every Attack, which drifts
/// from the jar-sourced old queue in three reviewed places:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Order. Old <c>AbstractPlayer.useCard</c> runs <c>card.use()</c> (which queues the card's own
/// actions), then <c>UseCardAction</c>'s constructor queues this power's
/// <c>GainBlockAction</c> at the bottom, so ordinary Attacks resolve their damage first
/// (<c>J/cards/Strike_Red.txt:92-106</c>, <c>J/actions/GameActionManager.txt:167-178</c>,
/// <c>J/powers/RagePower.txt:55-72</c>). Whirlwind is the exception: <c>WhirlwindAction</c> is
/// queued by <c>use()</c> and only appends its hits when it updates, i.e. behind the block that was
/// already queued, so the block lands before the expanded damage
/// (<c>J/cards/Whirlwind.txt:30-46</c>, <c>J/actions/unique/WhirlwindAction.txt:46-69,109-135</c>).
/// </description></item>
/// <item><description>
/// Amount freeze. The old hook captures <c>amount</c> when the play is queued, so a later change to
/// the power does not change the promised grant (<c>J/powers/RagePower.txt:55-72</c>).
/// </description></item>
/// <item><description>
/// Expiry. Old <c>atEndOfTurn(true)</c> queues <c>RemoveSpecificPowerAction</c>, which removes the
/// then-current instance of the power, not a stale captured one
/// (<c>J/actions/common/RemoveSpecificPowerAction.txt:29-46</c>).
/// </description></item>
/// </list>
/// <para>
/// The capture shape is the shipped <c>AfterimagePower</c> / this mod's
/// <see cref="HeatsinksPower"/> shape: freeze in <c>BeforeCardPlayed</c>, consume once in
/// <c>AfterCardPlayed</c> keyed by the physical card. Only a real Attack <c>CardPlay</c> owned by
/// this power's player triggers anything - thorns, HP loss, poison and every other damage source
/// never do, and a play whose card owner is not the playing player is rejected.
/// </para>
/// <para>
/// Boundaries (not certified by the candidate and deliberately not fixed here): a nested re-entry of
/// the same physical card is unsupported (last capture wins, one grant); if this instance is removed
/// during the same ordinary play the frozen grant is dropped with it; a host ending combat consumes
/// nothing because <c>AfterCardPlayed</c> is not called when combat is no longer in progress and
/// <c>CreatureCmd.GainBlock</c> returns early under <c>IsOverOrEnding</c>; the escaping-owner
/// mapping of the old dead-or-escaped guard is not reproduced.
/// </para>
/// </summary>
public sealed class Spire1RagePower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse the shipped canonical power art; this mod authors no separate power icon.
    public override string CustomPackedIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.RagePower>().PackedIconPath;

    public override string CustomBigIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.RagePower>().ResolvedBigIconPath;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override List<(string, string)>? Localization => new PowerLoc(
        "Rage",
        "#Whenever you play an Attack this turn, gain {Amount} *Block*.",
        "Whenever you play an Attack, gain Block.");

    private sealed class Data
    {
        /// <summary>
        /// Ordinary-Attack grants frozen at play start, keyed by the physical card being played. The
        /// entry is consumed by exactly one <c>AfterCardPlayed</c>; an expanded-X play never writes
        /// one because it is paid before the card's damage instead.
        /// </summary>
        public readonly Dictionary<CardModel, int> FrozenBlockForPlay = new();
    }

    protected override object InitInternalData() => new Data();

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!IsOwnAttackPlay(cardPlay))
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        if (IsExpandedXAttack(cardPlay.Card))
        {
            // Whirlwind lane: the old expansion appends its hits behind the block that was queued at
            // play start, so the grant is paid here, before OnPlay deals any damage. One grant per
            // physical play regardless of the resolved hit count, and no frozen entry is written, so
            // AfterCardPlayed cannot pay this play twice. Drop any stale entry for the same card.
            GetInternalData<Data>().FrozenBlockForPlay.Remove(cardPlay.Card);
            int frozenXBlock = Amount;
            if (frozenXBlock <= 0 || Owner.IsDead)
            {
                return;
            }
            Flash();
            await CreatureCmd.GainBlock(Owner, frozenXBlock, ValueProp.Unpowered, null);
            return;
        }
        // Ordinary lane: freeze the amount promised when the play started; the damage resolves before
        // this grant, and a later Amount change must not alter it.
        GetInternalData<Data>().FrozenBlockForPlay[cardPlay.Card] = Amount;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!IsOwnAttackPlay(cardPlay))
        {
            return;
        }
        if (!GetInternalData<Data>().FrozenBlockForPlay.Remove(cardPlay.Card, out int frozenBlock))
        {
            return;
        }
        if (frozenBlock <= 0 || Owner.IsDead)
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner, frozenBlock, ValueProp.Unpowered, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        // "This turn": drop anything frozen by an unfinished play before the instance goes away, then
        // remove the then-current instance by type instead of this captured reference.
        GetInternalData<Data>().FrozenBlockForPlay.Clear();
        await PowerCmd.Remove<Spire1RagePower>(Owner);
    }

    /// <summary>
    /// Exact owner isolation: the played card must be an Attack, owned by this power's creature, and
    /// played by this power's player. Enemy plays, other players' plays and borrowed plays (card
    /// owner != playing player, which the candidate rejected) never qualify.
    /// </summary>
    private bool IsOwnAttackPlay(CardPlay cardPlay) =>
        cardPlay.Card.Type == CardType.Attack &&
        ReferenceEquals(cardPlay.Card.Owner.Creature, Owner) &&
        ReferenceEquals(cardPlay.Player, Owner.Player);

    /// <summary>
    /// The one reviewed expansion carrier is Whirlwind-shaped: an X-cost Attack that hits all
    /// enemies, whose old <c>WhirlwindAction</c> appended its hits after the already-queued block.
    /// Single-target X Attacks (Skewer) queue their hits inside <c>use()</c> and keep the ordinary
    /// after-damage order, so they are deliberately excluded.
    /// </summary>
    private static bool IsExpandedXAttack(CardModel card) =>
        card.EnergyCost.CostsX && card.TargetType == TargetType.AllEnemies;
}
