using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class PendingClientSideInteractionActivationStore
    {
        private static readonly object sync = new();
        private static readonly ConditionalWeakTable<IWorldSession, PendingActivationHolder> pendingActivations = new();

        public static void Set(
            IWorldSession session,
            IWorldEntity entity,
            uint clientSideInteractionId,
            uint spell4BaseId,
            bool invokeActivateCast)
        {
            Arm(session, Begin(session, entity, clientSideInteractionId, spell4BaseId, invokeActivateCast));
        }

        public static PendingClientSideInteractionActivation Begin(IWorldSession session, IWorldEntity entity,
            uint clientSideInteractionId, uint spell4BaseId, bool invokeActivateCast)
        {
            if (session?.Player == null || entity == null)
                return null;

            var activation = new PendingClientSideInteractionActivation(session, entity,
                clientSideInteractionId, spell4BaseId, invokeActivateCast);
            lock (sync)
            {
                pendingActivations.Remove(session);
                pendingActivations.Add(session, new PendingActivationHolder(activation));
            }
            return activation;
        }

        public static void Arm(IWorldSession session, PendingClientSideInteractionActivation activation)
        {
            if (activation == null || !activation.OwnsActor(session))
                return;

            lock (sync)
                if (pendingActivations.TryGetValue(session, out var holder)
                    && ReferenceEquals(holder.Activation, activation))
                    holder.Ready = true;
        }

        public static void Cancel(IWorldSession session, PendingClientSideInteractionActivation activation)
        {
            if (session == null || activation == null)
                return;
            lock (sync)
                if (pendingActivations.TryGetValue(session, out var holder) && ReferenceEquals(holder.Token, activation.Token))
                {
                    holder.Activation = null;
                    holder.Ready = false;
                }
        }

        public static bool IsCurrent(IWorldSession session, PendingClientSideInteractionActivation activation)
        {
            if (session == null || activation == null || !activation.OwnsActor(session))
                return false;
            lock (sync)
                return pendingActivations.TryGetValue(session, out var holder) && ReferenceEquals(holder.Token, activation.Token);
        }

        public static bool TryConsume(
            IWorldSession session,
            uint requestedId,
            out PendingClientSideInteractionActivation activation)
        {
            activation = default;
            if (session == null)
                return false;

            lock (sync)
            {
                if (!pendingActivations.TryGetValue(session, out PendingActivationHolder holder) || !holder.Ready || holder.Activation == null)
                    return false;

                PendingClientSideInteractionActivation candidate = holder.Activation;
                if (requestedId != 0u
                    && requestedId != candidate.EntityGuid
                    && requestedId != candidate.ClientSideInteractionId
                    && requestedId != candidate.Spell4BaseId)
                    return false;

                // Retain only the token after consumption, releasing the captured actor/map graph.
                // Reentrant callbacks and late cast finishes must not act for a newer ticket.
                holder.Activation = null;
                holder.Ready = false;
                activation = candidate;
            }
            if (IsCurrent(session, activation))
                return true;
            Cancel(session, activation);
            activation = null;
            return false;
        }

        private sealed class PendingActivationHolder(PendingClientSideInteractionActivation activation)
        {
            public object Token { get; } = activation.Token;
            public PendingClientSideInteractionActivation Activation { get; set; } = activation;
            public bool Ready { get; set; }
        }
    }

    internal sealed class PendingClientSideInteractionActivation
    {
        public object Token { get; } = new();
        public IPlayer Player { get; }
        public IWorldEntity Entity { get; }
        public uint EntityGuid { get; }
        public uint CreatureId { get; }
        public uint ClientSideInteractionId { get; }
        public uint Spell4BaseId { get; }
        public bool InvokeActivateCast { get; }
        private readonly IGameSession playerSession;
        private readonly EventQueue events;
        private readonly IBaseMap map;
        private readonly IBaseMap entityMap;
        private readonly IAccount account;
        private readonly IAccount sessionAccount;
        private readonly ulong? accountEpoch;
        private readonly ulong? admission;

        public PendingClientSideInteractionActivation(IWorldSession session, IWorldEntity entity,
            uint clientSideInteractionId, uint spell4BaseId, bool invokeActivateCast)
        {
            Player = session.Player;
            Entity = entity;
            EntityGuid = entity.Guid;
            CreatureId = entity.CreatureId;
            ClientSideInteractionId = clientSideInteractionId;
            Spell4BaseId = spell4BaseId;
            InvokeActivateCast = invokeActivateCast;
            playerSession = Player.Session;
            events = session.Events;
            map = Player.Map;
            entityMap = entity.Map;
            account = Player.Account;
            sessionAccount = session.Account;
            accountEpoch = account?.WorldSessionEpoch;
            admission = (Player as IPlayerMapAdmission)?.MapAdmissionGeneration;
        }

        public bool OwnsActor(IWorldSession session) => ReferenceEquals(session?.Player, Player)
            && ReferenceEquals(Player.Session, playerSession) && ReferenceEquals(session.Events, events)
            && (playerSession == null || ReferenceEquals(playerSession, session))
            && ReferenceEquals(Player.Map, map) && ReferenceEquals(Player.Account, account)
            && ReferenceEquals(session.Account, sessionAccount) && account?.WorldSessionEpoch == accountEpoch
            && (account == null || ReferenceEquals(account.Session, session))
            && (sessionAccount == null || ReferenceEquals(sessionAccount, account))
            && (admission == null || ((IPlayerMapAdmission)Player).IsMapAdmissionCurrent(admission.Value));

        public bool IsCurrent(IWorldSession session) => PendingClientSideInteractionActivationStore.IsCurrent(session, this);

        public IWorldEntity ResolveTarget(IWorldSession session)
        {
            if (!IsCurrent(session) || !ReferenceEquals(Entity.Map, entityMap)
                || Entity.Guid != EntityGuid || Entity.CreatureId != CreatureId)
                return null;

            IWorldEntity current = Player.GetVisible<IWorldEntity>(EntityGuid);
            bool visible = current != null;
            current ??= map?.GetEntity<IWorldEntity>(EntityGuid);
            if (!ReferenceEquals(current, Entity) || (!visible && !Player.CanSeeEntity(Entity)))
                return null;
            return IsCurrent(session) ? Entity : null;
        }
    }
}
