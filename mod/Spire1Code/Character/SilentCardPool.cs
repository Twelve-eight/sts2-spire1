using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Spire1.Spire1Code.Config;
using EngineSilentCardPool = MegaCrit.Sts2.Core.Models.CardPools.SilentCardPool;

namespace Spire1.Spire1Code.Character;

public class SilentCardPool : CustomCardPoolModel
{
    public override string Title => Silent.CharacterId; //This is not a display name.

    // Reuse StS2 Silent's energy icon (res://images/packed/sprite_fonts/silent_energy_icon.png)
    // and its green card frame, matching PlaceholderID="silent". No custom charui paths.
    public override string EnergyColorName => "silent";
    public override string CardFrameMaterialPath => "card_frame_green";

    //Color of small card icons
    public override Color DeckEntryCardColor => new("5EBD00");

    public override bool IsColorless => false;

    /// <summary>
    /// C02 content gate (2026-10-02). The pool identity stays stable - ModelDb caches
    /// AllCardPools at Preload and CardModel.Pool resolves through those caches, so swapping the
    /// character's pool getter would break old saves holding SPIRE1-* ids. The gate therefore
    /// lives here, on the single query entry every reward/shop/event consumer uses
    /// (CardCreationOptions.GetPossibleCards, MerchantInventory, CardFactory, compendium).
    /// When the cards group (or the per-run snapshot) is closed, this pool answers with the
    /// ENGINE Silent pool, i.e. original vanilla semantics - no SPIRE1-* card and no
    /// SharedCardReuse twin can be offered.
    /// </summary>
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            return base.FilterThroughEpochs(unlockState, cards);
        }
        return ModelDb.CardPool<EngineSilentCardPool>().GetUnlockedCards(unlockState, CardMultiplayerConstraint.None);
    }
}