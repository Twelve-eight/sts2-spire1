using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rewards;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Associates a presentation source with the actual screen, grid, or card node, never with a
/// canonical CardModel. Weak keys bound the associations to host node/list lifetimes; explicit
/// pool and screen boundaries remove them earlier. Missing provenance is not a pure view.
/// </summary>
internal static class Spire1ScopedPortrait
{
    private sealed class Domain(CharacterModel character)
    {
        internal CharacterModel Character = character;
    }

    private static readonly ConditionalWeakTable<Node, Domain> Domains = new();
    private static readonly ConditionalWeakTable<List<CardCreationResult>, Domain> RewardOptions = new();
    // Resolved through AccessTools.Field, which searches the type and its super types, so a field
    // declared on a base type still resolves. A missing member is a pinned-ABI violation and fails
    // loudly, once, at first use.
    private static AccessTools.FieldRef<T, F> Ref<T, F>(string name) where T : class =>
        AccessTools.FieldRefAccess<T, F>(
            AccessTools.Field(typeof(T), name) ?? throw new MissingFieldException(typeof(T).FullName, name));

    private static readonly AccessTools.FieldRef<NCardGrid, List<List<NGridCardHolder>>> GridRows =
        Ref<NCardGrid, List<List<NGridCardHolder>>>("_cardRows");
    private static readonly AccessTools.FieldRef<NCardsViewScreen, NCardGrid> ScreenGrid =
        Ref<NCardsViewScreen, NCardGrid>("_grid");
    private static readonly AccessTools.FieldRef<NInspectCardScreen, NCard> InspectCard =
        Ref<NInspectCardScreen, NCard>("_card");
    private static readonly AccessTools.FieldRef<NInspectCardScreen, List<CardModel>> InspectCards =
        Ref<NInspectCardScreen, List<CardModel>>("_cards");
    private static readonly AccessTools.FieldRef<NCardRewardSelectionScreen, Control> RewardRow =
        Ref<NCardRewardSelectionScreen, Control>("_cardRow");
    private static readonly AccessTools.FieldRef<MerchantEntry, Player> MerchantPlayer =
        Ref<MerchantEntry, Player>("_player");
    private static readonly AccessTools.FieldRef<NHandCardHolder, NPlayerHand> HandOf =
        Ref<NHandCardHolder, NPlayerHand>("_hand");
    private static readonly AccessTools.FieldRef<NCardLibrary, Dictionary<NCardPoolFilter, Func<CardModel, bool>>> LibraryFilters =
        Ref<NCardLibrary, Dictionary<NCardPoolFilter, Func<CardModel, bool>>>("_poolFilters");
    private static readonly AccessTools.FieldRef<NCardLibrary, Dictionary<CharacterModel, NCardPoolFilter>> LibraryCharacters =
        Ref<NCardLibrary, Dictionary<CharacterModel, NCardPoolFilter>>("_cardPoolFilters");
    private static readonly AccessTools.FieldRef<NCardLibrary, NCardGrid> LibraryGrid =
        Ref<NCardLibrary, NCardGrid>("_grid");
    private static readonly AccessTools.FieldRef<NCard, TextureRect> Portrait =
        Ref<NCard, TextureRect>("_portrait");
    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortrait =
        Ref<NCard, TextureRect>("_ancientPortrait");
    private static readonly Action<NCard> UpdatePortrait =
        (AccessTools.Method(typeof(NCard), "UpdatePortrait", Type.EmptyTypes)
            ?? throw new MissingMethodException(typeof(NCard).FullName, "UpdatePortrait"))
            .CreateDelegate<Action<NCard>>();

    internal static CharacterModel? Get(Node? node) =>
        node is not null && Domains.TryGetValue(node, out Domain? domain) ? domain.Character : null;

    private static bool Set(Node node, CharacterModel? character)
    {
        if (!Spire1PortraitSource.IsSourceCharacter(character))
            character = null;
        if (Domains.TryGetValue(node, out Domain? old))
        {
            if (ReferenceEquals(old.Character, character))
                return false;
            if (character is null)
                Domains.Remove(node);
            else
                old.Character = character;
            return true;
        }
        if (character is null)
            return false;
        Domains.Add(node, new Domain(character));
        return true;
    }

