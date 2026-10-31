using System.Collections.ObjectModel;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;

namespace NexusForever.Game.Challenges.Reward
{
    public enum CharacterChallengeRewardCommandKind { Load, Activate, Medal, Finish, Choose, Claim, LegacyAdvance, ChooseContract, ClaimContract }

    /// <summary>Actor-captured values only; no mutable player/prerequisite callback runs on the SQL worker.</summary>
    public sealed class CharacterChallengeRewardCommand
    {
        private readonly HashSet<uint> prerequisites;
        public CharacterChallengeRewardCommandKind Kind { get; }
        public ulong CharacterId { get; }
        internal uint? ExpectedAccountId { get; private init; }
        public CharacterChallengeState Before { get; }
        public CharacterChallengeState After { get; }
        public string Identity { get; }
        public uint TrackId { get; }
        public ulong CycleId { get; }
        public ulong Revision { get; }
        public uint RewardRowId { get; }
        public uint Choice { get; }
        public byte Medal { get; }
        public ChallengeRewardModifierSnapshot Modifier { get; }
        public DateTime CreatedUtc { get; }
        public bool EnableCharacterTracks { get; }
        public IReadOnlyDictionary<uint, ulong> Versions { get; }
        internal IReadOnlyList<CharacterChallengeQuestChange> QuestChanges { get; }
        internal CharacterChallengeState LegacyPredecessor { get; }
        internal ContractRewardCatalog ContractCatalog { get; private init; }
        internal IReadOnlyList<ContractTrackState> ContractBefore { get; private init; }
        internal ContractRewardSelection ContractSelection { get; private init; }
        internal ContractRewardAwardCatalog ContractAwards { get; private init; }
        internal ContractRewardClaimQuote ContractQuote { get; private init; }
        internal ContractCurrencyBefore ContractCurrencyBefore { get; private init; }
        internal uint ContractAccountId { get; private init; }
        internal ushort ContractRealmId { get; private init; }

        private CharacterChallengeRewardCommand(CharacterChallengeRewardCommandKind kind, ulong character,
            IEnumerable<uint> satisfied, CharacterChallengeState before = null, CharacterChallengeState after = null,
            string identity = null, uint track = 0, ulong cycle = 0, ulong revision = 0, uint row = 0, uint choice = 0,
            byte medal = 0, ChallengeRewardModifierSnapshot modifier = default, DateTime created = default,
            bool enabled = false, IReadOnlyDictionary<uint, ulong> versions = null,
            IEnumerable<CharacterChallengeQuestChange> questChanges = null, CharacterChallengeState legacyPredecessor = null)
        {
            if (character == 0) throw new ArgumentOutOfRangeException(nameof(character));
            Kind = kind; CharacterId = character; Before = before; After = after; Identity = identity;
            TrackId = track; CycleId = cycle; Revision = revision; RewardRowId = row; Choice = choice;
            Medal = medal; Modifier = modifier; CreatedUtc = created; EnableCharacterTracks = enabled;
            prerequisites = satisfied?.ToHashSet() ?? [];
            Versions = new ReadOnlyDictionary<uint, ulong>(versions?.ToDictionary(row => row.Key, row => row.Value) ?? []);
            QuestChanges = Array.AsReadOnly(questChanges?.ToArray() ?? []);
            LegacyPredecessor = legacyPredecessor;
        }

        public static CharacterChallengeRewardCommand Load(ulong character, IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Load, character, satisfied);

