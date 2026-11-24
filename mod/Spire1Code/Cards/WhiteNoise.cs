using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect - White Noise (Uncommon Skill). 1 cost, Exhaust: add a random Power card into
/// your hand; it costs 0 this turn. Upgrade: cost 0.
///
/// <para>
/// Authority (jar bytecode, not the wiki): <c>.tmp/sts1-jar-dump/cards/WhiteNoise.txt</c>
/// constructor = cost 1, SKILL/BLUE/UNCOMMON/CardTarget.NONE, <c>exhaust = true</c>;
/// <c>use @0-28</c> = <c>AbstractDungeon.returnTrulyRandomCardInCombat(CardType.POWER)</c> ->
/// <c>makeCopy()</c> -> <c>setCostForTurn(0)</c> -> <c>MakeTempCardInHandAction(card, true)</c>;
/// <c>upgrade @0-16</c> = <c>upgradeName()</c> + <c>upgradeBaseCost(0)</c>, Exhaust is kept.
/// </para>
///
/// <para>
/// Source domain: the legacy draw is the OWNED character's color Power pool
/// (<c>AbstractDungeon.initializeCardPools @45-110</c> fills the src* pools from
/// <c>player.getCardPool(...)</c>), minus <c>CardTags.HEALING</c> rows
/// (<c>AbstractDungeon.txt:2793-2878</c>). <see cref="LegacyPowerCatalog"/> carries the
/// complete four-character inventory (51 rows) so any profiled owner is served, and refuses
/// generation when any mapped row cannot be resolved. The shipped StS2 WhiteNoise instead
/// reads <c>Owner.Character.CardPool</c> (host pools, no legacy identity, no HEALING rule);
/// that is why this own class exists and why SharedCardReuse must treat WhiteNoise as a
/// field-drift twin.
/// </para>
///
/// <para>
/// Unlock: the legacy src pools are built by CardLibrary.addRed/Green/Blue/PurpleCards, which
/// filter <c>rarity != BASIC &amp;&amp; (!UnlockTracker.isCardLocked(cardID) ||
/// Settings.treatEverythingAsUnlocked())</c> (<c>CardLibrary.txt:2903-3078</c>). This mod ships no
/// legacy unlock tracker and authors every declared legacy card, so production is the
/// treatEverythingAsUnlocked branch: the declared non-HEALING inventory is the offered set, with
/// no live per-play unlock query (a missing canonical row is a HOLD, not a silent exclusion).
/// </para>
///
/// <para>
/// Pool placement follows the existing drift-twin shape (Alchemize, Chill, Reboot): the class
/// is parked in the sink <see cref="Spire1LegacyPool"/> and reaches the Defect character pool
/// only through SharedCardReuse's FieldDriftTwins resolution ("drift-own" in non-pure mode,
/// "pure-own" in PureSts1Pools mode). It must NOT also carry a DefectCardPool attribute:
/// ModHelper.ConcatModelsFromMods does not deduplicate, so attribute + twin injection would
/// list the same card twice in one pool and double its reward weight.
/// </para>
///
/// <para>
/// Turn cost: StS1 writes only <c>costForTurn</c> (<c>AbstractCard.setCostForTurn @0-14</c>:
/// write only while <c>costForTurn &gt;= 0</c>, clamp below 0) and that field survives a play,
/// so a returned-to-hand card stays free for the rest of the turn. The matching engine call is
/// <c>EnergyCost.SetThisTurn(0)</c> (EndOfTurn-only absolute modifier), the granular form the
/// production Infernal Blade (<c>Cards/InfernalBlade.cs:34</c>) and Bullet Time
/// (<c>Cards/BulletTime.cs:21</c>) already use. It is deliberately not
/// <c>SetToFreeThisTurn</c>/<c>SetThisTurnOrUntilPlayed</c>
/// (<c>CardEnergyCost.cs:197-203</c>): the WhenPlayed bit is the host native counterexample
/// proved for exactly this card by the whitenoise probe (W07 legacy loop return keeps 0, W08
/// native until-played pays 1 on the replay), and it is never a freeToPlayOnce flag. The call
/// is skipped for X/unplayable sentinels so a negative base is left untouched.
/// </para>
///
/// <para>
/// RNG: <c>RunState.Rng.CombatCardGeneration</c> through
/// <c>CardFactory.GetDistinctForCombat</c> (<c>CardFactory.cs:119-133</c>), the same host
/// stream the shipped WhiteNoise, Discovery and Creative AI use. The factory's own
/// <c>FilterForRun</c>/<c>FilterForCombat</c> predicates are checked first; if they would
/// shrink the declared legacy domain (multiplayer/Blitz constraints or a gate closing), the
/// play is refused instead of biasing the pool.
/// </para>
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public sealed class WhiteNoise() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// R8 (Main-approved, 2026-11-16) following the Alchemize precedent (Cards/Alchemize.cs:
    /// IsPlayable gates the content groups, OnPlay throws if a hook bypasses it, the pool is not
    /// trimmed): the granted card is authored Spire1 content and the whole effect sits behind the
    /// powers/cards groups, so with either group closed the card must not be playable. R1 (1131
    /// completion contract): the independent unavailable fuse and the source-owner check join the
    /// same gate, so an unsupported owner or an empty native host pool also blocks ordinary manual
    /// play instead of spending energy on an empty reward. The fuse stays a declared independent
    /// safety-fail state with its own OnPlay guard; supply removal belongs to the mod's filters.
    /// </summary>
    protected override bool IsPlayable =>
        !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards)
        && HasSourceForOwner();

    /// <summary>
    /// R1 contract: evaluated last, only after the fuse/groups short-circuit, on a mutable owned
    /// card. A legacy profile matches without forcing any lazy snapshot (the legacy resolution
    /// below still owns HOLD/no-shrink behavior); otherwise the exact native host pool must be
    /// non-empty. No array, hash set, RNG, card creation or grant here - this is polled by CanPlay.
    /// </summary>
    private bool HasSourceForOwner()
    {
        if (LegacyPowerCatalog.ForOwner(Owner.Character) is not null)
        {
            return true;
        }
        return LegacyPowerCatalog.UsesNativeHostPool(Owner.Character)
            && LegacyPowerCatalog.GetNativeHostPowerPool(Owner).Any();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Contract step 1: keep the independent fuse Warn+return as the first existing fail-safe.
        // The R1 native fallback never applies in this state; IsPlayable also blocks manual play.
        if (Spire1PowersGate.ContentUnavailableActive)
        {
            MainFile.Logger.Warn("[Spire1] White Noise card grant skipped: content unavailable fuse is active");
            return;
        }
        // Contract step 2 (R8, Main-approved): groups are a playability gate, not a silent no-op.
        // AutoPlay/wrapper bypasses do not consult CanPlay and must fail loudly here.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)
            || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            throw new InvalidOperationException(
                "WhiteNoise was played while the powers/cards content groups are disabled; CanPlay must block it.");
        }

        // Contract step 3: the play must belong to this exact owner and live combat before any
        // source lookup or RNG. A stale/missing context throws instead of granting elsewhere.
        if (!ReferenceEquals(play.Card, this) || !ReferenceEquals(play.Player, Owner))
        {
            throw new InvalidOperationException("WhiteNoise requires its exact CardPlay and owner.");
        }
        PlayerCombatState state = Owner.PlayerCombatState
            ?? throw new InvalidOperationException("WhiteNoise requires the owner's live PlayerCombatState.");
        ICombatState combatState = Owner.Creature.CombatState
            ?? throw new InvalidOperationException("WhiteNoise requires the owner's live combat state.");
        CombatId? combatId = CombatManager.Instance.CurrentCombatId;
        if (!CombatManager.Instance.IsCurrentLiveCombat(combatId)
            || !ReferenceEquals(Owner.PlayerCombatState, state)
            || !ReferenceEquals(Owner.Creature.CombatState, combatState)
            || !combatState.ContainsCreature(Owner.Creature))
        {
            throw new InvalidOperationException("WhiteNoise requires the exact live owner combat; the play context is stale.");
        }

        string ownerType = Owner.Character.GetType().FullName ?? "null";

        // Contract step 4: exact legacy profile first, never a native fallback for a legacy owner.
        // A supported native host owner materializes its real host pool exactly once.
        LegacyPowerProfile? profile = LegacyPowerCatalog.ForOwner(Owner.Character);
        bool nativeBranch = profile is null;
        CardModel[] offered;
        if (profile is not null)
        {
            if (!LegacyPowerCatalog.TryGetOfferedPool(profile, out offered, out string[] holds))
            {
                MainFile.Logger.Error(
                    $"[Spire1] White Noise source pool refused for {profile.Character}: {string.Join(",", holds)}; no generation.");
                return;
            }
        }
        else if (LegacyPowerCatalog.UsesNativeHostPool(Owner.Character))
        {
            offered = LegacyPowerCatalog.GetNativeHostPowerPool(Owner).ToArray();
            if (offered.Length == 0)
            {
                throw new InvalidOperationException(
                    $"WhiteNoise native host Power pool is empty for owner '{ownerType}'; no grant.");
            }
            HashSet<ModelId> nativeIds = new();
            foreach (CardModel card in offered)
            {
                if (!card.IsCanonical
                    || card.Type != CardType.Power
                    || !ReferenceEquals(ModelDb.GetByIdOrNull<CardModel>(card.Id), card))
                {
                    throw new InvalidOperationException(
                        $"WhiteNoise native host Power pool entry '{card.Id}' is not the canonical registered Power instance for owner '{ownerType}'.");
                }
                if (!nativeIds.Add(card.Id))
                {
                    throw new InvalidOperationException(
                        $"WhiteNoise native host Power pool has a conflicting canonical id '{card.Id}' for owner '{ownerType}'.");
                }
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"WhiteNoise has no source pool for owner type '{ownerType}': no exact legacy profile and no supported native host pool.");
        }

        // Contract step 5: the legacy branch keeps the full-domain no-shrink check; the native
        // branch already uses the host's allowed subset and must not compare to raw host rows.
        if (!nativeBranch
            && !CardFactory.FilterForCombat(offered.Where(card => card.IsAllowedInRun(Owner.RunState)))
                .SequenceEqual(offered, ReferenceEqualityComparer.Instance))
        {
            MainFile.Logger.Error(
                $"[Spire1] White Noise source pool refused for {profile!.Character}: host factory predicates would shrink the declared legacy domain; no generation.");
            return;
        }

        // Contract step 6: exactly one generation call, enumerated once to its first result.
        CardModel? generated = CardFactory.GetDistinctForCombat(
            Owner, offered, 1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (generated is null)
        {
            if (nativeBranch)
            {
                throw new InvalidOperationException(
                    $"WhiteNoise native host draw returned no card for owner '{ownerType}'; no grant.");
            }
            MainFile.Logger.Error(
                $"[Spire1] White Noise source pool refused for {profile!.Character}: actual factory output escaped the declared legacy domain; no grant.");
            return;
        }
        // Contract step 7: product identity/domain invariants. Native failures throw; the legacy
        // branch keeps its existing refusal (never widened into an accepted native outcome).
        bool productValid = generated.IsMutable
            && ReferenceEquals(generated.Owner, Owner)
            && generated.Pile is null
            && ReferenceEquals(generated.CombatState, combatState)
            && combatState.ContainsCard(generated)
            && offered.Any(card => card.Id == generated.Id && card.GetType() == generated.GetType());
        if (!productValid)
        {
            if (nativeBranch)
            {
                throw new InvalidOperationException(
                    $"WhiteNoise native host draw escaped the declared invariants for owner '{ownerType}'; no grant.");
            }
            MainFile.Logger.Error(
                $"[Spire1] White Noise source pool refused for {profile!.Character}: actual factory output escaped the declared legacy domain; no grant.");
            return;
        }

        // Contract step 8: branch-specific existing cost APIs. Legacy keeps the StS1 EndOfTurn-only
        // energy write; native uses the shipped WhiteNoise call so native energy and star costs
        // (for example NeutronAegis' 5 stars) follow host behavior.
        if (nativeBranch)
        {
            generated.SetToFreeThisTurn();
        }
        else if (!generated.EnergyCost.CostsX && generated.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
        {
            generated.EnergyCost.SetThisTurn(0);
        }

        // Contract step 9: hand overflow is host-owned discard routing; no retry or rollback.
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
