using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NLog;

namespace NexusForever.Game.Reward
{
    /// <summary>
    /// Maintains immutable contributions for one character actor. The full reload
    /// must include every expected owner; readiness differs from loaded-empty.
    /// </summary>
    public sealed class RewardTrackPresentation(IPlayer actor) : IRewardTrackPresentation
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private readonly object gate = new();
        private RewardTrack[] accountRows = [], characterRows = [], contractRows = [];
        private bool accountReady, characterReady, characterExpected, contractReady, contractExpected, invalidated;
        private IGameSession boundSession;
        private IAccount boundAccount;
        private ulong boundCharacter;
        private object characterSnapshot, publishedCharacterSnapshot;
        private object contractSnapshot, publishedContractSnapshot;
        private enum Owner { Account, Character, Contract }
        private long sentReloadSequence;

        public bool IsCurrent { get { lock (gate) return CurrentActor(); } }

        public void ExpectCharacterTracks()
        {
            lock (gate)
                if (!invalidated) characterExpected = true;
        }

        public bool PublishAccountTracks(IReadOnlyList<RewardTrack> tracks, bool reload = true)
            => Publish(tracks, Owner.Account, reload);

        public bool PublishCharacterTracks(IReadOnlyList<RewardTrack> tracks, bool reload = true)
            => Publish(tracks, Owner.Character, reload);

        public bool PublishCharacterTracks(IReadOnlyList<RewardTrack> tracks, object snapshot, bool reload = true)
            => snapshot != null && Publish(tracks, Owner.Character, reload, snapshot);

        public void ExpectContractTracks()
        {
            lock (gate)
                if (!invalidated) contractExpected = true;
        }

        public bool PublishContractTracks(IReadOnlyList<RewardTrack> tracks, object snapshot, bool reload = true)
            => snapshot != null && Publish(tracks, Owner.Contract, reload, snapshot);

        public bool IsPublishedContractSnapshot(object snapshot)
        {
            lock (gate) return snapshot != null && CurrentActor() && ReferenceEquals(snapshot, publishedContractSnapshot);
        }

        public bool IsPublishedCharacterSnapshot(object snapshot)
        {
            lock (gate) return snapshot != null && CurrentActor() && ReferenceEquals(snapshot, publishedCharacterSnapshot);
        }

        private bool Publish(IReadOnlyList<RewardTrack> tracks, Owner owner, bool reload, object snapshot = null)
        {
            lock (gate)
            {
                if (!CurrentActor() || !TryCopy(tracks, owner == Owner.Contract ? 2 : 1, out RewardTrack[] copy))
                    return false;
                RewardTrack[] other = (owner == Owner.Account ? characterRows.Concat(contractRows)
                    : owner == Owner.Character ? accountRows.Concat(contractRows) : accountRows.Concat(characterRows)).ToArray();
                var ids = other.Select(row => row.RewardTrackId).ToHashSet();
                if (copy.Any(row => ids.Contains(row.RewardTrackId)))
                    return false;

                if (boundSession == null)
                {
                    boundSession = actor.Session;
                    boundAccount = actor.Account;
                    boundCharacter = actor.CharacterId;
                }
                if (owner == Owner.Character)
                {
                    characterRows = copy;
                    characterReady = true;
                    characterSnapshot = snapshot;
                }
                else if (owner == Owner.Contract)
                {
                    contractRows = copy;
                    contractReady = true;
                    contractSnapshot = snapshot;
                }
                else
                {
                    accountRows = copy;
                    accountReady = true;
                }

                if (reload && accountReady && (!characterExpected || characterReady) && (!contractExpected || contractReady))
                {
                    // The packet gets its own copies too: queue/consumer mutation
                    // cannot change the next composed snapshot. No owner locks are
                    // acquired while this presentation gate is held.
                    long sequence = ++sentReloadSequence;
                    var session = actor.Session;
                    object sentSnapshot = null, sentContractSnapshot = null;
                    void SendReload()
                    {
                        sentSnapshot = characterSnapshot;
                        sentContractSnapshot = contractSnapshot;
                        session.EnqueueMessageEncrypted(new ServerRewardTracksReload
                        {
                            RewardsTracks = accountRows.Concat(characterRows).Concat(contractRows).Select(Copy).ToList()
                        });
                    }

                    try { SendReload(); }
                    catch (Exception error)
                    {
                        if (sequence != sentReloadSequence || !CurrentActor()) throw;

                        log.Warn(error, "Reward-track reload failed; retrying the current snapshot on its original actor.");
                        SendReload();
                    }
                    // Enqueue can reenter presentation. Only the last actual
                    // full reload binds claims; an accepted but unsent
                    // contribution must not authorize a new cycle.
                    if (sequence == sentReloadSequence && CurrentActor())
                    {
                        publishedCharacterSnapshot = sentSnapshot;
                        publishedContractSnapshot = sentContractSnapshot;
                    }
                }
                return true;
            }
        }

        private bool CurrentActor() => !invalidated && actor?.Session != null && actor.Account != null
            && actor.Session is not NetworkSession { IsDisconnecting: true }
            && (actor.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, actor))
            && actor.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
            && ReferenceEquals(actor.Account.Session, actor.Session)
            && (boundSession == null || (ReferenceEquals(boundSession, actor.Session)
                && ReferenceEquals(boundAccount, actor.Account) && boundCharacter == actor.CharacterId));

        public void Invalidate()
        {
            lock (gate)
            {
                invalidated = true;
                accountRows = characterRows = contractRows = [];
                accountReady = characterReady = contractReady = false;
                characterSnapshot = publishedCharacterSnapshot = null;
                contractSnapshot = publishedContractSnapshot = null;
            }
        }

        private static bool TryCopy(IReadOnlyList<RewardTrack> tracks, int maximumActive, out RewardTrack[] copy)
        {
            copy = null;
            if (tracks == null)
                return false;
            var ids = new HashSet<uint>();
            int active = 0;
            foreach (RewardTrack row in tracks)
            {
                if (row == null || row.RewardTrackId is 0 or > 0x3FFF
                    || !ids.Add(row.RewardTrackId) || row.ClaimedRewards?.GetBuffer().Length != sizeof(uint)
                    || row.RewardTrackRewardsIds == null || row.RewardTrackRewardsIds.Count > 32
                    || row.RewardTrackRewardsIds.Any(id => id is 0 or > 0x3FFF)
                    || (row.IsActive && ++active > maximumActive))
                    return false;
            }
            copy = tracks.Select(Copy).ToArray();
            return true;
        }

        private static RewardTrack Copy(RewardTrack row)
        {
            var mask = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
            Array.Copy(row.ClaimedRewards.GetBuffer(), mask.GetBuffer(), sizeof(uint));
            return new RewardTrack
            {
                RewardTrackId = row.RewardTrackId, RewardPointsEarned = row.RewardPointsEarned,
                ClaimedRewards = mask, Unlocked = row.Unlocked,
                RewardTrackRewardsIds = row.RewardTrackRewardsIds.ToList(), IsActive = row.IsActive
            };
        }
    }
}
