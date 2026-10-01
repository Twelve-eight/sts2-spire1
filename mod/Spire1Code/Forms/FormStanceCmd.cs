using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Use the same reachable stance route as the existing cards. Only the run modifier replaces rules;
/// invoking this API in an ordinary run still enters ordinary Calm, Wrath, or Divinity.
/// </summary>
public static class FormStanceCmd
{
    public static Task EnterVoidSerpent(PlayerChoiceContext ctx, Player player, CardModel? source)
        => StanceCmd.Enter<CalmPower>(ctx, player, source);

    public static Task EnterDemonReaper(PlayerChoiceContext ctx, Player player, CardModel? source)
        => StanceCmd.Enter<WrathPower>(ctx, player, source);

    public static Task EnterEchoCelestial(PlayerChoiceContext ctx, Player player, CardModel? source)
        => StanceCmd.Enter<DivinityPower>(ctx, player, source);

    public static Task ExitForm(PlayerChoiceContext ctx, Player player, CardModel? source)
        => StanceCmd.Exit(ctx, player, source);
}
