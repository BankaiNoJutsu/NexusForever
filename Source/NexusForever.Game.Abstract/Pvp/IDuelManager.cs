using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Pvp
{
    public interface IDuelManager : IUpdate
    {
        DuelFailureReason? Initiate(IPlayer challenger, IPlayer opponent);
        DuelFailureReason? Accept(IPlayer player);
        bool Decline(IPlayer player);
        bool Forfeit(IPlayer player);
        bool AreDueling(IPlayer player, IPlayer target);
        bool TryFinishDefeat(IPlayer loser, IPlayer winner);
        void OnPlayerDisconnect(IPlayer player);
    }
}
