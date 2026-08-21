using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Extensions;

public interface IOnStanceChanged
{
    Task OnStanceChanged(PlayerChoiceContext ctx, StancePower? from, StancePower? to);
}
