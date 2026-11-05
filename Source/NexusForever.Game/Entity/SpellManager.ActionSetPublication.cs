using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private enum ActionSetInitial { Book, Points, Sets, Active, Amps }
        private sealed record ActionSetSpellUpdate(uint Id, byte Tier, byte Spec);
        private sealed class ActionSetRequest(byte spec, bool amps)
        {
            internal long Sequence;
            internal readonly byte Spec = spec;
            internal bool Points, Amps = amps;
            internal readonly List<ActionSetSpellUpdate> Updates = [];
            internal readonly Dictionary<uint, NetworkItem> Items = [];
        }
        private sealed class ActionSetPublication(ExperiencePersistence.Spellbook book, ActionSetRequest[] requests)
        {
            internal readonly ExperiencePersistence.Spellbook Book = book;
            internal readonly ActionSetRequest[] Requests = requests;
            internal List<Func<IWritable>> Packets;
            internal int Cursor;
        }
        private readonly object actionSetPublicationGate = new();
        private readonly List<ActionSetRequest> actionSetRequests = [];
        private readonly Queue<ActionSetPublication> actionSetPublications = [];
        private ExperiencePersistence.Spellbook confirmedActionSets;
        private IAccount actionSetAccount;
        private ulong actionSetEpoch;
        private long actionSetSequence;
        private ActionSetRequest actionSetCapture;
        private int actionSetCaptureThread;
        private bool actionSetSending, actionSetScheduled;
        internal long ActionSetSequence { get { lock (actionSetPublicationGate) return actionSetSequence; } }

        private void InitialiseActionSetPublication()
        {
            confirmedActionSets = LoadedSpellbook;
            if (player is not Player owner) return;
            actionSetAccount = owner.Account; actionSetEpoch = actionSetAccount?.WorldSessionEpoch ?? 0;
        }
        private bool OwnsActionSetPublication() => OwnsAmpPowerPublication()
            && ReferenceEquals(player.Account, actionSetAccount) && actionSetAccount?.WorldSessionEpoch == actionSetEpoch
            && ReferenceEquals(actionSetAccount.Session, ampPowerSession);

        public void QueueActionSetChanges(byte spec, Action apply, IReadOnlyList<uint> selected, bool publishAmps)
        {
            if (player is not Player || !OwnsActionSetPublication() || actionSetCapture != null || spec >= ActionSet.MaxActionSets)
                throw new InvalidOperationException("Public LAS changes require their original World spellbook actor.");
            var request = new ActionSetRequest(spec, publishAmps);
            byte points = actionSets[spec].TierPoints;
            actionSetCapture = request; actionSetCaptureThread = Environment.CurrentManagedThreadId;
            try
            {
                apply();
                foreach (uint id in selected.Where(id => id != 0).Distinct())
                    if (spells.TryGetValue(id, out var spell) && spell.Item != null)
                        request.Items[id] = CloneActionSetItem(spell.Item.Build());
            }
            finally { actionSetCapture = null; actionSetCaptureThread = 0; }
            request.Points |= actionSets[spec].TierPoints != points;
            lock (actionSetPublicationGate)
            {
                request.Sequence = ++actionSetSequence;
                actionSetRequests.Add(request);
            }
            player.RequestSave();
        }

        private void SendActionSetSpellUpdate(uint id, byte tier, byte spec)
        {
            if (actionSetCapture != null && actionSetCaptureThread == Environment.CurrentManagedThreadId)
            { actionSetCapture.Updates.Add(new(id, tier, spec)); return; }
            player.Session.EnqueueMessageEncrypted(new ServerSpellUpdate { Spell4BaseId = id, TierIndex = tier, SpecIndex = spec, Activated = tier > 0 });
        }

        internal void AcknowledgeActionSetPublication(ExperiencePersistence.Spellbook book, long sequence)
        {
            lock (actionSetPublicationGate)
            {
                confirmedActionSets = book;
                var accepted = actionSetRequests.Where(request => request.Sequence <= sequence).ToArray();
                if (accepted.Length == 0) return;
                actionSetPublications.Enqueue(new(book, accepted));
                actionSetRequests.RemoveAll(request => request.Sequence <= sequence);
            }
        }
        internal void NotifyActionSetPublicationReady() => ScheduleActionSetPublication(Task.CompletedTask);
        private void ScheduleActionSetPublication(Task delay)
        {
            if (!OwnsActionSetPublication()) return;
            lock (actionSetPublicationGate)
            {
                if (actionSetSending || actionSetScheduled || actionSetPublications.Count == 0) return;
                actionSetScheduled = true;
            }
            ampPowerEvents.EnqueueThreadSafe(new PredicateEvent(
                () => !OwnsActionSetPublication() || delay.IsCompleted && player is Player { IsNormalSaveInProgress: false }, PublishActionSetChanges));
        }
        private void PublishActionSetChanges()
        {
            lock (actionSetPublicationGate)
            {
                actionSetScheduled = false;
                if (actionSetSending || !OwnsActionSetPublication()) return;
                actionSetSending = true;
            }
            bool retry = false;
            try
            {
                while (OwnsActionSetPublication())
                {
                    ActionSetPublication publication;
                    lock (actionSetPublicationGate)
                        if (!actionSetPublications.TryPeek(out publication)) return;
                    publication.Packets ??= CaptureActionSetPackets(publication);
                    while (publication.Cursor < publication.Packets.Count)
                    {
                        if (!OwnsActionSetPublication()) return;
                        ampPowerSession.EnqueueMessageEncrypted(publication.Packets[publication.Cursor]());
                        publication.Cursor++;
                    }
                    lock (actionSetPublicationGate) actionSetPublications.Dequeue();
                }
            }
            catch (Exception error) { retry = true; log.Warn(error, "Committed LAS publication retains its original packet cursor."); }
            finally
            {
                lock (actionSetPublicationGate) actionSetSending = false;
                ScheduleActionSetPublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }

        private bool DeferActionSetInitial(ActionSetInitial kind)
        {
            if (actionSetCapture != null && actionSetCaptureThread == Environment.CurrentManagedThreadId)
            {
                actionSetCapture.Points |= kind == ActionSetInitial.Points;
                actionSetCapture.Amps |= kind == ActionSetInitial.Amps;
                return true;
            }
            lock (actionSetPublicationGate)
            {
                if (player is not Player || actionSetRequests.Count == 0 && actionSetPublications.Count == 0 && !actionSetSending) return false;
                var book = confirmedActionSets;
                var publication = new ActionSetPublication(book, []) { Packets = [] };
                if (kind == ActionSetInitial.Book) publication.Packets.Add(CaptureAbilityBook(book));
                if (kind == ActionSetInitial.Points) publication.Packets.Add(() => BuildActionSetPoints(book));
                if (kind == ActionSetInitial.Active) publication.Packets.Add(() => new ServerActionSetClearCache());
                if (kind == ActionSetInitial.Active) publication.Packets.Add(() => BuildConfirmedActionSet(book, book.Active));
                for (byte i = 0; i < ActionSet.MaxActionSets; i++)
                {
                    byte spec = i;
                    if (kind == ActionSetInitial.Sets) publication.Packets.Add(() => BuildConfirmedActionSet(book, spec));
                    if (kind == ActionSetInitial.Amps) publication.Packets.Add(() => BuildConfirmedAmps(book, spec));
                }
                actionSetPublications.Enqueue(publication);
            }
            ScheduleActionSetPublication(Task.CompletedTask); return true;
        }

        private List<Func<IWritable>> CaptureActionSetPackets(ActionSetPublication publication)
        {
            var book = publication.Book;
            var packets = new List<Func<IWritable>>();
            foreach (var update in publication.Requests.SelectMany(request => request.Updates).GroupBy(row => (row.Id, row.Spec)).Select(group => group.Last()))
            {
                var learned = book.Learned.SingleOrDefault(row => row.Id == update.Id);
                if (learned == null) continue;
                byte tier = book.Shortcuts.SingleOrDefault(row => row.Spec == update.Spec && row.Type == (byte)ShortcutType.SpellbookItem && row.Object == update.Id)?.Tier ?? learned.Tier;
                packets.Add(() => new ServerSpellUpdate { Spell4BaseId = update.Id, SpecIndex = update.Spec, TierIndex = tier, Activated = tier > 0 });
            }
            packets.Add(CaptureAbilityBook(book));
            packets.Add(() => new ServerActionSetClearCache());
            foreach (var group in publication.Requests.GroupBy(request => request.Spec))
            {
                byte spec = group.Key;
                foreach (uint id in book.Shortcuts.Where(row => row.Spec == spec && row.Location < ClientRequestActionSetChanges.ActionCount && row.Type == (byte)ShortcutType.SpellbookItem).Select(row => row.Object).Distinct())
                {
                    var item = group.Reverse().Select(request => request.Items.GetValueOrDefault(id)).FirstOrDefault(value => value != null)
                        ?? (spells.TryGetValue(id, out var spell) ? spell.Item?.Build() : null);
                    if (item == null) continue;
                    var captured = CloneActionSetItem(item);
                    packets.Add(() => new ServerItemAdd { InventoryItem = new() { Item = CloneActionSetItem(captured), Reason = ItemUpdateReason.NoReason } });
                }
                packets.Add(() => BuildConfirmedActionSet(book, spec));
                if (group.Any(request => request.Points)) packets.Add(() => BuildActionSetPoints(book));
                if (group.Any(request => request.Amps)) packets.Add(() => BuildConfirmedAmps(book, spec));
            }
            return packets;
        }
        private Func<IWritable> CaptureAbilityBook(ExperiencePersistence.Spellbook book)
        {
            var rows = new List<(uint Id, byte Tier, byte Spec)>();
            foreach (var spell in book.Learned)
            {
                var info = GetGlobalSpellManager().GetSpellBaseInfo(spell.Id);
                if (info == null) continue;
                for (byte spec = 0; spec < ActionSet.MaxActionSets; spec++)
                {
                    byte tier = book.Shortcuts.SingleOrDefault(row => row.Spec == spec && row.Type == (byte)ShortcutType.SpellbookItem && row.Object == spell.Id)?.Tier ?? spell.Tier;
                    rows.Add((spell.Id, tier, spec));
                    if (info.SpellType.Id != 5) break;
                }
            }
            return () => new ServerAbilityBook { Spells = rows.Select(row => new ServerAbilityBook.Spell { Spell4BaseId = row.Id, TierIndexAchieved = row.Tier, SpecIndex = row.Spec }).ToList() };
        }
        private static ServerActionSet BuildConfirmedActionSet(ExperiencePersistence.Spellbook book, byte spec)
        {
            var packet = new ServerActionSet { SpecIndex = spec, Unlocked = 1, Result = LimitedActionSetResult.Ok };
            for (ushort i = 0; i < ActionSet.MaxActionCount; i++)
            {
                var shortcut = book.Shortcuts.SingleOrDefault(row => row.Spec == spec && row.Location == i);
                packet.Actions.Add(new() { ShortcutType = shortcut == null ? ShortcutType.None : (ShortcutType)shortcut.Type,
                    ObjectId = shortcut?.Object ?? 0, Location = new() { Location = shortcut == null ? (InventoryLocation)300 : InventoryLocation.Ability, BagIndex = i } });
            }
            return packet;
        }
        private static ServerAmpList BuildConfirmedAmps(ExperiencePersistence.Spellbook book, byte spec)
            => new() { SpecIndex = spec, Amps = book.Amps.Where(row => row.Spec == spec).Select(row => row.Id).ToList() };
        private static ServerAbilityPoints BuildActionSetPoints(ExperiencePersistence.Spellbook book)
        {
            uint total = (uint)(ActionSet.MaxTierPoints + Math.Min(book.Bonus, ActionSet.MaxBonusTierPoints));
            uint spent = (uint)book.Shortcuts.Where(row => row.Spec == book.Active && row.Type == (byte)ShortcutType.SpellbookItem).Sum(row => ActionSet.CalculateTierCost(row.Tier));
            return new() { AbilityPoints = checked(total - spent), TotalAbilityPoints = total };
        }
        private static NetworkItem CloneActionSetItem(NetworkItem value)
        {
            var copy = new NetworkItem { ItemGuid = value.ItemGuid, MakerCharacterId = value.MakerCharacterId, Item2Id = value.Item2Id,
                LocationData = new() { Location = value.LocationData.Location, BagIndex = value.LocationData.BagIndex }, StackCount = value.StackCount,
                Charges = value.Charges, RandomCircuitData = value.RandomCircuitData, RandomGlyphData = value.RandomGlyphData, ThresholdData = value.ThresholdData,
                Durability = value.Durability, Unknown44 = value.Unknown44, Unknown48 = value.Unknown48, DyeData = value.DyeData, DynamicFlags = value.DynamicFlags,
                ExpirationTimeLeft = value.ExpirationTimeLeft, SellPrices = value.SellPrices.Select(price => new NetworkItem.PriceInfo { CostType = price.CostType, Amount = price.Amount, Value = price.Value }).ToArray(),
                PowerCoreItem2Id = value.PowerCoreItem2Id, EffectiveItemLevel = value.EffectiveItemLevel };
            copy.Microchips.AddRange(value.Microchips); copy.Glyphs.AddRange(value.Glyphs);
            copy.TimeLimitedTradingPartners.AddRange(value.TimeLimitedTradingPartners.Select(row => new NetworkItem.TradingPartnerInfo { Unknown0 = row.Unknown0, Unknown8 = row.Unknown8 }));
            return copy;
        }
    }
}