    internal static void BindCard(NCard? card, CharacterModel? character)
    {
        if (card is not null && Set(card, character))
            Redraw(card);
    }

    internal static void ForgetCard(NCard card) => Domains.Remove(card);

    internal static void Redraw(NCard card)
    {
        if (card.IsNodeReady() && card.Model is not null)
            UpdatePortrait(card);
    }

    internal static void Paint(NCard card, bool big)
    {
        if (card.Visibility != ModelVisibility.Visible || card.Model is not CardModel model)
            return;
        Texture2D? texture = Spire1PortraitSource.Find(Get(card), model, big);
        if (texture is null)
            return;
        TextureRect target = model.Rarity == CardRarity.Ancient ? AncientPortrait(card) : Portrait(card);
        target.Texture = texture;
    }

    internal static void BindGrid(NCardGrid? grid, CharacterModel? character)
    {
        if (grid is null)
            return;
        Set(grid, character);
        foreach (List<NGridCardHolder> row in GridRows(grid))
            foreach (NGridCardHolder holder in row)
                BindCard(holder.CardNode, character);
    }

    internal static void BindRows(NCardGrid grid)
    {
        CharacterModel? character = Get(grid);
        foreach (List<NGridCardHolder> row in GridRows(grid))
            foreach (NGridCardHolder holder in row)
                BindCard(holder.CardNode, character);
    }

    internal static void BindRow(NCardGrid grid, List<NGridCardHolder> row)
    {
        CharacterModel? character = Get(grid);
        foreach (NGridCardHolder holder in row)
            BindCard(holder.CardNode, character);
    }

    internal static void ForgetGrid(NCardGrid grid)
    {
        BindGrid(grid, null);
        Domains.Remove(grid);
    }

    internal static void BindScreen(NCardsViewScreen screen, CharacterModel? character)
    {
        Set(screen, character);
        BindGrid(ScreenGrid(screen), character);
    }

    internal static void BindGridFromParent(NCardGrid grid)
    {
        for (Node? parent = grid.GetParent(); parent is not null; parent = parent.GetParent())
        {
            if (parent is NCardLibrary library)
            {
                // Resolved from the library's real selected-filter state at display time, so a reopened
                // library does not depend on a later filter event having fired first.
                CharacterModel? source = ResolveLibrary(library);
                Set(library, source);
                BindGrid(grid, source);
                return;
            }
            if (parent is NCardsViewScreen screen)
            {
                BindGrid(grid, Get(screen));
                return;
            }
        }
    }

    private static CharacterModel? ResolveLibrary(NCardLibrary library)
    {
        Dictionary<NCardPoolFilter, Func<CardModel, bool>>? filters = LibraryFilters(library);
        Dictionary<CharacterModel, NCardPoolFilter>? characters = LibraryCharacters(library);
        if (filters is null || characters is null)
            return null;

        NCardPoolFilter? selected = null;
        int count = 0;
        foreach (NCardPoolFilter filter in filters.Keys)
        {
            if (!filter.IsSelected)
                continue;
            selected = filter;
            count++;
        }

        // These pool filters act like radio buttons; zero or several selected filters is not this view.
        if (count != 1)
            return null;
        foreach (KeyValuePair<CharacterModel, NCardPoolFilter> pair in characters)
        {
            if (ReferenceEquals(pair.Value, selected) && Spire1PortraitSource.IsSourceCharacter(pair.Key))
                return pair.Key;
        }
        return null;
    }

    internal static void ApplyLibraryFilter(NCardLibrary library)
    {
        CharacterModel? source = ResolveLibrary(library);
        Set(library, source);
        BindGrid(LibraryGrid(library), source);
    }

    internal static void CloseLibrary(NCardLibrary library)
    {
        BindGrid(LibraryGrid(library), null);
        Domains.Remove(library);
    }

    internal static void RememberReward(List<CardCreationResult> options, Player player)
    {
        RewardOptions.Remove(options);
        CharacterModel? character = player.Character;
        if (Spire1PortraitSource.IsSourceCharacter(character))
            RewardOptions.Add(options, new Domain(character));
    }

