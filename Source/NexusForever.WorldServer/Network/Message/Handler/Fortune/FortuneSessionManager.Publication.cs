using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public partial class FortuneSessionManager
    {
        private const int MaxPublicationFailures = 3;
        private readonly ConditionalWeakTable<IWorldSession, CommittedPublication> pendingPublications = new();

        private static bool OwnsPublicationQueue(IWorldSession session, IAccount account, IPlayer player, EventQueue events)
            => OwnsSession(session, account, player) && ReferenceEquals(session.Events, events)
                && session is not NetworkSession { IsDisconnecting: true };

        private bool HasPendingPublication(IWorldSession session)
        {
            if (!pendingPublications.TryGetValue(session, out CommittedPublication pending))
                return false;
            if (pending.OwnsQueue())
                return true;
            pendingPublications.Remove(session);
            return false;
        }

        private void PublishCommitted(IWorldSession session, IAccount account, EventQueue events, Func<bool> ownsQueue,
            Func<bool> cardsAreCurrent, FortuneSession deal, Func<AuthContext, bool> verifyInventory, IReadOnlyList<Action> steps)
            => AdvancePublication(session, account, events,
                new CommittedPublication(account.WorldSessionEpoch, ownsQueue, cardsAreCurrent, deal, verifyInventory, steps), retry: false);

        // Callers and queue callbacks both hold sync. A successful step is
        // retired before attempting the next one, including on a partial send.
        private void AdvancePublication(IWorldSession session, IAccount account, EventQueue events,
            CommittedPublication pending, bool retry)
        {
            try
            {
                if (pending.OwnsQueue() && retry && !VerifyPublicationRetry(account.Id, pending))
                {
                    if (pending.OwnsQueue())
                        DisconnectFortune(session);
                    else
                        ReleasePublication(session, pending);
                    return;
                }

                while (pending.NextStep < pending.Steps.Count && pending.OwnsQueue())
                {
                    pending.Steps[pending.NextStep]();
                    pending.NextStep++;
                }
                ReleasePublication(session, pending);
            }
            catch (Exception error)
            {
                log.LogWarning(error, "Committed Fortune publication failed for account {AccountId}.", account.Id);
                if (!pending.OwnsQueue())
                {
                    ReleasePublication(session, pending);
                    return;
                }

                if (++pending.Failures >= MaxPublicationFailures || events == null ||
                    (pendingPublications.TryGetValue(session, out CommittedPublication other) && !ReferenceEquals(other, pending)))
                {
                    DisconnectFortune(session);
                    return;
                }

                if (!retry)
                    pendingPublications.Add(session, pending);
                events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                {
                    lock (sync)
                    {
                        if (pendingPublications.TryGetValue(session, out CommittedPublication current) && ReferenceEquals(current, pending))
                            AdvancePublication(session, account, events, pending, retry: true);
                    }
                }));
            }
        }

        private bool VerifyPublicationRetry(uint accountId, CommittedPublication pending)
        {
            using AuthContext context = databaseManager.GetDatabase<AuthDatabase>().CreateContext();
            return context.Database.CreateExecutionStrategy().Execute(() =>
            {
                using IDbContextTransaction proof = context.Database.BeginTransaction(IsolationLevel.Serializable);
                if (!IsWorldSessionEpoch(context, accountId, pending.Epoch) || pending.VerifyInventory?.Invoke(context) == false)
                    return false;
                if (!pending.CardsAreCurrent())
                    return true;
                AccountFortuneSessionModel stored = context.AccountFortuneSession.AsNoTracking().SingleOrDefault(row => row.Id == accountId);
                return stored != null && ReadCards(stored).SequenceEqual(pending.Deal.Cards);
            });
        }

        private void ReleasePublication(IWorldSession session, CommittedPublication pending)
        {
            if (pendingPublications.TryGetValue(session, out CommittedPublication current) && ReferenceEquals(current, pending))
                pendingPublications.Remove(session);
        }

        private void DisconnectFortune(IWorldSession session)
        {
            pendingPublications.Remove(session);
            // The account deal may already have been published by a newer session.
            session.CanProcessIncomingPackets = false;
            session.ForceDisconnect();
        }

        private sealed class CommittedPublication(ulong epoch, Func<bool> ownsQueue, Func<bool> cardsAreCurrent,
            FortuneSession deal, Func<AuthContext, bool> verifyInventory, IReadOnlyList<Action> steps)
        {
            public ulong Epoch { get; } = epoch;
            public Func<bool> OwnsQueue { get; } = ownsQueue;
            public Func<bool> CardsAreCurrent { get; } = cardsAreCurrent;
            public FortuneSession Deal { get; } = deal;
            public Func<AuthContext, bool> VerifyInventory { get; } = verifyInventory;
            public IReadOnlyList<Action> Steps { get; } = steps;
            public int NextStep { get; set; }
            public int Failures { get; set; }
        }
    }
}
