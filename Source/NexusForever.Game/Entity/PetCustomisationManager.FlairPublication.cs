using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Shared.Game.Events;
using NLog;
using WirePet = NexusForever.Network.World.Message.Model.Shared.PetCustomisation;

namespace NexusForever.Game.Entity
{
    public partial class PetCustomisationManager
    {
        internal const uint MaxWireFlairId = (1u << 14) - 1;
        private const uint FlairBitCount = 512u * 8;
        private sealed record FlairNotice(ushort? Id, uint[] Bits, WirePet[] Pets)
        {
            internal IWritable Build()
            {
                if (Id.HasValue) return new ServerUnlockPetFlair { PetFlairId = Id.Value };
                var packet = new ServerPetCustomisationList { PetCustomisations = Pets.Select(ClonePet).ToList() };
                foreach (uint bit in Bits) packet.UnlockedFlair.SetBit(bit, true);
                return packet;
            }
        }
        private static readonly ILogger flairPublicationLog = LogManager.GetCurrentClassLogger();
        private readonly object flairPublicationGate = new();
        private readonly HashSet<ushort> flairNotificationIntent = [];
        private readonly Queue<FlairNotice> flairNotices = [];
        private HashSet<uint> confirmedFlairs;
        private Identity flairIdentity;
        private IGameSession flairSession;
        private EventQueue flairEvents;
        private IAccount flairAccount;
        private ulong flairEpoch;
        private bool flairScheduled, flairSending;

        private void InitialiseFlairPublication()
        {
            confirmedFlairs = LoadedExperienceFlairs.Select(row => row.Id).ToHashSet();
            if (player is not Player owner) return;
            flairIdentity = new() { Id = owner.CharacterId, RealmId = owner.Identity.RealmId };
            flairSession = owner.Session; flairEvents = flairSession?.Events;
            flairAccount = owner.Account; flairEpoch = flairAccount?.WorldSessionEpoch ?? 0;
        }
        private bool OwnsFlairPublication() => player is Player owner && flairIdentity != null
            && owner.OwnsPetFlairPublication(this, flairIdentity, flairSession)
            && ReferenceEquals(flairSession.Events, flairEvents) && ReferenceEquals(owner.Account, flairAccount)
            && flairAccount?.WorldSessionEpoch == flairEpoch && ReferenceEquals(flairAccount.Session, flairSession);
        private void QueueFlairPublication(ushort id, bool notify)
        {
            lock (flairPublicationGate) if (notify) flairNotificationIntent.Add(id);
        }
        internal void AcknowledgeFlairPublication(ExperiencePersistence.Flair[] rows)
        {
            lock (flairPublicationGate)
            {
                confirmedFlairs = rows.Select(row => row.Id).ToHashSet();
                foreach (ushort id in flairNotificationIntent.Where(id => confirmedFlairs.Contains(id)).Order().ToArray())
                {
                    flairNotices.Enqueue(new(id, null, null)); flairNotificationIntent.Remove(id);
                }
            }
        }
        private static WirePet ClonePet(WirePet pet) => new() { PetType = pet.PetType, PetObjectId = pet.PetObjectId,
            PetName = pet.PetName, SlotFlairIds = pet.SlotFlairIds.ToArray() };
        private void QueueInitialFlairPublication()
        {
            lock (flairPublicationGate)
            {
                uint[] bits = petFlairs.Where(row => confirmedFlairs.Contains(row.Key))
                    .SelectMany(row => row.Value.Entry.UnlockBitIndex ?? []).Where(bit => bit < FlairBitCount).Distinct().ToArray();
                WirePet[] pets = petCustomisations.Values.Select(row => ClonePet(row.Build())).ToArray();
                foreach (var pet in pets)
                    for (int i = 0; i < pet.SlotFlairIds.Length; i++)
                        if (!confirmedFlairs.Contains(pet.SlotFlairIds[i])) pet.SlotFlairIds[i] = 0;
                flairNotices.Enqueue(new(null, bits, pets));
            }
            NotifyFlairPublicationReady();
        }
        internal void NotifyFlairPublicationReady() => ScheduleFlairPublication(Task.CompletedTask);
        private void ScheduleFlairPublication(Task delay)
        {
            if (!OwnsFlairPublication()) return;
            lock (flairPublicationGate)
            {
                if (flairScheduled || flairSending || flairNotices.Count == 0) return;
                flairScheduled = true;
            }
            flairEvents.EnqueueThreadSafe(new PredicateEvent(
                () => !OwnsFlairPublication() || delay.IsCompleted && player is Player { IsNormalSaveInProgress: false }, PublishFlairNotices));
        }
        private void PublishFlairNotices()
        {
            lock (flairPublicationGate)
            {
                flairScheduled = false;
                if (flairSending || !OwnsFlairPublication()) return;
                flairSending = true;
            }
            bool retry = false;
            try
            {
                while (OwnsFlairPublication())
                {
                    FlairNotice notice;
                    lock (flairPublicationGate) if (!flairNotices.TryPeek(out notice)) return;
                    if (!OwnsFlairPublication()) return;
                    flairSession.EnqueueMessageEncrypted(notice.Build());
                    lock (flairPublicationGate) flairNotices.Dequeue();
                }
            }
            catch (Exception error) { retry = true; flairPublicationLog.Warn(error, "Committed pet flair notice retains its original packet cursor."); }
            finally
            {
                lock (flairPublicationGate) flairSending = false;
                ScheduleFlairPublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