    internal static void AttachReward(NCardRewardSelectionScreen? screen, IReadOnlyList<CardCreationResult> options)
    {
        if (screen is null)
            return;
        CharacterModel? character = null;
        if (options is List<CardCreationResult> list && RewardOptions.TryGetValue(list, out Domain? source))
        {
            character = source.Character;
            RewardOptions.Remove(list);
        }
        Set(screen, character);
        BindRewardCards(screen);
    }

    internal static void BindRewardCards(NCardRewardSelectionScreen screen)
    {
        Control? row = RewardRow(screen);
        if (row is null)
            return;
        CharacterModel? character = Get(screen);
        for (int i = 0; i < row.GetChildCount(); i++)
        {
            if (row.GetChild(i) is NGridCardHolder holder && !holder.IsQueuedForDeletion())
                BindCard(holder.CardNode, character);
        }
    }

    internal static void BindMerchant(MerchantCardEntry? entry, NCard? card)
    {
        CharacterModel? character = entry is null ? null : MerchantPlayer(entry)?.Character;
        BindCard(card, character);
    }

    internal static CharacterModel? HandCharacter(NHandCardHolder holder) => Get(HandOf(holder));

    // An unresolved publisher must never clear a domain that a real publisher already established:
    // NHandCardHolder.Create calls SetCard -> UpdateCard before _hand is assigned, and a selected
    // holder may carry no hand at all while holding the same NCard node.
    internal static void BindCardIfKnown(NCard? card, CharacterModel? character)
    {
        if (card is not null && character is not null)
            BindCard(card, character);
    }

    internal static void BindCombat(NPlayerHand? hand, CombatState? state)
    {
        if (hand is null || state is null)
            return;
        Player? player = LocalContext.GetMe((ICombatState)state);
        CharacterModel? character = player?.Character;
        if (character is null)
            return;
        Set(hand, character);
        Control? container = hand.CardHolderContainer;
        if (container is null)
            return;
        for (int i = 0; i < container.GetChildCount(); i++)
            if (container.GetChild(i) is NHandCardHolder holder)
                BindCardIfKnown(holder.CardNode, character);
    }

    internal readonly struct InspectState(NInspectCardScreen? screen, List<CardModel>? cards, CharacterModel? character)
    {
        internal readonly NInspectCardScreen? Screen = screen;
        internal readonly List<CardModel>? Cards = cards;
        internal readonly CharacterModel? Character = character;
    }

    internal static InspectState BeginInspect(CharacterModel? character)
    {
        NInspectCardScreen? screen = NGame.Instance.GetInspectCardScreen();
        return screen is null ? default : new InspectState(screen, InspectCards(screen), character);
    }

    internal static void FinishInspect(InspectState state)
    {
        NInspectCardScreen? screen = state.Screen;
        // Open always assigns a new list; an unchanged list means this call did not open that screen.
        if (screen is null || ReferenceEquals(state.Cards, InspectCards(screen)))
            return;
        Set(screen, state.Character);
        NCard? card = InspectCard(screen);
        BindCard(card, state.Character);
        if (card is not null)
            Paint(card, true);
    }

    internal static void ResetInspect(NInspectCardScreen screen, bool restore)
    {
        Domains.Remove(screen);
        NCard? card = InspectCard(screen);
        if (card is null)
            return;
        Domains.Remove(card);
        if (restore && card.IsNodeReady() && card.Model is CardModel model)
        {
            TextureRect target = model.Rarity == CardRarity.Ancient ? AncientPortrait(card) : Portrait(card);
            target.Texture = model.BigPortrait;
        }
    }