        internal static CharacterChallengeRewardCommand LoadForAccount(ulong character, uint account)
            => account != 0 ? new(CharacterChallengeRewardCommandKind.Load, character, null) { ExpectedAccountId = account }
                : throw new ArgumentException("An account-owned load requires its original account.");
        public static CharacterChallengeRewardCommand Activate(CharacterChallengeState before, CharacterChallengeState after,
            string attempt, bool enabled, IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Activate, (after ?? throw new ArgumentNullException(nameof(after))).CharacterId,
                satisfied, before, after, attempt, enabled: enabled);
        internal static CharacterChallengeRewardCommand ActivateFromLegacy(CharacterChallengeState before, CharacterChallengeState legacy,
            CharacterChallengeState after, string attempt, bool enabled, IEnumerable<uint> satisfied)
            => new(CharacterChallengeRewardCommandKind.Activate, after.CharacterId, satisfied, before, after, attempt,
                enabled: enabled, legacyPredecessor: legacy);
        public static CharacterChallengeRewardCommand Credit(CharacterChallengeState before, CharacterChallengeState after,
            byte medal, ChallengeRewardModifierSnapshot modifier, DateTime created, IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Medal, (before ?? throw new ArgumentNullException(nameof(before))).CharacterId,
                satisfied, before, after, before.AttemptId, medal: medal, modifier: modifier, created: created);
        public static CharacterChallengeRewardCommand Finish(CharacterChallengeState before, CharacterChallengeState after,
            IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Finish, (before ?? throw new ArgumentNullException(nameof(before))).CharacterId,
                satisfied, before, after, before.AttemptId);
        public static CharacterChallengeRewardCommand AdvanceLegacy(CharacterChallengeState before, CharacterChallengeState after, byte medal)
            => new(CharacterChallengeRewardCommandKind.LegacyAdvance, (before ?? throw new ArgumentNullException(nameof(before))).CharacterId,
                null, before, after, before.AttemptId, medal: medal);
        public static CharacterChallengeRewardCommand Choose(ulong character, uint track, IReadOnlyDictionary<uint, ulong> versions,
            IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Choose, character, satisfied, track: track, versions: versions);
        public static CharacterChallengeRewardCommand Claim(ulong character, uint track, ulong cycle, ulong revision, uint row,
            uint choice, string claim, DateTime created, IEnumerable<uint> satisfied = null)
            => new(CharacterChallengeRewardCommandKind.Claim, character, satisfied, identity: claim, track: track, cycle: cycle,
                revision: revision, row: row, choice: choice, created: created);

        internal bool Meets(uint id) => id == 0 || prerequisites.Contains(id);
        internal static CharacterChallengeRewardCommand ClaimContract(ulong character, uint account, ushort realm,
            string identity, DateTime created, ContractRewardAwardCatalog awards, IReadOnlyList<ContractTrackState> before,
            ContractRewardClaimQuote quote, ContractCurrencyBefore currencyBefore, IEnumerable<uint> satisfied = null)
        {
            ArgumentNullException.ThrowIfNull(awards); ArgumentNullException.ThrowIfNull(quote);
            if (account == 0 || realm == 0 || !Guid.TryParseExact(identity, "N", out var guid) || guid == Guid.Empty || created == default || created.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Contract claim requires its retained account/realm/operation identity.");
            var frozen = ContractRewardSnapshot.Validate(before, character, awards.Catalog);
            var canonical = ContractRewardClaimQuote.Capture(awards, character, frozen, quote.Award.TrackId, quote.CycleId, quote.Revision, quote.Award.RewardRowId, quote.Award.Choice);
            if (canonical.Json != quote.Json || (quote.Award.CharacterCurrency == null) != (currencyBefore == null))
                throw new ArgumentException("Contract claim quote or wallet owner differs from its frozen baseline.");
            if (currencyBefore != null) ContractCurrencyDelivery.Capture(quote.Award.CharacterCurrency, currencyBefore);
            return new(CharacterChallengeRewardCommandKind.ClaimContract, character, satisfied, identity: identity, track: quote.Award.TrackId,
                cycle: quote.CycleId, revision: quote.Revision, row: quote.Award.RewardRowId, choice: quote.Award.Choice, created: created)
            { ContractCatalog = awards.Catalog, ContractAwards = awards, ContractBefore = frozen, ContractQuote = quote,
                ContractCurrencyBefore = currencyBefore, ContractAccountId = account, ContractRealmId = realm };
        }
        internal static CharacterChallengeRewardCommand ChooseContract(ulong character, uint track, string operation,
            ContractRewardCatalog catalog, IReadOnlyList<ContractTrackState> before, ContractRewardSelection selection, IEnumerable<uint> satisfied)
        {
            if (!Guid.TryParseExact(operation, "N", out var identity) || identity == Guid.Empty) throw new ArgumentException("Invalid Contract choose identity.");
            var frozen = ContractRewardSnapshot.Validate(before, character, catalog);
            var target = frozen.SingleOrDefault(row => row.TrackId == track) ?? throw new ArgumentException("Contract choose requires its advertised track.");
            if (catalog.GetTrack(track) == null || target.SelectionInitialized == (selection != null)
                || selection != null && (selection.OperationId != operation || selection.AcceptanceId != null || selection.TrackId != track || selection.CycleId != target.CycleId))
                throw new ArgumentException("Contract choose lost its frozen selected-cycle owner.");
            return new(CharacterChallengeRewardCommandKind.ChooseContract, character, satisfied, identity: operation, track: track)
                { ContractCatalog = catalog, ContractBefore = frozen, ContractSelection = selection };
        }
        internal CharacterChallengeRewardCommand WithQuestChanges(IEnumerable<CharacterChallengeQuestChange> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);
            var copied = changes.ToArray();
            if (Kind is not (CharacterChallengeRewardCommandKind.Medal or CharacterChallengeRewardCommandKind.Finish or CharacterChallengeRewardCommandKind.LegacyAdvance)
                || Before?.Activated != true || After == null || After.Activated || Before.CharacterId != CharacterId
                || After.CharacterId != CharacterId || Before.ChallengeId != After.ChallengeId
                || After.Completions != checked(Before.Completions + 1)
                || copied.Any(row => row == null || row.CharacterId != CharacterId)
                || copied.Select(row => row.QuestId).Distinct().Count() != copied.Length)
                throw new ArgumentException("Quest participants require one terminal completion and distinct character-owned graphs.");
            return new(Kind, CharacterId, prerequisites, Before, After, Identity, TrackId, CycleId, Revision,
                RewardRowId, Choice, Medal, Modifier, CreatedUtc, EnableCharacterTracks, Versions, copied, LegacyPredecessor);
        }

