using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        internal RewardPersistenceIdentity NormalSaveIdentity => rewardQueue?.CapturedIdentity;

        internal NormalSaveSnapshot CaptureNormalSave(RewardPersistenceIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);
            lock (persistenceGate)
            {
                bool companionLease = contractCompanion is { Captured: false } && ReferenceEquals(activeSave, contractCompanion.Lease)
                    && activeSave?.Context == null && activeSave?.Frozen == null;
                if (activeSave != null && !companionLease || rewardPersistenceRequiresReload || activeRewardLease != null || rewardQueueRequests != 0)
                    throw new InvalidOperationException("Challenge owner already has persistence work.");
                var unit = companionLease ? activeSave : new ChallengeSaveUnit { Key = (identity.PersistenceDatabase, characterId) };
                if (unit.Key != (identity.PersistenceDatabase, characterId)) throw new InvalidOperationException("Contract companion changed its persistence identity.");
                if (!companionLease) lock (saveLeaseGate)
                {
                    if (saveLeases.ContainsKey(unit.Key)) throw new InvalidOperationException("Another Challenge owner has persistence work.");
                    saveLeases.Add(unit.Key, unit);
                }
                try
                {
                    var rows = new Dictionary<ushort, CharacterChallengeModel>();
                    foreach (var pair in activeChallenges)
                    {
                        var value = BuildModel(pair.Value);
                        if (dirtyChallenges.ContainsKey(pair.Key) || uncertainPersistenceStates.ContainsKey(pair.Key)
                            || !SameChallengeState(persistenceBaselines.GetValueOrDefault(pair.Key), value)) rows.Add(pair.Key, value);
                    }
                    var companion = contractCompanion;
                    if (companion != null)
                    {
                        if (companion.Captured) throw new InvalidOperationException("Contract Challenge companion already captured.");
                        companion.Captured = true;
                    }
                    var snapshot = new NormalSaveSnapshot(this, unit, rows, new(dirtyChallenges), companion);
                    unit.Frozen = snapshot; activeSave = unit; return snapshot;
                }
                catch
                {
                    lock (saveLeaseGate) if (saveLeases.TryGetValue(unit.Key, out var current) && ReferenceEquals(current, unit)) saveLeases.Remove(unit.Key);
                    if (ReferenceEquals(activeSave, unit)) activeSave = null;
                    throw;
                }
            }
        }

        internal sealed class NormalSaveSnapshot(ChallengeManager owner, ChallengeSaveUnit unit,
            Dictionary<ushort, CharacterChallengeModel> rows, Dictionary<ushort, long> revisions, ContractCompanion companion) : IDisposable
        {
            internal Dictionary<ushort, CharacterChallengeModel> Rows { get; } = rows;
            internal Dictionary<ushort, long> Revisions { get; } = revisions;
            private bool disposed;
            private CharacterContext attempted;
            internal bool HasContractCompanion => companion != null;
            internal bool RetainBidRollback { get; set; }
            internal bool CanRetryBidRollback(CharacterContext context, bool partialEnrollment = false) => !disposed && companion == null && RetainBidRollback
                && (ReferenceEquals(attempted, context) || partialEnrollment && attempted == null)
                && (context == null || context.IsProvenBidRollback) && !owner.rewardPersistenceRequiresReload
                && ReferenceEquals(owner.activeSave, unit) && unit.Context == null;
            internal void RetryBidRollback(CharacterContext context, bool partialEnrollment = false)
            {
                if (!CanRetryBidRollback(context, partialEnrollment)) throw new InvalidOperationException("Frozen Challenge save lost its proven bid rollback.");
                attempted = null;
            }
            internal Dictionary<ushort, CharacterChallengeModel> ComposedBaselines => companion?.SqlBaselines;
            internal void BeginContractSave(CharacterContext context) => companion?.Stage(context);
            internal bool FinishContract(bool success) => companion?.Finish(success) ?? success;
            internal void Save(CharacterContext context)
            {
                lock (owner.persistenceGate)
                {
                    try
                    {
                        if (disposed || !ReferenceEquals(owner.activeSave, unit) || unit.Key != (ChallengeSaveDatabaseKey(context), owner.characterId))
                            throw new InvalidOperationException("Frozen Challenge save lost its database/owner.");
                        attempted = context;
                        owner.SaveChallengePersistence(context, this);
                    }
                    catch (Exception error) { context.RejectSave(error); throw; }
                }
            }
            public void Dispose()
            {
                lock (owner.persistenceGate)
                {
                    if (disposed) return; disposed = true;
                    companion?.Abandon();
                    if (!ReferenceEquals(owner.activeSave, unit)) return;
                    if (unit.Context != null)
                    {
                        // An unfinished real context keeps its ordinary lease;
                        // disposing a snapshot cannot invent terminal SQL proof.
                        owner.rewardPersistenceRequiresReload = true; return;
                    }
                    owner.activeSave = null;
                    lock (saveLeaseGate) if (saveLeases.TryGetValue(unit.Key, out var current) && ReferenceEquals(current, unit)) saveLeases.Remove(unit.Key);
                }
            }
        }
    }
}