    internal static void RefreshInspect(NInspectCardScreen screen)
    {
        NCard? card = InspectCard(screen);
        BindCard(card, Get(screen));
        if (card is not null)
            Paint(card, true);
    }
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class Spire1ScopedPortraitTexturePatch
{
    private static void Postfix(NCard __instance) => Spire1ScopedPortrait.Paint(__instance, false);
}

[HarmonyPatch(typeof(NCard), nameof(NCard.OnReturnedFromPool))]
internal static class Spire1ScopedPortraitReturnedPatch
{
    private static void Prefix(NCard __instance) => Spire1ScopedPortrait.ForgetCard(__instance);
}

[HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
internal static class Spire1ScopedPortraitFreedPatch
{
    private static void Prefix(NCard __instance) => Spire1ScopedPortrait.ForgetCard(__instance);
}

[HarmonyPatch(typeof(NCardGrid), "InitGrid", new Type[] { })]
internal static class Spire1ScopedPortraitGridInitPatch
{
    private static void Postfix(NCardGrid __instance) => Spire1ScopedPortrait.BindRows(__instance);
}

[HarmonyPatch(typeof(NCardGrid), "AssignCardsToRow", new[] { typeof(List<NGridCardHolder>), typeof(int) })]
internal static class Spire1ScopedPortraitGridRebindPatch
{
    private static void Postfix(NCardGrid __instance, List<NGridCardHolder> row) =>
        Spire1ScopedPortrait.BindRow(__instance, row);
}

[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.SetCards))]
internal static class Spire1ScopedPortraitGridSetPatch
{
    private static void Prefix(NCardGrid __instance) => Spire1ScopedPortrait.BindGridFromParent(__instance);
}

[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.ClearGrid))]
internal static class Spire1ScopedPortraitGridClearPatch
{
    private static void Prefix(NCardGrid __instance) => Spire1ScopedPortrait.ForgetGrid(__instance);
}

// NCardLibraryGrid overrides both grid builders, so a call on the library grid never reaches the
// NCardGrid declarations above; the exact overrides are patched too.
[HarmonyPatch(typeof(NCardLibraryGrid), "InitGrid", new Type[] { })]
internal static class Spire1ScopedPortraitLibraryGridInitPatch
{
    private static void Postfix(NCardLibraryGrid __instance) => Spire1ScopedPortrait.BindRows(__instance);
}

[HarmonyPatch(typeof(NCardLibraryGrid), "AssignCardsToRow", new[] { typeof(List<NGridCardHolder>), typeof(int) })]
internal static class Spire1ScopedPortraitLibraryGridRebindPatch
{
    private static void Postfix(NCardLibraryGrid __instance, List<NGridCardHolder> row) =>
        Spire1ScopedPortrait.BindRow(__instance, row);
}

[HarmonyPatch(typeof(NCardLibrary), "UpdateFilter")]
internal static class Spire1ScopedPortraitLibraryPatch
{
    private static void Prefix(NCardLibrary __instance) => Spire1ScopedPortrait.ApplyLibraryFilter(__instance);
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuClosed))]
internal static class Spire1ScopedPortraitLibraryClosePatch
{
    private static void Prefix(NCardLibrary __instance) => Spire1ScopedPortrait.CloseLibrary(__instance);
}

[HarmonyPatch(typeof(NDeckViewScreen), nameof(NDeckViewScreen.ShowScreen))]
internal static class Spire1ScopedPortraitDeckPatch
{
    private static void Postfix(Player player, NDeckViewScreen? __result)
    {
        if (__result is NCardsViewScreen screen)
            Spire1ScopedPortrait.BindScreen(screen, player.Character);
    }
}

[HarmonyPatch(typeof(CardReward), "OnSelect")]
internal static class Spire1ScopedPortraitRewardOwnerPatch
{
    private static void Prefix(CardReward __instance, List<CardCreationResult> ____cards) =>
        Spire1ScopedPortrait.RememberReward(____cards, __instance.Player);
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.ShowScreen))]
internal static class Spire1ScopedPortraitRewardScreenPatch
{
    private static void Postfix(IReadOnlyList<CardCreationResult> options, NCardRewardSelectionScreen? __result) =>
        Spire1ScopedPortrait.AttachReward(__result, options);
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.RefreshOptions))]
internal static class Spire1ScopedPortraitRewardRefreshPatch
{
    private static void Postfix(NCardRewardSelectionScreen __instance) => Spire1ScopedPortrait.BindRewardCards(__instance);
}

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
internal static class Spire1ScopedPortraitMerchantPatch
{
    private static void Postfix(MerchantCardEntry? ____cardEntry, NCard? ____cardNode) =>
        Spire1ScopedPortrait.BindMerchant(____cardEntry, ____cardNode);
}