        internal object Stage(CharacterChallengeRewardRepository repository, IInventoryRewardReservation inventory)
        {
            repository.BindQuestChanges(QuestChanges);
            return Kind switch
            {
                CharacterChallengeRewardCommandKind.Load => repository.LoadState(),
                CharacterChallengeRewardCommandKind.Activate => LegacyPredecessor == null
                    ? repository.BeginAttempt(Before, After, Identity, EnableCharacterTracks)
                    : repository.BeginAttemptFromLegacy(Before, LegacyPredecessor, After, Identity, EnableCharacterTracks),
                CharacterChallengeRewardCommandKind.Medal => repository.CreditMedal(Identity, Before.ChallengeId, Medal, Modifier, Before, After, CreatedUtc),
                CharacterChallengeRewardCommandKind.Finish => repository.FinishAttempt(Identity, Before.ChallengeId, Before, After),
                CharacterChallengeRewardCommandKind.LegacyAdvance => repository.AdvanceLegacy(Identity, Before.ChallengeId, Medal, Before, After),
                CharacterChallengeRewardCommandKind.Choose => repository.ChooseTrack(TrackId, Versions),
                CharacterChallengeRewardCommandKind.Claim => repository.Claim(TrackId, CycleId, Revision, RewardRowId, Choice, Identity, CreatedUtc, inventory),
                CharacterChallengeRewardCommandKind.ChooseContract => inventory == null
                    ? repository.ChooseContractTrack(TrackId, Identity, ContractCatalog, ContractBefore, ContractSelection)
                    : throw new InvalidOperationException("Contract choice cannot enroll an inventory delivery."),
                CharacterChallengeRewardCommandKind.ClaimContract => repository.ClaimContract(this, inventory),
                _ => throw new InvalidOperationException("Unsupported Character reward command.")
            };
        }
    }

    public enum CharacterChallengeRewardExecutionOutcome { Committed, Failed, Conflict, Uncertain }
    public sealed record CharacterChallengeRewardExecution(CharacterChallengeRewardExecutionOutcome Outcome,
        object Operation, CharacterChallengeRewardsState State, bool Changed, bool Recovered, bool OriginalUnitEnded,
        Exception Error = null)
    {
        internal IReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot> QuestStates { get; init; }
            = new ReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot>(new Dictionary<ushort, CharacterChallengeQuestSnapshot>());
    }
}
