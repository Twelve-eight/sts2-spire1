using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Ironclad - Flex's temporary Strength. The base game's TemporaryStrengthPower is abstract
/// (never instantiated directly; every granting model subclasses it, e.g. FlexPotionPower), so this
/// subclasses BaseLib's CustomTemporaryPowerModelWrapper instead, which mirrors that behavior:
/// real StrengthPower is applied on apply, synced on amount change, and removed with -amount at end of turn.
/// </summary>
public class FlexPower : CustomTemporaryPowerModelWrapper<Flex, StrengthPower>
{
}
