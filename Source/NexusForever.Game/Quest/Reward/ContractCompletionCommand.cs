using System.Collections.ObjectModel;
using System.Text.Json;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest.Reward
{
    // Scalar persistence input from a retained normal-save completion journal or
    // an independent executor. The actor proves modifier/Quest ownership before
    // constructing it; this command retains no actor or gameplay callbacks.
    internal sealed class ContractCompletionCommand
    {
        internal ulong CharacterId { get; }
        internal string AcceptanceId { get; }
        internal ContractRewardSource Source { get; }
        internal uint Points { get; }
        internal string Evaluation { get; }
        internal ushort RewardChoice { get; }
        internal DateTime CreatedUtc { get; }
        internal IReadOnlyList<CharacterChallengeQuestChange> Changes { get; }
        internal ContractRewardSelection Selection { get; private init; }
        internal ContractTrackState SelectionBaseline { get; private init; }
        private readonly uint[] prerequisites;

        internal ContractCompletionCommand WithSelection(ContractRewardSelection selection, ContractTrackState baseline)
        {
            if (selection == null || selection.TrackId != Source.TrackId || selection.AcceptanceId != AcceptanceId
                || selection.CycleId != (baseline?.CycleId ?? 0) || baseline?.SelectionInitialized == true
                || baseline != null && (baseline.CharacterId != CharacterId || baseline.TrackId != Source.TrackId))
                throw new ArgumentException("Contract selection lost its frozen completion owner.");
            return new(CharacterId, AcceptanceId, Source, Points, Evaluation, RewardChoice, CreatedUtc, Changes, prerequisites)
                { Selection = selection, SelectionBaseline = baseline };
        }

        internal static ContractCompletionCommand FromModifier(ulong characterId, string acceptanceId, ContractRewardSource source,
            ContractRewardModifierSnapshot modifier, ushort rewardChoice, DateTime createdUtc,
            IEnumerable<CharacterChallengeQuestChange> changes, IEnumerable<uint> satisfiedPrerequisites = null)
        {
            ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(modifier);
            if (modifier.TrackId != source.TrackId || modifier.TrackType != source.TrackType || !modifier.TryGetPoints(source.BasePoints, out uint points))
                throw new ArgumentException("Contract evaluation does not belong to this source or has unmapped arithmetic.");
            string evaluation = JsonSerializer.Serialize(new { Policy = "build16042-exact-integral-contract-v1", modifier.TrackId,
                modifier.TrackType, modifier.Multiplier, modifier.Revision, modifier.Sources });
            return new(characterId, acceptanceId, source, points, evaluation, rewardChoice, createdUtc, changes, satisfiedPrerequisites);
        }

        internal ContractCompletionCommand(ulong characterId, string acceptanceId, ContractRewardSource source,
            uint evaluatedPoints, string evaluation, ushort rewardChoice, DateTime createdUtc,
            IEnumerable<CharacterChallengeQuestChange> changes, IEnumerable<uint> satisfiedPrerequisites = null)
        {
            ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(changes);
            var copy = changes.OrderBy(change => change?.QuestId).ToArray();
            prerequisites = satisfiedPrerequisites?.Distinct().Order().ToArray() ?? [];
            if (characterId == 0 || !Quest.ValidContractAcceptanceId(acceptanceId)
                || source.QuestId is 0 or > 0x7FFF || source.PeriodicQuestGroupId == 0 || source.TrackId is 0 or > 0x3FFF
                || source.TrackType > 1 || source.Quality is < 1 or > 4
                || source.TrackPrerequisiteId != 0 && !prerequisites.Contains(source.TrackPrerequisiteId)
                || string.IsNullOrWhiteSpace(evaluation) || evaluation.Length > 512
                || createdUtc.Kind != DateTimeKind.Utc || createdUtc == default
                || copy.Any(change => change == null || change.CharacterId != characterId)
                || copy.Select(change => change.QuestId).Distinct().Count() != copy.Length)
                throw new ArgumentException("Contract completion requires one canonical owner, frozen evaluation and distinct complete Quest graphs.");
            var terminal = copy.SingleOrDefault(change => change.QuestId == source.QuestId);
            if (terminal?.After?.Quest.State != (byte)QuestState.Completed || terminal.After.Quest.ContractAcceptanceId != acceptanceId
                || terminal.Before?.Quest.State == (byte)QuestState.Completed && terminal.Before.Quest.ContractAcceptanceId == acceptanceId)
                throw new ArgumentException("Contract receipt requires a new terminal transition of the exact accepted generation.");
            CharacterId = characterId; AcceptanceId = acceptanceId; Source = source; Points = evaluatedPoints;
            Evaluation = evaluation; RewardChoice = rewardChoice; CreatedUtc = createdUtc;
            Changes = Array.AsReadOnly(copy);
        }

        internal string Payload(QuestTransactionParticipants participants)
        {
            object Snapshot(CharacterChallengeQuestSnapshot value)
            {
                var encoded = participants.EncodeQuest(value);
                return encoded == null ? null : new { encoded.Quest, encoded.Objectives };
            }
            // Time is audit provenance, not part of the idempotency key. A replay
            // retains the original receipt time and cannot replace its evaluation.
            string json = JsonSerializer.Serialize(new
            {
                Version = 1, CharacterId, AcceptanceId, Source, Points, Evaluation, RewardChoice,
                Prerequisites = prerequisites,
                Quests = Changes.Select(change => new { change.QuestId, Before = Snapshot(change.Before), After = Snapshot(change.After) }).ToArray()
            });
            if (Selection == null) return json;
            var payload = System.Text.Json.Nodes.JsonNode.Parse(json).AsObject();
            payload["Selection"] = System.Text.Json.Nodes.JsonNode.Parse(Selection.Json);
            return payload.ToJsonString();
        }
    }

    internal sealed record ContractCompletionReceipt(ulong CharacterId, string AcceptanceId, ushort QuestId,
        uint TrackId, uint BasePoints, uint Points, string PayloadJson, DateTime CreatedUtc);
    internal sealed record ContractRewardSlotState(byte Ordinal, uint RewardRowId, uint RequiredPoints, string SelectionJson, ContractRewardClaimProof Claim = null)
    {
        internal bool Claimed => Claim != null;
    }
    internal sealed record ContractTrackState(ulong CharacterId, uint TrackId, ulong CycleId, uint Points, ulong Revision, bool Active)
    {
        internal bool SelectionInitialized { get; init; }
        internal IReadOnlyList<ContractRewardSlotState> Slots { get; init; } = Array.Empty<ContractRewardSlotState>();
    }
    internal sealed record ContractCompletionStage(bool Applied, ContractCompletionReceipt Receipt, ContractTrackState Track,
        IReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot> Quests);
}
