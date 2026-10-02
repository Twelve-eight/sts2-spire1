using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Spire1.Spire1Code.Config;
using Spire1Ironclad = Spire1.Spire1Code.Character.Ironclad;
using Spire1Silent = Spire1.Spire1Code.Character.Silent;
using Spire1Defect = Spire1.Spire1Code.Character.Defect;
using EngineIroncladCardPool = MegaCrit.Sts2.Core.Models.CardPools.IroncladCardPool;
using EngineSilentCardPool = MegaCrit.Sts2.Core.Models.CardPools.SilentCardPool;
using EngineDefectCardPool = MegaCrit.Sts2.Core.Models.CardPools.DefectCardPool;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// C10 (2026-10-02): closes the LargeCapsule bypass around the Spire1 cards content gate.
/// <para>
/// ENGINE FACT: LargeCapsule.AfterObtained adds one Basic Strike and one Basic Defend to the
/// deck through the private helpers GetStrikeForCharacter / GetDefendForCharacter, which read
/// character.CardPool.AllCards directly and bypass CardPoolModel.GetUnlockedCards /
/// FilterThroughEpochs (see .tmp/dllsrc/MegaCrit.Sts2.Core.Models.Relics/LargeCapsule.cs:40-53).
/// The C02 pool gate lives on GetUnlockedCards, and Spire1CardPool.AllCards must stay
/// unfiltered so CardModel.Pool and old-save SPIRE1-* ids keep resolving (C02-04), so the
/// engine helper is the only correct interception point. This file adds an independent patch;
/// no engine file and no C02/C03/C05/C06 agent file is modified.
/// </para>
/// <para>
/// BEHAVIOR: when the cards content group is open (master switch + group switch + this-run
/// snapshot), both helpers run untouched - byte-for-byte the vanilla behavior, including the
/// TestMode/Deprived branch. When the group is closed, only the three Spire1 placeholder
/// characters are intercepted; they receive the engine-equivalent Basic Strike/Defend of their
/// placeholder identity (Ironclad / Silent / Defect), never a SPIRE1-* card. Vanilla
/// characters and other mods' characters are never touched, even with the group closed.
/// </para>
/// <para>
/// FAIL-CLOSED MOUNT: HarmonyPrepare resolves BOTH engine helpers before either prefix is
/// applied. If either target is missing (engine signature drift), nothing is mounted, vanilla
/// LargeCapsule behavior is left unchanged, and one explicit Error is logged - the gate never
/// reports itself as active while unpatched. At runtime the engine substitute has three tiers
/// (engine pool, the character's engine-declared starting deck, typed engine core card); if
/// all three fail (reachable only when ModelDb has lost its core cards), the patch logs an
/// Error and aborts the grant without calling the original helper. This preserves fail-closed
/// behavior instead of allowing SPIRE1 basic content to leak.
/// </para>
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleGatePatch
{
    private static bool _gateAppliedLogged;
    private static bool _resolveFailureLogged;

    /// <summary>
    /// Resolve both engine targets before patching anything: a drifted signature must never
    /// leave the Strike helper patched while the Defend helper (or vice versa) still leaks
    /// SPIRE1 content. Verified against the current test DLL via reflection:
    /// private static CardModel GetStrikeForCharacter(CharacterModel character) and
    /// private static CardModel GetDefendForCharacter(CharacterModel character).
    /// </summary>
    [HarmonyPrepare]
    private static bool Prepare()
    {
        MethodInfo? strike = AccessTools.Method(typeof(LargeCapsule), "GetStrikeForCharacter");
        MethodInfo? defend = AccessTools.Method(typeof(LargeCapsule), "GetDefendForCharacter");
        if (!IsExpectedSignature(strike) || !IsExpectedSignature(defend))
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule gate NOT mounted: engine target drifted " +
                $"(GetStrikeForCharacter={Describe(strike)}, GetDefendForCharacter={Describe(defend)}; " +
                "expected private static CardModel X(CharacterModel)). " +
                "LargeCapsule can still grant SPIRE1 basic cards while the cards group is off.");
            return false;
        }

        return true;
    }

    /// <summary>Exactly the signature this class binds: static, one CharacterModel parameter,
    /// CardModel return. Any drift must stop the whole class (never a half-mounted gate).</summary>
    private static bool IsExpectedSignature(MethodInfo? method)
        => method is not null
           && method.IsStatic
           && method.ReturnType == typeof(CardModel)
           && method.GetParameters() is [var parameter]
           && parameter.ParameterType == typeof(CharacterModel);

    private static string Describe(MethodInfo? method)
    {
        if (method is null)
        {
            return "missing";
        }
        string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"found {method.ReturnType.Name}({parameters})";
    }

    // __0 = the verified `character` parameter of the engine helper. Positional binding keeps
    // working if the engine only renames the parameter; a type or order change fails the mount
    // above (fail closed + Error log).
    [HarmonyPatch(typeof(LargeCapsule), "GetStrikeForCharacter")]
    [HarmonyPrefix]
    private static bool ReplaceStrike(CharacterModel __0, ref CardModel __result)
        => TryReplaceBasic(__0, CardTag.Strike, ref __result);

    [HarmonyPatch(typeof(LargeCapsule), "GetDefendForCharacter")]
    [HarmonyPrefix]
    private static bool ReplaceDefend(CharacterModel __0, ref CardModel __result)
        => TryReplaceBasic(__0, CardTag.Defend, ref __result);

    private static bool TryReplaceBasic(CharacterModel character, CardTag tag, ref CardModel __result)
    {
        // Open group: original engine behavior, untouched for every character.
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            return true;
        }

        // Closed group: scope is exactly the three Spire1 placeholder characters. Vanilla
        // characters keep the engine helper, so vanilla LargeCapsule is unchanged for them.
        if (!IsSpire1Character(character))
        {
            return true;
        }

        try
        {
            CardModel? replacement = ResolveEngineBasic(character, tag);
            if (replacement is null)
            {
                string detail = "no Basic card with the required tag was resolvable";
                LogResolveFailure(character, tag, detail);
                throw new InvalidOperationException(
                    $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name}: {detail}");
            }

            __result = replacement;
            if (!_gateAppliedLogged)
            {
                _gateAppliedLogged = true;
                MainFile.Logger.Info(
                    $"[Spire1] LargeCapsule gate active: cards content group off - {character.GetType().Name} " +
                    $"receives the engine Basic {tag} card instead of SPIRE1 content.");
            }
            return false;
        }
        catch (Exception e)
        {
            LogResolveFailure(character, tag, $"{e.GetType().Name}: {e.Message}");
            throw new InvalidOperationException(
                $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name} after engine Basic resolution failed.",
                e);
        }
    }

    private static bool IsSpire1Character(CharacterModel character)
        => character is Spire1Ironclad or Spire1Silent or Spire1Defect;

    /// <summary>
    /// Engine-equivalent Basic card for the Spire1 placeholder identity, using the same rule as
    /// the engine helper (Rarity == Basic + tag) but against the engine pool. Two fallback
    /// tiers keep the closed gate from ever handing back a SPIRE1-* card: the character's
    /// StartingDeck (declared with engine starter cards) and finally the typed engine core
    /// card. A null return is only reachable if the engine content itself is missing.
    /// </summary>
    private static CardModel? ResolveEngineBasic(CharacterModel character, CardTag tag)
    {
        CardPoolModel enginePool = character switch
        {
            Spire1Ironclad => ModelDb.CardPool<EngineIroncladCardPool>(),
            Spire1Silent => ModelDb.CardPool<EngineSilentCardPool>(),
            Spire1Defect => ModelDb.CardPool<EngineDefectCardPool>(),
            _ => throw new InvalidOperationException("not a Spire1 character"),
        };

        CardModel? fromPool = enginePool.AllCards.FirstOrDefault(
            c => c.Rarity == CardRarity.Basic && c.Tags.Contains(tag));
        if (fromPool is not null)
        {
            return fromPool;
        }

        CardModel? fromDeck = character.StartingDeck.FirstOrDefault(
            c => c.Rarity == CardRarity.Basic && c.Tags.Contains(tag));
        if (fromDeck is not null)
        {
            return fromDeck;
        }

        return tag == CardTag.Strike ? EngineStrike(character) : EngineDefend(character);
    }

    private static CardModel EngineStrike(CharacterModel character) => character switch
    {
        Spire1Ironclad => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(),
        Spire1Silent => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(),
        Spire1Defect => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(),
        _ => throw new InvalidOperationException("not a Spire1 character"),
    };

    private static CardModel EngineDefend(CharacterModel character) => character switch
    {
        Spire1Ironclad => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(),
        Spire1Silent => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(),
        Spire1Defect => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(),
        _ => throw new InvalidOperationException("not a Spire1 character"),
    };

    private static void LogResolveFailure(CharacterModel character, CardTag tag, string detail)
    {
        if (_resolveFailureLogged)
        {
            return;
        }
        _resolveFailureLogged = true;
        MainFile.Logger.Error(
            $"[Spire1] LargeCapsule gate: could not resolve the engine Basic {tag} card for " +
            $"{character.GetType().Name} ({detail}); the grant is aborted and the original helper is not called.");
    }
}