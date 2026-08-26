using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — Ritual Dagger (SPECIAL Attack). Deal 15 damage; if Fatal, permanently increase this card's
/// damage by 3 (5 upgraded) for the rest of the run. Exhaust. 1 cost.
/// Granted only by The Nest event (mod/Spire1Code/Events/Nest.cs).
///
/// Numbers verified against the jar bytecode (com.megacrit.cardcrawl.cards.colorless.RitualDagger): the
/// constructor sets misc = 15, baseMagicNumber = 3 and baseDamage = misc, i.e. the base damage is 15 — NOT the
/// 3 that research/sts1data/cards-colorless.json reports (that extraction mistook the magic number for the
/// damage). upgrade() only calls upgradeMagicNumber(2); the damage never changes on upgrade.
///
/// The growth is StS1's RitualDaggerAction: on a kill (target dying / at 0 HP, not halfDead, and without the
/// "Minion" power) it adds magicNumber to `misc` on the master-deck copy AND on every in-battle instance
/// sharing the card's uuid, then re-derives baseDamage from misc — so the buff survives the combat and the
/// rest of the run.
///
/// StS2 equivalent, verified against the shipped card that does exactly this
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Cards/GeneticAlgorithm.cs): a pair of [SavedProperty] ints on the
/// card model are serialized with the run save, the setter pushes the value into DynamicVars.Damage, and the
/// played combat clone forwards the buff to its DeckVersion (the run-deck card it was cloned from). The extra
/// pass over the other in-combat clones mirrors StS1's GetAllInBattleInstances loop and follows the same
/// DeckVersion matching the mod already uses in Cards/LessonLearned.cs.
///
/// StS1's "Minion" exclusion is StS2's PowerModel.ShouldOwnerDeathTriggerFatal() (MinionPower is the power that
/// returns false). The predicate below matches the shipped Feed
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Cards/Feed.cs:38): the default return value is true, so a Fatal
/// effect fires when EVERY power on the target allows it. (2026-08-26) Feed.cs and LessonLearned.cs
/// now both use the correct non-negated predicate — LessonLearned was fixed in 3cfbcf1, Feed in the
/// reverify fix batch.
///
/// SPECIAL rarity maps to CardRarity.Ancient + EventCardPool, matching the shipped Apparition
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.CardPools/EventCardPool.cs:24).
/// </summary>
[Pool(typeof(EventCardPool))]
public class RitualDagger : Spire1Card
{
    private const int _startingDamage = 15;

    private int _currentDamage = _startingDamage;
    private int _increasedDamage;

    public RitualDagger() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>Damage this copy currently deals: 15 plus everything it has earned from kills this run.</summary>
    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = value;
        }
    }

    /// <summary>Total damage earned from kills, kept separately so a downgrade can rebuild CurrentDamage.</summary>
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(CurrentDamage, ValueProp.Move),
        new IntVar("Increase", 3),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Fatal)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target ?? throw new ArgumentNullException(nameof(play));

        // Fatal only counts if every power on the target agrees the death should trigger Fatal effects
        // (MinionPower is the notable power that refuses).
        bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        if (!shouldTriggerFatal || !attack.Results.SelectMany(hit => hit).Any(r => r.WasTargetKilled))
            return;

        BuffEveryInstance(DynamicVars["Increase"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Increase"].UpgradeValueBy(2m);

    protected override void AfterDowngraded() => UpdateDamage();

    /// <summary>
    /// Applies the permanent buff to the played card, to the run-deck card it came from, and to every other
    /// in-combat clone of that same deck card — StS1's master-deck plus GetAllInBattleInstances behaviour.
    /// </summary>
    private void BuffEveryInstance(int increase)
    {
        BuffFromKill(increase);

        CardModel? deckVersion = DeckVersion;
        (deckVersion as RitualDagger)?.BuffFromKill(increase);

        var combatState = Owner.PlayerCombatState;
        if (deckVersion == null || combatState == null)
            return;

        foreach (CardModel copy in combatState.AllCards.ToList())
        {
            if (copy != this && copy.DeckVersion == deckVersion && copy is RitualDagger clone)
                clone.BuffFromKill(increase);
        }
    }

    private void BuffFromKill(int increase)
    {
        IncreasedDamage += increase;
        UpdateDamage();
    }

    private void UpdateDamage() => CurrentDamage = _startingDamage + IncreasedDamage;
}
