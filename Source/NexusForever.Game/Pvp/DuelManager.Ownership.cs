using System.Threading;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Pvp;

public sealed partial class DuelManager
{
    private enum DuelOwnership { Current, Busy, Stale }

    private sealed record DuelParticipant(IPlayer Player, uint UnitId, Identity Identity,
        IAccount Account, uint AccountId, ulong Epoch, IGameSession Session, EventQueue Events,
        IBaseMap Map, IPlayerMapAdmission Admission, object AdmissionGate, ulong Generation)
    {
        internal static DuelParticipant Capture(IPlayer player)
        {
            var account = player.Account; var session = player.Session;
            if (account == null || session?.Events == null || player.Identity == null) return null;
            var admission = player as IPlayerMapAdmission;
            object gate = admission?.MapAdmissionSyncRoot;
            if (gate != null && !Monitor.TryEnter(gate)) return null;
            try
            {
                var owner = new DuelParticipant(player, player.Guid, player.Identity, account, account.Id,
                    account.WorldSessionEpoch, session, session.Events, player.Map, admission, gate,
                    admission?.MapAdmissionGeneration ?? 0);
                return owner.Ownership() == DuelOwnership.Current ? owner : null;
            }
            finally { if (gate != null) Monitor.Exit(gate); }
        }

        internal DuelOwnership Ownership()
        {
            if (!Player.InWorld || Player.Guid != UnitId || Player.Identity != Identity || !ReferenceEquals(Player.Account, Account)
                || Account.Id != AccountId || Account.WorldSessionEpoch != Epoch
                || !ReferenceEquals(Player.Session, Session) || !ReferenceEquals(Account.Session, Session)
                || !ReferenceEquals(Session.Events, Events) || !ReferenceEquals(Player.Map, Map)
                || Session is not IPlayerSession bound || !ReferenceEquals(bound.Player, Player)
                || Session is NetworkSession { IsDisconnecting: true }) return DuelOwnership.Stale;
            if (Admission == null) return DuelOwnership.Current;
            if (!ReferenceEquals(Admission.MapAdmissionSyncRoot, AdmissionGate)) return DuelOwnership.Stale;
            if (!Monitor.TryEnter(AdmissionGate)) return DuelOwnership.Busy;
            try { return Admission.MapAdmissionGeneration == Generation ? DuelOwnership.Current : DuelOwnership.Stale; }
            finally { Monitor.Exit(AdmissionGate); }
        }
    }

    private static DuelOwnership GetOwnership(DuelSession session)
    {
        var a = session.ChallengerOwner.Ownership(); var b = session.OpponentOwner.Ownership();
        if (a == DuelOwnership.Stale || b == DuelOwnership.Stale) return DuelOwnership.Stale;
        return a == DuelOwnership.Busy || b == DuelOwnership.Busy ? DuelOwnership.Busy : DuelOwnership.Current;
    }

    private bool IsRegistered(DuelSession session) => sessionsByPlayer.TryGetValue(session.ChallengerOwner.UnitId, out var current)
        && ReferenceEquals(current, session) && sessionsByPlayer.TryGetValue(session.OpponentOwner.UnitId, out current)
        && ReferenceEquals(current, session);
}
