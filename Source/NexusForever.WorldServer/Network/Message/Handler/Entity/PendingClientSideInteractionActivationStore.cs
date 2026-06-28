using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;

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
            if (session == null || entity == null)
                return;

            var activation = new PendingClientSideInteractionActivation(
                entity.Guid,
                entity.CreatureId,
                clientSideInteractionId,
                spell4BaseId,
                invokeActivateCast);

            lock (sync)
            {
                pendingActivations.Remove(session);
                pendingActivations.Add(session, new PendingActivationHolder(activation));
            }
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
                if (!pendingActivations.TryGetValue(session, out PendingActivationHolder holder))
                    return false;

                PendingClientSideInteractionActivation candidate = holder.Activation;
                if (requestedId != 0u
                    && requestedId != candidate.EntityGuid
                    && requestedId != candidate.ClientSideInteractionId
                    && requestedId != candidate.Spell4BaseId)
                    return false;

                pendingActivations.Remove(session);
                activation = candidate;
                return true;
            }
        }

        private sealed class PendingActivationHolder(PendingClientSideInteractionActivation activation)
        {
            public PendingClientSideInteractionActivation Activation { get; } = activation;
        }
    }

    internal readonly record struct PendingClientSideInteractionActivation(
        uint EntityGuid,
        uint CreatureId,
        uint ClientSideInteractionId,
        uint Spell4BaseId,
        bool InvokeActivateCast);
}
