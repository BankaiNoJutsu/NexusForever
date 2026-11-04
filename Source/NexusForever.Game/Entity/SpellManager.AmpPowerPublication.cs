using NexusForever.Game.Abstract;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private readonly object ampPowerPublicationGate = new();
        private Identity ampPowerIdentity;
        private IGameSession ampPowerSession;
        private EventQueue ampPowerEvents;
        private ushort confirmedAmpPower, publishedAmpPower;
        private bool ampPowerSending, ampPowerScheduled, ampPowerInitialPending;

        private void InitialiseAmpPowerPublication()
        {
            if (player is not Player { Identity: { } identity } owner) return;
            ampPowerIdentity = new() { Id = identity.Id, RealmId = identity.RealmId };
            ampPowerSession = owner.Session;
            ampPowerEvents = ampPowerSession?.Events;
            confirmedAmpPower = publishedAmpPower = bonusAmpPower;
        }

        private bool OwnsAmpPowerPublication() => ampPowerIdentity != null && ampPowerSession != null
            && ReferenceEquals(ampPowerSession.Events, ampPowerEvents)
            && player is Player owner && owner.OwnsAmpPowerPublication(this, ampPowerIdentity, ampPowerSession);

        internal void AcknowledgeAmpPowerPublication(ushort power)
        {
            lock (ampPowerPublicationGate)
                confirmedAmpPower = Math.Min(power, MaxBonusAmpPower);
        }

        internal void NotifyAmpPowerPublicationReady() => ScheduleAmpPowerPublication(Task.CompletedTask);

        private void ScheduleAmpPowerPublication(Task delay)
        {
            if (ampPowerEvents == null || !OwnsAmpPowerPublication()) return;
            lock (ampPowerPublicationGate)
            {
                if (ampPowerSending || ampPowerScheduled || !HasPendingAmpPowerPublication) return;
                ampPowerScheduled = true;
            }
            ampPowerEvents.EnqueueThreadSafe(new PredicateEvent(
                () => !OwnsAmpPowerPublication() || delay.IsCompleted && player is Player { IsNormalSaveInProgress: false },
                () => PublishAmpPower(initial: false)));
        }

        private void SendInitialAmpPower()
        {
            if (player is Player) PublishAmpPower(initial: true);
            else SendAmpPowerUpdate(player.Session, bonusAmpPower);
        }

        private bool HasPendingAmpPowerPublication => ampPowerInitialPending || confirmedAmpPower != publishedAmpPower;

        private void PublishAmpPower(bool initial)
        {
            bool current = OwnsAmpPowerPublication();
            bool initialPending;
            ushort power;
            lock (ampPowerPublicationGate)
            {
                if (!initial) ampPowerScheduled = false;
                if (!current) return;
                if (initial) ampPowerInitialPending = true;
                // A reentrant bootstrap waits for the in-flight value so it cannot
                // send a newer snapshot before an older enqueue has returned.
                if (ampPowerSending || !HasPendingAmpPowerPublication) return;
                initialPending = ampPowerInitialPending;
                ampPowerInitialPending = false;
                ampPowerSending = true;
                power = confirmedAmpPower;
            }

            bool retry = false, sent = false;
            try
            {
                if (!OwnsAmpPowerPublication()) return;
                SendAmpPowerUpdate(ampPowerSession, power);
                if (!OwnsAmpPowerPublication()) return;
                lock (ampPowerPublicationGate) publishedAmpPower = power;
                sent = true;
            }
            catch (Exception error)
            {
                retry = true;
                log.Warn(error, "Committed AMP power update failed on the original actor.");
            }
            finally
            {
                lock (ampPowerPublicationGate)
                {
                    ampPowerSending = false;
                    ampPowerInitialPending |= initialPending && !sent;
                }
                ScheduleAmpPowerPublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }

        private static void SendAmpPowerUpdate(IGameSession session, ushort power)
            => session.EnqueueMessageEncrypted(new ServerAmpPowerUpdate { BonusPower = power });
    }
}
