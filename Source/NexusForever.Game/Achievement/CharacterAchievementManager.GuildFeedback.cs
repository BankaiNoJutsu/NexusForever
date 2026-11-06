using System.Text.Json;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Achievement
{
    public sealed partial class CharacterAchievementManager
    {
        private sealed record ChecklistContribution(uint Bit, FeedbackEligibility Eligibility);
        private sealed record Rule(ushort Cause, AchievementEntry Entry, AchievementChecklistEntry[] Checklist,
            FeedbackEligibility Eligibility, ChecklistContribution[] Bits);
        private sealed record Part(ushort Parent, Rule[] Rules);
        private sealed class Pending(GuildAchievementFeedbackReceipt.Stored receipt)
        {
            internal readonly GuildAchievementFeedbackReceipt.Stored Receipt = receipt;
            internal bool Ready;
        }
        private static readonly JsonSerializerOptions feedbackJson = new() { IncludeFields = true };
        private readonly object feedbackGate = new();
        private readonly Dictionary<string, Pending> pendingFeedback = [];
        private readonly Dictionary<string, GuildAchievementFeedbackReceipt.Stored> seenFeedback = [];
        private uint feedbackAccount;
        private object feedbackAccountOwner, feedbackEpoch;
        private EventQueue feedbackEvents;
        private bool applyingFeedback, recoveringFeedback;
        private double feedbackRecoveryDelay;
        private Rule[] activeFeedbackRules;
        private HashSet<ushort> activeFeedbackCauses, activeFeedbackCompletions;
        internal bool IsApplyingGuildFeedback => applyingFeedback || feedbackEligibilityAssignments != null;
        private bool MatchesFeedbackSession => ReferenceEquals(owner.Account, feedbackAccountOwner)
            && Equals(owner.Account?.WorldSessionEpoch, feedbackEpoch) && owner.Account?.Id == feedbackAccount
            && feedbackEvents != null && ReferenceEquals(publicationSession.Events, feedbackEvents);
        private bool OwnsFeedback => OwnsPublication && MatchesFeedbackSession
            && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);
        private bool OwnsFeedbackSave
        {
            get
            {
                // Original cleanup can finish after registry removal. A different
                // registered actor, session, epoch or manager cannot take over it.
                if (!MatchesFeedbackSession || owner is not Player original || original.CharacterId != AchievementCharacter
                    || publicationIdentity == null || owner.Identity?.Id != publicationIdentity.Id || owner.Identity.RealmId != publicationIdentity.RealmId
                    || !ReferenceEquals(original.AchievementManager, this) || !ReferenceEquals(owner.Session, publicationSession)
                    || publicationSession is not IPlayerSession bound || !ReferenceEquals(bound.Player, owner)) return false;
                IPlayer registered = playerManager?.GetPlayer(publicationIdentity);
                return registered == null || ReferenceEquals(registered, owner);
            }
        }

        private void InitialiseGuildFeedback(uint account)
        {
            feedbackAccount = account; feedbackAccountOwner = owner.Account; feedbackEpoch = owner.Account?.WorldSessionEpoch;
            feedbackEvents = owner.Session?.Events;
        }
        internal string PrepareGuildFeedback(ushort parent)
        {
            if (!OwnsFeedback || AchievementDatabase == null)
                throw new InvalidOperationException("Guild feedback lost its original loaded Character actor.");
            if (feedbackEligibilityAssignments != null) throw new InvalidOperationException("Guild feedback eligibility cannot reenter preparation.");
            var rules = new List<Rule>();
            IAchievementInfo[] catalog = GetGlobalAchievementManager().GetCharacterAchievements(AchievementType.AchievementComplete).ToArray();
            var possibleCompletions = catalog.Where(info => info.IsPlayerAchievement && !base.HasCompletedAchievement(info.Id)).Select(info => info.Id).ToHashSet();
            var causes = new Queue<ushort>(); var visited = new HashSet<ushort>(); causes.Enqueue(parent);
            feedbackEligibilityAssignments = [];
            try
            {
                while (causes.TryDequeue(out ushort cause))
                {
                    if (!visited.Add(cause)) continue;
                    // Even direct siblings can depend on an earlier sibling's
                    // completion in this event. Freeze those causal decisions
                    // together with every other gate on the original actor.
                    feedbackVariableAchievements = possibleCompletions;
                    foreach (IAchievementInfo info in catalog)
                    {
                        if (!info.IsPlayerAchievement || base.HasCompletedAchievement(info.Id) || IsDisabled(DisableType.Achievement, info.Id)) continue;
                        FeedbackEligibility eligibility = null; var bits = new List<ChecklistContribution>();
                        if (info.ChecklistEntries.Count == 0)
                        {
                            if (!MatchesFeedbackObject(info.Entry.ObjectId, info.Entry.ObjectIdAlt, cause)) continue;
                            eligibility = CaptureFeedbackEligibility(() => CanUpdateAchievement(owner, info.Entry, cause, 0));
                            if (!eligibility.CanAccept) continue;
                        }
                        else
                        {
                            foreach (AchievementChecklistEntry entry in info.ChecklistEntries)
                            {
                                if ((entry.ObjectId == 0 && entry.ObjectIdAlt == 0) || !MatchesFeedbackObject(entry.ObjectId, entry.ObjectIdAlt, cause)
                                    || !AchievementProgressRules.TryBuildChecklistBit(entry.Bit, out uint bit)) continue;
                                var gate = CaptureFeedbackEligibility(() => CanUpdateChecklist(owner, entry, cause, 0));
                                if (gate.CanAccept) bits.Add(new(bit, gate));
                            }
                            if (bits.Count == 0) continue;
                        }
                        rules.Add(new(cause, info.Entry, info.ChecklistEntries.ToArray(), eligibility, bits.ToArray()));
                        causes.Enqueue(info.Id);
                    }
                }
            }
            finally { feedbackEligibilityAssignments = null; feedbackVariableAchievements = null; }
            if (rules.Count == 0) return null;
            return JsonSerializer.Serialize(rules, feedbackJson);
        }
        internal GuildAchievementFeedbackReceipt.Stored CreateGuildFeedback(ulong guild, (ushort Parent, string Plan)[] sources)
        {
            var parts = sources.Select(source => new Part(source.Parent, JsonSerializer.Deserialize<Rule[]>(source.Plan, feedbackJson))).ToArray();
            return GuildAchievementFeedbackReceipt.Create(new(GuildAchievementFeedbackReceipt.Protocol, AchievementDatabase,
                AchievementCharacter, feedbackAccount, publicationIdentity.RealmId, guild, parts.Select(part => part.Parent).ToArray(),
                JsonSerializer.Serialize(parts, feedbackJson)));
        }
        internal void EnqueueGuildFeedback(GuildAchievementFeedbackReceipt.Stored receipt)
            => feedbackEvents.EnqueueThreadSafe(new TaskEvent(Task.CompletedTask, () => ApplyGuildFeedback(receipt)));

        internal void ApplyGuildFeedback(GuildAchievementFeedbackReceipt.Stored receipt)
        {
            if (!OwnsFeedback) return; // committed intent remains available to a fresh actor
            GuildAchievementFeedbackReceipt.Intent intent = GuildAchievementFeedbackReceipt.Decode(receipt);
            if (intent.Character != AchievementCharacter || intent.Account != feedbackAccount || intent.Realm != publicationIdentity.RealmId
                || intent.Database != AchievementDatabase)
                throw new InvalidOperationException("Guild feedback changed its captured Character recipient.");
            Part[] parts = JsonSerializer.Deserialize<Part[]>(intent.Plan, feedbackJson)
                ?? throw new InvalidOperationException("Guild feedback lost its accepted contribution plan.");
            if (!parts.Select(part => part.Parent).SequenceEqual(intent.Parents))
                throw new InvalidOperationException("Guild feedback changed its ordered parent completion chain.");
            // Changed metadata remains a recoverable, blocked intent. Never rerun
            // prerequisites on a replacement actor or silently reinterpret its bits.
            foreach (Rule rule in parts.SelectMany(part => part.Rules))
            {
                IAchievementInfo info = GetGlobalAchievementManager().GetAchievement((ushort)rule.Entry.Id);
                if (info == null || !info.IsPlayerAchievement || rule.Entry.Id != info.Id
                    || JsonSerializer.Serialize(new Rule(rule.Cause, info.Entry, info.ChecklistEntries.ToArray(), rule.Eligibility, rule.Bits), feedbackJson)
                        != JsonSerializer.Serialize(rule, feedbackJson))
                    throw new InvalidOperationException("Guild feedback achievement metadata changed after acceptance.");
            }
            Pending pending;
            lock (feedbackGate)
            {
                if (seenFeedback.TryGetValue(receipt.Id, out var retained))
                {
                    if (retained != receipt) throw new InvalidOperationException("Guild feedback has conflicting duplicate deliveries.");
                    return;
                }
                if (applyingFeedback) throw new InvalidOperationException("Guild feedback cannot reenter another contribution.");
                pending = new(receipt); pendingFeedback.Add(receipt.Id, pending); seenFeedback.Add(receipt.Id, receipt); applyingFeedback = true;
            }
            try
            {
                activeFeedbackCauses = []; activeFeedbackCompletions = [];
                foreach (Part part in parts)
                {
                    activeFeedbackRules = part.Rules;
                    ApplyAcceptedGuildFeedbackCause(part.Parent);
                }
                lock (feedbackGate) pending.Ready = true;
            }
            catch
            {
                // A companion callback may fail partway through its live effects.
                // Do not seal a delivery marker or save that partial graph.
                owner.Session.ForceDisconnect(); throw;
            }
            finally
            {
                activeFeedbackRules = null; activeFeedbackCauses = null; activeFeedbackCompletions = null;
                lock (feedbackGate) applyingFeedback = false;
            }
            ((Player)owner).RequestGuildAchievementFeedbackSave();
        }
        private void ApplyAcceptedGuildFeedbackCause(ushort cause)
        {
            if (!activeFeedbackCauses.Add(cause)) return;
            foreach (Rule rule in activeFeedbackRules.Where(rule => rule.Cause == cause))
            {
                ushort id = (ushort)rule.Entry.Id;
                if (HasCompletedAchievement(id)) continue;
                uint mask = 0;
                if (rule.Checklist.Length == 0)
                {
                    if (!EvaluateFeedbackEligibility(rule.Eligibility)) continue;
                }
                else
                {
                    foreach (var bit in rule.Bits)
                        if (EvaluateFeedbackEligibility(bit.Eligibility)) mask |= bit.Bit;
                    if (mask == 0) continue;
                }
                IAchievement row = GetAchievement(id);
                if (rule.Checklist.Length == 0)
                {
                    uint required = AchievementProgressRules.GetRequiredProgress(rule.Entry.RequiredProgress);
                    if (row.ProgressCount < required) row.ProgressCount++;
                }
                else row.CompletedChecklistMask |= mask;
                if (row.IsComplete())
                {
                    activeFeedbackCompletions.Add(id);
                    CompleteAchievement(owner, row);
                }
            }
        }
        internal Action<CharacterContext, ExperiencePersistence.Achievement[]> CaptureGuildFeedbackSave()
        {
            GuildAchievementFeedbackReceipt.Stored[] rows;
            lock (feedbackGate)
            {
                if (applyingFeedback || pendingFeedback.Values.Any(row => !row.Ready))
                    throw new InvalidOperationException("Guild feedback cannot save a partially applied contribution.");
                rows = pendingFeedback.Values.Select(row => row.Receipt).ToArray();
            }
            if (rows.Length == 0) return null;
            if (!OwnsFeedbackSave) throw new InvalidOperationException("Guild feedback lost its original Character save actor.");
            return (context, achievements) =>
            {
                void RequireActor()
                {
                    if (!OwnsFeedbackSave) throw new InvalidOperationException("Guild feedback lost its original Character transaction actor.");
                }
                try
                {
                    RequireActor();
                    context.OnSaveValidation(_ => RequireActor());
                    foreach (var row in rows)
                        GuildAchievementFeedbackReceipt.StageDelivery(context, row, AchievementCharacter, feedbackAccount, achievements, () =>
                        {
                            lock (feedbackGate) pendingFeedback.Remove(row.Id);
                        });
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            };
        }
        internal void RequireFrozenGuildFeedbackSave()
        {
            lock (feedbackGate)
                if (pendingFeedback.Count != 0)
                    throw new InvalidOperationException("Guild feedback requires its frozen ordinary Character save.");
        }
        internal void PumpGuildFeedbackRecovery(double seconds)
        {
            feedbackRecoveryDelay -= seconds;
            if (recoveringFeedback || feedbackRecoveryDelay > 0 || !OwnsFeedback || AchievementDatabase == null) return;
            feedbackRecoveryDelay = 30; recoveringFeedback = true;
            var database = ((Player)owner).GetCharacterDatabase();
            ulong character = AchievementCharacter; uint account = feedbackAccount; ushort realm = publicationIdentity.RealmId;
            string route = AchievementDatabase;
            var task = Task.Run(() => GuildAchievementFeedbackReceipt.ReadPending(database.CreateContext, character, account, realm, route));
            feedbackEvents.EnqueueThreadSafe(new TaskEvent(task, () =>
            {
                recoveringFeedback = false;
                if (!OwnsFeedback) return;
                foreach (var row in task.Result) ApplyGuildFeedback(row);
            }, () =>
            {
                recoveringFeedback = false;
                publicationLog.Error(task.Exception, "Guild achievement feedback recovery could not read its committed intents.");
            }));
        }
    }
}
