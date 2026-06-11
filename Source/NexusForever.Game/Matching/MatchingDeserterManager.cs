using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Entity;
using NexusForever.Game.Retail;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching
{
    public sealed class MatchingDeserterManager : IMatchingDeserterManager
    {
        private readonly object syncRoot = new();
        private readonly Dictionary<ulong, DeserterPenalty> penalties = new();
        private readonly HashSet<ulong> loadedCharacterIds = [];

        private readonly IMatchingPenaltyStore matchingPenaltyStore;
        private readonly IPlayerManager playerManager;

        public MatchingDeserterManager()
        {
        }

        public MatchingDeserterManager(IMatchingPenaltyStore matchingPenaltyStore)
            : this(matchingPenaltyStore, null)
        {
        }

        public MatchingDeserterManager(
            IMatchingPenaltyStore matchingPenaltyStore,
            IPlayerManager playerManager)
        {
            this.matchingPenaltyStore = matchingPenaltyStore;
            this.playerManager        = playerManager;
        }

        public bool CanQueue(ulong characterId, MatchType matchType)
        {
            if (!TryGetActivePenalty(characterId, out DeserterPenalty penalty))
                return true;

            return penalty.IsPvP != IsPvPMatchType(matchType);
        }

        public void ApplyDeserter(ulong characterId, MatchType matchType, double completionRatio)
        {
            bool isPvP = IsPvPMatchType(matchType);
            int baseSeconds = isPvP ? RetailCertainRules.PvpDeserterBaseSeconds : RetailCertainRules.PveDeserterBaseSeconds;
            double scale = Math.Clamp(1d - completionRatio, 0d, 1d);
            int durationSeconds = Math.Max(60, (int)Math.Round(baseSeconds * scale));

            uint spell4Id = isPvP
                ? RetailMatchingDeserterSpells.PvpDeserterSpell4Id
                : RetailMatchingDeserterSpells.GetPveDeserterSpell4Id(completionRatio);

            var penalty = new DeserterPenalty
            {
                IsPvP        = isPvP,
                MatchType    = matchType,
                Spell4Id     = spell4Id,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(durationSeconds)
            };

            lock (syncRoot)
            {
                loadedCharacterIds.Add(characterId);
                penalties[characterId] = penalty;
            }

            PersistPenalty(characterId, penalty);

            IPlayer player = TryGetOnlinePlayer(characterId);
            ApplyDeserterSpell(player, spell4Id);
            SyncDeserterUi(player);
        }

        public void ClearDeserter(ulong characterId)
        {
            uint spell4Id = 0;
            lock (syncRoot)
            {
                if (penalties.TryGetValue(characterId, out DeserterPenalty penalty))
                    spell4Id = penalty.Spell4Id;

                loadedCharacterIds.Add(characterId);
                penalties.Remove(characterId);
            }

            DeletePersistedPenalty(characterId);

            IPlayer player = TryGetOnlinePlayer(characterId);
            if (spell4Id != 0u)
                RemoveDeserterSpell(player, spell4Id);

            SyncDeserterUi(player);
        }

        public void RestoreDeserter(IPlayer player)
        {
            if (player == null)
                return;

            if (!TryGetActivePenalty(player.CharacterId, out DeserterPenalty penalty))
                return;

            ApplyDeserterSpell(player, penalty.Spell4Id);
        }

        public int GetRemainingPenaltyMilliseconds(ulong characterId)
        {
            if (!TryGetActivePenalty(characterId, out DeserterPenalty penalty))
                return 0;

            return (int)(penalty.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalMilliseconds;
        }

        public uint[] GetMatchingPenaltyTimesMilliseconds(ulong characterId)
        {
            var penaltyTimes = new uint[16];

            if (!TryGetActivePenalty(characterId, out DeserterPenalty penalty))
                return penaltyTimes;

            TimeSpan remaining = penalty.ExpiresAtUtc - DateTimeOffset.UtcNow;
            uint remainingMs = (uint)Math.Min(uint.MaxValue, Math.Ceiling(remaining.TotalMilliseconds));
            foreach (MatchType matchType in Enum.GetValues<MatchType>())
            {
                if (matchType == MatchType.None)
                    continue;

                if (IsPvPMatchType(matchType) == penalty.IsPvP)
                    penaltyTimes[(int)matchType] = remainingMs;
            }

            return penaltyTimes;
        }

        public void SyncDeserterUi(IPlayer player)
        {
            if (player?.Session == null)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerMatchingPenaltyUpdated
            {
                MatchingPenaltyTimesMS = GetMatchingPenaltyTimesMilliseconds(player.CharacterId)
            });
        }

        private bool TryGetActivePenalty(ulong characterId, out DeserterPenalty penalty)
        {
            EnsurePenaltyLoaded(characterId);

            bool deletePersisted = false;
            lock (syncRoot)
            {
                if (!penalties.TryGetValue(characterId, out penalty))
                    return false;

                if (penalty.ExpiresAtUtc > DateTimeOffset.UtcNow)
                    return true;

                penalties.Remove(characterId);
                deletePersisted = true;
            }

            if (deletePersisted)
                DeletePersistedPenalty(characterId);

            penalty = null;
            return false;
        }

        private void EnsurePenaltyLoaded(ulong characterId)
        {
            lock (syncRoot)
            {
                if (loadedCharacterIds.Contains(characterId))
                    return;

                loadedCharacterIds.Add(characterId);
            }

            MatchingPenaltyState? state = matchingPenaltyStore?.Get(characterId);
            if (state == null)
                return;

            var penalty = new DeserterPenalty
            {
                IsPvP        = state.Value.IsPvP,
                MatchType    = state.Value.MatchType,
                Spell4Id     = state.Value.Spell4Id,
                ExpiresAtUtc = state.Value.ExpiresAtUtc
            };

            if (penalty.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                DeletePersistedPenalty(characterId);
                return;
            }

            lock (syncRoot)
                penalties[characterId] = penalty;
        }

        private void PersistPenalty(ulong characterId, DeserterPenalty penalty)
        {
            matchingPenaltyStore?.Save(characterId, new MatchingPenaltyState(
                penalty.IsPvP,
                penalty.MatchType,
                penalty.Spell4Id,
                penalty.ExpiresAtUtc));
        }

        private void DeletePersistedPenalty(ulong characterId)
        {
            matchingPenaltyStore?.Delete(characterId);
        }

        private IPlayer TryGetOnlinePlayer(ulong characterId)
        {
            return playerManager?.GetPlayer(characterId);
        }

        private static bool IsPvPMatchType(MatchType matchType)
        {
            return matchType
                is MatchType.BattleGround
                or MatchType.Arena
                or MatchType.Warplot
                or MatchType.RatedBattleground
                or MatchType.OpenArena;
        }

        static void ApplyDeserterSpell(IPlayer player, uint spell4Id)
        {
            if (player is not Player unit || spell4Id == 0u)
                return;

            unit.CastSpell(spell4Id, new SpellParameters
            {
                UserInitiatedSpellCast = false
            });
        }

        static void RemoveDeserterSpell(IPlayer player, uint spell4Id)
        {
            if (player is not Player unit || spell4Id == 0u)
                return;

            unit.RemoveCCStatesBySpell(spell4Id);
            unit.RemoveSpellProperties(spell4Id);
        }

        private sealed class DeserterPenalty
        {
            public bool IsPvP { get; init; }
            public MatchType MatchType { get; init; }
            public uint Spell4Id { get; init; }
            public DateTimeOffset ExpiresAtUtc { get; init; }
        }
    }
}
