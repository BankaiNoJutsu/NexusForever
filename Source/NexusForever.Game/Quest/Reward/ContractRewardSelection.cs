using System.Security.Cryptography;
using System.Text.Json;

namespace NexusForever.Game.Quest.Reward
{
    // This is an explicit server sampling policy, not a retail RNG claim. The
    // actor supplies an evaluated prerequisite set before ordinary callbacks.
    internal sealed class ContractRewardSelection
    {
        private const string Policy = "contract-eligible-uniform-v1";
        private sealed record Payload(uint Version, string Policy, uint TrackId, ulong CycleId, string AcceptanceId, uint[] Satisfied, uint[] Rewards);
        private sealed record ChoicePayload(uint Version, string Policy, uint TrackId, ulong CycleId, string OperationId, uint[] Satisfied, uint[] Rewards);
        internal uint TrackId { get; }
        internal ulong CycleId { get; }
        internal string AcceptanceId { get; }
        internal string OperationId { get; }
        internal string Json { get; }
        internal IReadOnlyList<uint> Rewards { get; }

        private ContractRewardSelection(Payload payload)
        {
            TrackId = payload.TrackId; CycleId = payload.CycleId; AcceptanceId = payload.AcceptanceId;
            Rewards = Array.AsReadOnly(payload.Rewards.ToArray()); Json = JsonSerializer.Serialize(payload);
        }

        private ContractRewardSelection(ChoicePayload payload)
        {
            TrackId = payload.TrackId; CycleId = payload.CycleId; OperationId = payload.OperationId;
            Rewards = Array.AsReadOnly(payload.Rewards.ToArray()); Json = JsonSerializer.Serialize(payload);
        }

        internal static ContractRewardSelection CaptureForChoose(ContractRewardCatalog catalog, uint trackId, ulong cycleId,
            string operationId, IEnumerable<uint> satisfiedPrerequisites, Func<int, int> select = null)
        {
            if (!Guid.TryParseExact(operationId, "N", out var operation) || operation == Guid.Empty)
                throw new ArgumentException("Choose selection requires its retained operation identity.");
            // Reuse the same evaluated pool/sampling policy, but do not invent a
            // Quest acceptance. Version2 explicitly records the choose operation.
            var sample = Capture(catalog, trackId, cycleId, operationId, satisfiedPrerequisites, select);
            var captured = JsonSerializer.Deserialize<Payload>(sample.Json);
            return new(new ChoicePayload(2, Policy, trackId, cycleId, operationId, captured.Satisfied, captured.Rewards));
        }

        internal static ContractRewardSelection Capture(ContractRewardCatalog catalog, uint trackId, ulong cycleId,
            string acceptanceId, IEnumerable<uint> satisfiedPrerequisites, Func<int, int> select = null)
        {
            ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(satisfiedPrerequisites);
            if (!Quest.ValidContractAcceptanceId(acceptanceId)) throw new ArgumentException("Selection requires its accepted generation.");
            var definition = catalog.GetTrack(trackId) ?? throw new ArgumentException("Unknown Contract selection owner.");
            var satisfied = satisfiedPrerequisites.Where(id => id != 0).Distinct().Order().ToArray();
            var set = satisfied.ToHashSet(); var rows = new List<uint>();
            select ??= RandomNumberGenerator.GetInt32;
            foreach (var slot in definition.Slots)
            {
                var eligible = catalog.EligibleCandidates(trackId, slot.Ordinal, set);
                if (eligible.Count == 0) throw new InvalidOperationException("No eligible Contract reward for a required ordinal.");
                int index = select(eligible.Count);
                if (index < 0 || index >= eligible.Count) throw new InvalidOperationException("Contract selection index is outside its eligible pool.");
                rows.Add(eligible[index].Id);
            }
            return new(new Payload(1, Policy, trackId, cycleId, acceptanceId, satisfied, rows.ToArray()));
        }

        internal static ContractRewardSelection Restore(ContractRewardCatalog catalog, string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 65536) throw new InvalidOperationException("Contract selection provenance is missing or oversized.");
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Version", out var version) && version.TryGetUInt32(out uint number) && number == 2)
            {
                var choice = JsonSerializer.Deserialize<ChoicePayload>(json);
                if (choice == null || !Guid.TryParseExact(choice.OperationId, "N", out var operation) || operation == Guid.Empty)
                    throw new InvalidOperationException("Choose selection lost its operation identity.");
                var legacy = new Payload(1, choice.Policy, choice.TrackId, choice.CycleId, choice.OperationId, choice.Satisfied, choice.Rewards);
                // The shared validation path proves every ordinal and frozen
                // prerequisite without changing the persisted v1 representation.
                Restore(catalog, JsonSerializer.Serialize(legacy));
                var restoredChoice = new ContractRewardSelection(choice);
                if (restoredChoice.Json != json) throw new InvalidOperationException("Choose selection provenance is not canonical.");
                return restoredChoice;
            }
            var payload = JsonSerializer.Deserialize<Payload>(json) ?? throw new InvalidOperationException("Missing Contract selection payload.");
            var definition = catalog?.GetTrack(payload.TrackId);
            if (payload.Version != 1 || payload.Policy != Policy || definition == null || !Quest.ValidContractAcceptanceId(payload.AcceptanceId)
                || payload.Satisfied == null || payload.Rewards == null || payload.Rewards.Length != definition.Slots.Count
                || payload.Satisfied.Any(id => id == 0) || !payload.Satisfied.SequenceEqual(payload.Satisfied.Distinct().Order()))
                throw new InvalidOperationException("Contract selection provenance is invalid.");
            var set = payload.Satisfied.ToHashSet();
            foreach (var slot in definition.Slots)
                if (!catalog.EligibleCandidates(payload.TrackId, slot.Ordinal, set).Any(row => row.Id == payload.Rewards[slot.Ordinal]))
                    throw new InvalidOperationException("Persisted Contract reward is outside its captured eligible ordinal.");
            var restored = new ContractRewardSelection(payload);
            if (restored.Json != json) throw new InvalidOperationException("Contract selection provenance is not canonical.");
            return restored;
        }
    }
}
