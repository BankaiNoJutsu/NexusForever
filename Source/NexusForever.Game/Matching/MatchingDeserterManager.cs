using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Entity;
using NexusForever.Game.Retail;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching
{
    public sealed class MatchingDeserterManager : Singleton<MatchingDeserterManager>, IMatchingDeserterManager
    {
        private readonly object syncRoot = new();
        private readonly Dictionary<ulong, DeserterPenalty> penalties = new();

        public bool CanQueue(ulong characterId, MatchType matchType)
        {
            lock (syncRoot)
            {
                if (!penalties.TryGetValue(characterId, out DeserterPenalty penalty))
                    return true;

                if (penalty.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                {
                    penalties.Remove(characterId);
                    return true;
                }

                return penalty.IsPvP != IsPvPMatchType(matchType);
            }
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

            lock (syncRoot)
            {
                penalties[characterId] = new DeserterPenalty
                {
                    IsPvP        = isPvP,
                    Spell4Id     = spell4Id,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(durationSeconds)
                };
            }

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

                penalties.Remove(characterId);
            }

            IPlayer player = TryGetOnlinePlayer(characterId);
            if (spell4Id != 0u)
                RemoveDeserterSpell(player, spell4Id);

            SyncDeserterUi(player);
        }

        public int GetRemainingPenaltyMilliseconds(ulong characterId)
        {
            lock (syncRoot)
            {
                if (!penalties.TryGetValue(characterId, out DeserterPenalty penalty))
                    return 0;

                TimeSpan remaining = penalty.ExpiresAtUtc - DateTimeOffset.UtcNow;
                return remaining <= TimeSpan.Zero ? 0 : (int)remaining.TotalMilliseconds;
            }
        }

        public void SyncDeserterUi(IPlayer player)
        {
            if (player?.Session == null)
                return;

            int remainingMs = GetRemainingPenaltyMilliseconds(player.CharacterId);
            if (remainingMs <= 0)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerMatchingMatchKickCooldownUpdate
            {
                Result               = MatchingQueueResult.PersonalKickCooldown,
                WaitTimeBeforeVoteMS = (uint)remainingMs
            });
        }

        static IPlayer TryGetOnlinePlayer(ulong characterId)
        {
            if (LegacyServiceProvider.Provider?.GetService(typeof(IPlayerManager)) is not IPlayerManager playerManager)
                return null;

            return playerManager.GetPlayer(characterId);
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
            public uint Spell4Id { get; init; }
            public DateTimeOffset ExpiresAtUtc { get; init; }
        }
    }
}