[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
internal static class Spire1ScopedPortraitInspectOpenPatch
{
    private static void Prefix(NInspectCardScreen __instance) => Spire1ScopedPortrait.ResetInspect(__instance, false);
}

[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
internal static class Spire1ScopedPortraitInspectDisplayPatch
{
    private static void Postfix(NInspectCardScreen __instance) => Spire1ScopedPortrait.RefreshInspect(__instance);
}

[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
internal static class Spire1ScopedPortraitInspectClosePatch
{
    private static void Prefix(NInspectCardScreen __instance) => Spire1ScopedPortrait.ResetInspect(__instance, true);
}

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
internal static class Spire1ScopedPortraitLibraryInspectPatch
{
    private static void Prefix(NCardHolder holder, out Spire1ScopedPortrait.InspectState __state) =>
        __state = Spire1ScopedPortrait.BeginInspect(Spire1ScopedPortrait.Get(holder.CardNode));
    private static void Postfix(Spire1ScopedPortrait.InspectState __state) => Spire1ScopedPortrait.FinishInspect(__state);
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), "InspectCard")]
internal static class Spire1ScopedPortraitRewardInspectPatch
{
    private static void Prefix(NCardHolder cardHolder, out Spire1ScopedPortrait.InspectState __state) =>
        __state = Spire1ScopedPortrait.BeginInspect(Spire1ScopedPortrait.Get(cardHolder.CardNode));
    private static void Postfix(Spire1ScopedPortrait.InspectState __state) => Spire1ScopedPortrait.FinishInspect(__state);
}

[HarmonyPatch(typeof(NMerchantCard), "OnPreview")]
internal static class Spire1ScopedPortraitMerchantInspectPatch
{
    private static void Prefix(NCard? ____cardNode, out Spire1ScopedPortrait.InspectState __state) =>
        __state = Spire1ScopedPortrait.BeginInspect(Spire1ScopedPortrait.Get(____cardNode));
    private static void Postfix(Spire1ScopedPortrait.InspectState __state) => Spire1ScopedPortrait.FinishInspect(__state);
}

[HarmonyPatch(typeof(NCardsViewScreen), "ShowCardDetail")]
internal static class Spire1ScopedPortraitDeckInspectPatch
{
    private static void Prefix(NCardsViewScreen __instance, out Spire1ScopedPortrait.InspectState __state) =>
        __state = Spire1ScopedPortrait.BeginInspect(Spire1ScopedPortrait.Get(__instance));
    private static void Postfix(Spire1ScopedPortrait.InspectState __state) => Spire1ScopedPortrait.FinishInspect(__state);
}

// Combat hand. The publisher is the real host hand instance: the character comes from the same
// LocalContext.GetMe(ICombatState) call the host itself performs in NCombatUi.Activate, never from a
// global NCard.Create caller or a mutable CardModel.Owner.
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
internal static class Spire1ScopedPortraitCombatActivatePatch
{
    private static void Postfix(NCombatUi __instance, CombatState state) =>
        Spire1ScopedPortrait.BindCombat(__instance.Hand, state);
}

[HarmonyPatch(typeof(NPlayerHand), "OnCombatStateChanged")]
internal static class Spire1ScopedPortraitHandStatePatch
{
    // The host stores the state and then refreshes every ordinary, queued and selected holder, so the
    // hand domain has to exist before those refreshes repaint.
    private static void Prefix(NPlayerHand __instance, CombatState state) =>
        Spire1ScopedPortrait.BindCombat(__instance, state);
}

[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.Add))]
internal static class Spire1ScopedPortraitHandAddPatch
{
    // Add returns an existing holder for a duplicate node, and for a duplicate model it frees the
    // input node and returns the existing holder, so only the returned holder's CardNode is in hand.
    private static void Postfix(NPlayerHand __instance, NHandCardHolder __result) =>
        Spire1ScopedPortrait.BindCardIfKnown(__result?.CardNode, Spire1ScopedPortrait.Get(__instance));
}

[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
internal static class Spire1ScopedPortraitHandHolderPatch
{
    private static void Prefix(NHandCardHolder __instance) =>
        Spire1ScopedPortrait.BindCardIfKnown(__instance.CardNode, Spire1ScopedPortrait.HandCharacter(__instance));
}
