using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private ContractCompanion contractCompanion;
        private bool CapturingContractCompanion => contractCompanion is { Captured: false };
        internal void BeginContractCompanion(object token)
        {
            lock (persistenceGate)
            {
                if (contractCompanion != null || !CharacterRewardsReady || activeSave != null || activeRewardLease != null || rewardQueueRequests != 0)
                    throw new InvalidOperationException("Contract companion requires the current idle Challenge owner.");
                var unit = new ChallengeSaveUnit { Key = (NormalSaveIdentity.PersistenceDatabase, characterId) };
                lock (saveLeaseGate)
                {
                    if (saveLeases.ContainsKey(unit.Key)) throw new InvalidOperationException("Another Challenge owner already holds this Contract companion.");
                    saveLeases.Add(unit.Key, unit);
                }
                activeSave = unit;
                contractCompanion = new(this, token, unit);
            }
        }
        internal void AbortContractCompanion(object token)
        {
            lock (persistenceGate)
                if (contractCompanion is { Captured: false } current && ReferenceEquals(current.Token, token))
                {
                    if (ReferenceEquals(activeSave, current.Lease) && activeSave.Context == null)
                    {
                        lock (saveLeaseGate) if (saveLeases.TryGetValue(activeSave.Key, out var lease) && ReferenceEquals(lease, activeSave)) saveLeases.Remove(activeSave.Key);
                        activeSave = null;
                    }
                    contractCompanion = null; rewardPersistenceRequiresReload = true;
                }
        }

        internal sealed class ContractCompanion(ChallengeManager owner, object token, object lease)
        {
            internal object Token { get; } = token;
            internal object Lease { get; } = lease;
            internal bool Captured;
            private readonly List<CharacterChallengeRewardCommand> commands = [];
            private readonly List<Action> afterCommit = [];
            private readonly Dictionary<ushort, CharacterChallengeState> expected = [];
            private CharacterContext context;
            private CharacterChallengeRewardsState candidate;
            private bool staged, ended;
            internal Dictionary<ushort, CharacterChallengeModel> SqlBaselines { get; } = [];
            internal CharacterChallengeState Expected(ushort id)
            {
                if (!expected.TryGetValue(id, out var value))
                {
                    value = CharacterChallengeState.FromModel(owner.persistenceBaselines.GetValueOrDefault(id));
                    expected.Add(id, value);
                }
                return value;
            }
            internal void Add(CharacterChallengeRewardCommand command, Action publish = null)
            {
                if (Captured || ended || !ReferenceEquals(owner.contractCompanion, this) || command.CharacterId != owner.characterId
                    || command.After == null || command.Before != Expected(command.After.ChallengeId))
                    throw new InvalidOperationException("Contract Challenge command lost its captured predecessor.");
                commands.Add(command); expected[command.After.ChallengeId] = command.After;
                if (publish != null) afterCommit.Add(publish);
            }
            internal void Stage(CharacterContext parent)
            {
                if (staged || ended || !Captured || !ReferenceEquals(owner.contractCompanion, this)
                    || parent.AtomicCharacterId != owner.characterId) throw new InvalidOperationException("Contract Challenge unit requires its single atomic parent.");
                staged = true; context = parent;
                try
                {
                    foreach (var command in commands)
                    {
                        using var step = parent.CreateTransactionParticipant();
                        var repository = new CharacterChallengeRewardRepository(step, owner.characterId, owner.characterRewardCatalog, owner.gameTableManager, command.Meets);
                        command.Stage(repository, null);
                        if (!step.ChangeTracker.HasChanges()) throw new InvalidOperationException("Contract Challenge gameplay cannot replay an already applied operation.");
                        step.SaveChanges();
                    }
                    foreach (ushort id in expected.Keys)
                    {
                        var row = parent.CharacterChallenge.AsNoTracking().SingleOrDefault(row => row.Id == owner.characterId && row.ChallengeId == id);
                        if (CharacterChallengeState.FromModel(row) != expected[id]) throw new InvalidOperationException("Contract Challenge SQL differs from its final command.");
                        SqlBaselines.Add(id, row);
                    }
                    parent.ProtectTransactionParticipantWrites(typeof(CharacterChallengeModel));
                    if (commands.Count != 0) parent.OnSaveValidation(phase =>
                    {
                        if (phase != CharacterSaveValidationPhase.BeforeExplicitCommit) return;
                        using var reader = parent.CreateTransactionParticipant(readOnly: true);
                        var repository = new CharacterChallengeRewardRepository(reader, owner.characterId, owner.characterRewardCatalog, owner.gameTableManager, _ => false);
                        candidate = repository.ReadStateForExecution();
                    });
                }
                catch (Exception error) { parent.RejectSave(error); throw; }
            }
            internal bool Finish(bool success)
            {
                if (ended) return false; ended = true;
                bool accepted = success && staged && ReferenceEquals(owner.contractCompanion, this) && (commands.Count == 0 || candidate != null);
                if (ReferenceEquals(owner.contractCompanion, this)) owner.contractCompanion = null;
                if (!accepted) { owner.RequireRewardReload(); return false; }
                foreach (var command in commands.Where(command => command.Kind == CharacterChallengeRewardCommandKind.Activate)) owner.provisionalAttempts.Remove(command.Identity);
                if (candidate != null)
                {
                    owner.committedCharacterRewards = candidate;
                    owner.PublishWorldReadiness();
                    if (owner.characterRewardPacketsRequested) owner.PublishCharacterRewards(reload: false);
                }
                foreach (Action publish in afterCommit) publish();
                return true;
            }
            internal void Abandon() { if (!ended) Finish(false); }
        }

        private bool CaptureContractTransition(ChallengeRuntimeState state, byte? medal, bool completion,
            Abstract.Account.Reward.ChallengeRewardModifierSnapshot modifier, Action afterCommit)
        {
            var mode = ProducerMode(state); var after = CharacterChallengeState.FromModel(BuildModel(state));
            var before = contractCompanion.Expected(after.ChallengeId);
            contractCompanion.Add(medal.HasValue
                ? mode.Mode == CharacterChallengeRewardMode.CharacterTrack
                    ? CharacterChallengeRewardCommand.Credit(before, after, medal.Value, modifier, DateTime.UtcNow)
                    : CharacterChallengeRewardCommand.AdvanceLegacy(before, after, medal.Value)
                : CharacterChallengeRewardCommand.Finish(before, after), afterCommit);
            // Reserve the transition before invoking reentrant callbacks. Every
            // ordinary item/Quest effect joins the enclosing frozen normal save.
            if (completion)
            {
                if (mode.Mode == CharacterChallengeRewardMode.LegacyDirect) GrantRewardTrackItem(gameTableManager.Challenge.GetEntry(state.ChallengeId), state.LastRewardTier);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1);
            }
            return true;
        }
    }
}
