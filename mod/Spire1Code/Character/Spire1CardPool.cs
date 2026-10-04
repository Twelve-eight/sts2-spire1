using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Unlocks;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Extensions;
using Godot;

namespace Spire1.Spire1Code.Character;

public class Spire1CardPool : CustomCardPoolModel
{
    public override string Title => Ironclad.CharacterId; //This is not a display name.
    
    // Reuse StS2 Ironclad's energy icon (res://images/packed/sprite_fonts/ironclad_energy_icon.png)
    // instead of the mod's gray placeholder charui/*.png, matching PlaceholderID="ironclad".
    public override string EnergyColorName => "ironclad";


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 1f; //Hue; changes the color.
    public override float S => 1f; //Saturation
    public override float V => 1f; //Brightness
    
    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load Spire1/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("ffffff");
    
    public override bool IsColorless => false;

    /// <summary>
    /// C02 content gate (2026-10-02). The pool identity stays stable - ModelDb caches
    /// AllCardPools at Preload and CardModel.Pool resolves through those caches, so swapping the
    /// character's pool getter would break old saves holding SPIRE1-* ids. The gate therefore
    /// lives here, on the single query entry every reward/shop/event consumer uses
    /// (CardCreationOptions.GetPossibleCards, MerchantInventory, CardFactory, compendium).
    /// When the cards group (or the per-run snapshot) is closed, this pool answers with the
    /// ENGINE Ironclad pool, i.e. original vanilla semantics - no SPIRE1-* card and no
    /// SharedCardReuse twin can be offered.
    /// </summary>
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取 (静态构造失败时不得把异常传播到引擎).
        if (!Spire1PowersGate.ContentUnavailableActive
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            return base.FilterThroughEpochs(unlockState, cards);
        }
        return ModelDb.CardPool<IroncladCardPool>().GetUnlockedCards(unlockState, CardMultiplayerConstraint.None);
    }
}