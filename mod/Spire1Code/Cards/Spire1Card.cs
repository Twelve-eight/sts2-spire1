using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Base class for all Ironclad (Spire1) cards. Carries the pool tag so concrete cards need no [Pool].
/// M1: every card uses the shipped placeholder art (card.png); real per-card art is a later wave.
/// </summary>
[Pool(typeof(Spire1CardPool))]
public abstract class Spire1Card(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 引擎把 CanonicalKeywords 首次访问后缓存进私有 _keywords，升级/读档均不清空；
    /// 需要按升级态变化关键词的卡（如突破极限）在 OnUpgrade 里调用本方法强制重新物化。
    /// </summary>
    protected void ResetKeywordCache()
        => typeof(global::MegaCrit.Sts2.Core.Models.CardModel)
            .GetField("_keywords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, null);
}
