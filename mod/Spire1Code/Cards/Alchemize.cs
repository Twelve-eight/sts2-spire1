using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Potions;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Alchemize (Rare Skill). Obtain a random potion (0 cost upgraded). Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Alchemize() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    /// <summary>
    /// 原版排除机制(jar 权威):`green.Alchemize` 构造器写入 `CardTags.HEALING`
    /// (javap: getfield tags + getstatic AbstractCard$CardTags.HEALING + ArrayList.add),
    /// 而 StS1 的战斗内随机生成(`AbstractDungeon.returnTrulyRandomCardInCombat`)对
    /// common/uncommon/rare 三池一律 `hasTag(HEALING)` 过滤 —— 带该 tag 的卡不会被
    /// 战斗内随机生成.引擎侧对应物是 CardModel.CanBeGeneratedInCombat(默认 true),
    /// 引擎自带 Alchemize 亦覆写为 false.两者结论一致,故此处显式关闭.
    /// 注:本卡移植的是 green(Silent)Alchemize;blue(Defect)同名类不带该 tag.
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// Content gates (SolAlchemizeCompleteness contract, 2026-11-16): the card itself needs the Cards
    /// group; the legacy pool contains the mod's own potions (Potions group), Essence of Steel applies
    /// the mod's Plated Armor power, and SmokeBomb requires the real escape helper patches. The whole
    /// card is gated before any RNG is consumed instead of rolling a row and then log-returning; the
    /// 33-row pool is not trimmed. Only these boolean dependencies are read here.
    /// </summary>
    protected override bool IsPlayable =>
        Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards)
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Potions)
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)
        && LegacyCombatEscape.EnsureInstalled();

    /// <summary>
    /// StS1 `green/Alchemize::use @0-15`: the potion is rolled by
    /// `AbstractDungeon.returnRandomPotion(true)` at play time (potionRng) and then handed to
    /// `ObtainPotionAction`. The host's hand-off command is <see cref="PotionCmd.TryToProcure"/>;
    /// this card never writes a potion slot itself and never retries a failed grant.
    ///
    /// The play ownership and the captured owner/combat references are invariants and fail loudly.
    /// A combat that ended or an owner that died before the draw is an explicit cancel (no RNG is
    /// consumed). Content/helper dependencies are re-asserted before the draw because a bypassing
    /// auto-play or hook must not roll an unusable or inert potion. After the one procure call there
    /// is no decorative "dropped grant" branch: success is committed, TooFull/NotAllowed are the only
    /// normal refusals, and an unknown reason or exception propagates so the card transaction fails
    /// instead of silently consuming a play.
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        Player owner = Owner;
        // Snapshot this card's real owner context before validating the incoming CardPlay. A wrong
        // play.Card/play.Player must not erase the cleanup context, and cleanup always targets this
        // card (never the erroneous play.Card). These reads are cheap and do not allocate a lease.
        PlayerCombatState? playerCombatState = owner.PlayerCombatState;
        ICombatState? combatState = owner.Creature.CombatState;
        IRunState runState = owner.RunState;
        CombatId? combatId = CombatManager.Instance.CurrentCombatId;

        try
        {
            if (!ReferenceEquals(play.Card, this) || !ReferenceEquals(play.Player, owner))
            {
                throw new InvalidOperationException(
                    "Alchemize was played through a CardPlay that does not belong to this card and owner.");
            }

            if (playerCombatState is null)
            {
                throw new InvalidOperationException("Alchemize's owner has no PlayerCombatState.");
            }
            if (combatState is null)
            {
                throw new InvalidOperationException("Alchemize's owner has no combat state.");
            }
            if (!ReferenceEquals(owner.Creature.CombatState, combatState)
                || !ReferenceEquals(play.Player.Creature.CombatState, combatState)
                || !ReferenceEquals(play.Player.PlayerCombatState, playerCombatState))
            {
                throw new InvalidOperationException("Alchemize's CardPlay owner/combat references are inconsistent.");
            }

            if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards)
                || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Potions)
                || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)
                || !LegacyCombatEscape.EnsureInstalled())
            {
                throw new InvalidOperationException(
                    "Alchemize was played while its Cards/Potions/Powers dependencies or the SmokeBomb escape helper are unavailable; CanPlay must block it.");
            }

            if (combatId is null
                || !CombatManager.Instance.IsCurrentLiveCombat(combatId)
                || !combatState.ContainsCreature(owner.Creature)
                || owner.Creature.IsDead)
            {
                MainFile.Logger.Info("[Spire1] Alchemize canceled: the owner's combat is no longer live before the draw");
                return;
            }

            LegacyAlchemizeEntry selection = LegacyAlchemizePool.Select(runState.Rng.CombatPotionGeneration);
            PotionModel potion = LegacyAlchemizePool.Resolve(selection);
            PotionProcureResult result = await PotionCmd.TryToProcure(potion, owner);

            if (result.success)
            {
                return;
            }

            switch (result.failureReason)
            {
                case PotionProcureFailureReason.TooFull:
                case PotionProcureFailureReason.NotAllowed:
                    MainFile.Logger.Info(
                        $"[Spire1] Alchemize procure refused normally: {potion.Id.Entry} ({result.failureReason})");
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Alchemize procure failed with an unknown reason {result.failureReason}; the card transaction must fail.");
            }
        }
        catch (Exception original)
        {
            bool sameCombat = combatId is not null
                && CombatManager.Instance.IsCurrentLiveCombat(combatId)
                && playerCombatState is not null
                && combatState is not null
                && ReferenceEquals(owner.RunState, runState)
                && ReferenceEquals(owner, Owner)
                && ReferenceEquals(owner.PlayerCombatState, playerCombatState)
                && ReferenceEquals(owner.Creature.CombatState, combatState);
            if (sameCombat && Pile is { Type: PileType.Play })
            {
                try
                {
                    await MoveToResultPileWithoutPlaying(choiceContext);
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(original, cleanupFailure);
                }
            }
            throw;
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
