using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Entity
{
    public class XpManager : IXpManager
    {
        private const byte DefaultMaxCharacterLevel = 50;
        private const float DefaultSignatureXpRate = 0.25f;
        private const ushort HousingWorldId = 1229;
        private const uint LevelUpFanfareSpell4BaseId = 53378u;
        private const double HousingRestXpPercentPerHour = 0.0024d;
        private const float RestXpCapLevelPercent = 1.5f;
        private const float RestXpKillPercent = 0.5f;

        public uint TotalXp
        {
            get => totalXp;
            private set
            {
                totalXp = value;
                isDirty = true;
            }
        }
        private uint totalXp;

        public uint RestBonusXp
        {
            get => restBonusXp;
            private set
            {
                restBonusXp = value;
                isDirty = true;
            }
        }
        private uint restBonusXp;

        private bool isDirty;
        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly ISharedConfiguration sharedConfiguration;

        /// <summary>
        /// Create a new <see cref="IXpManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public XpManager(IPlayer player, CharacterModel model, IGameTableManager gameTableManager, ISharedConfiguration sharedConfiguration = null)
        {
            this.player = player;
            this.gameTableManager = gameTableManager;
            this.sharedConfiguration = sharedConfiguration;
            totalXp = model.TotalXp;

            CalculateRestXpAtLogin(model);
        }

        public static byte CalculateLevelForXp(uint totalXp, IGameTableManager gameTableManager)
        {
            byte maxLevel = GetMaxCharacterLevel();
            uint level = (gameTableManager.XpPerLevel?.Entries ?? [])
                .Where(e => e.Id <= maxLevel && e.MinXpForLevel <= totalXp)
                .Select(e => e.Id)
                .DefaultIfEmpty(1u)
                .Max();

            return (byte)Math.Clamp(level, 1u, maxLevel);
        }

        public static byte ResolveStoredLevel(byte storedLevel, uint totalXp, IGameTableManager gameTableManager)
        {
            return Math.Max(storedLevel, CalculateLevelForXp(totalXp, gameTableManager));
        }

        public void Save(CharacterContext context)
        {
            if (!isDirty)
                return;

            // character is attached in Player::Save, this will only be local lookup
            CharacterModel character = context.Character.Find(player.CharacterId);
            character.TotalXp = TotalXp;
            character.RestBonusXp = RestBonusXp;

            EntityEntry<CharacterModel> entity = context.Entry(character);
            entity.Property(p => p.TotalXp).IsModified = true;
            entity.Property(p => p.RestBonusXp).IsModified = true;

            isDirty = false;
        }

        private void CalculateRestXpAtLogin(CharacterModel model)
        {
            // don't calculate rest xp for first login
            if (model.LastOnline == null)
                return;

            if (!TryGetCurrentLevelXpSpan(out uint levelXpSpan))
                return;

            uint maximumBonusXp = GetMaximumRestBonusXp(levelXpSpan);

            double xpPercentEarned;

            double hoursSinceLogin = DateTime.UtcNow.Subtract((DateTime)model.LastOnline).TotalHours;
            switch (model.WorldId)
            {
                case HousingWorldId:
                    xpPercentEarned = hoursSinceLogin * HousingRestXpPercentPerHour;
                    break;
                default:
                    xpPercentEarned = 0d;
                    break;
            }

            uint bonusXpValue = Math.Clamp((uint)(levelXpSpan * xpPercentEarned), 0, maximumBonusXp);
            uint totalBonusXp = Math.Clamp(model.RestBonusXp + bonusXpValue, 0u, maximumBonusXp);
            RestBonusXp = totalBonusXp;
        }

        public uint ModifyRestBonusXp(float levelSpanMultiplier)
        {
            if (float.IsNaN(levelSpanMultiplier) || float.IsInfinity(levelSpanMultiplier))
                return RestBonusXp;

            if (!TryGetCurrentLevelXpSpan(out uint levelXpSpan))
                return RestBonusXp;

            uint maximumBonusXp = GetMaximumRestBonusXp(levelXpSpan);
            RestBonusXp = CalculateModifiedRestBonusXp(RestBonusXp, levelXpSpan, maximumBonusXp, levelSpanMultiplier);
            return RestBonusXp;
        }

        public static uint CalculateModifiedRestBonusXp(uint currentRestBonusXp, uint levelXpSpan, uint maximumRestBonusXp, float levelSpanMultiplier)
        {
            if (float.IsNaN(levelSpanMultiplier) || float.IsInfinity(levelSpanMultiplier))
                return Math.Min(currentRestBonusXp, maximumRestBonusXp);

            if (levelXpSpan == 0u || maximumRestBonusXp == 0u)
                return 0u;

            double modifiedRestBonusXp = currentRestBonusXp + levelXpSpan * (double)levelSpanMultiplier;
            if (modifiedRestBonusXp <= 0d)
                return 0u;

            if (modifiedRestBonusXp >= maximumRestBonusXp)
                return maximumRestBonusXp;

            return (uint)modifiedRestBonusXp;
        }

        /// <summary>
        /// Grants <see cref="IPlayer"/> the supplied experience, handling level up if necessary.
        /// </summary>
        /// <param name="earnedXp">Experience to grant</param>
        /// <param name="reason"><see cref="ExpReason"/> for the experience grant</param>
        public void GrantXp(uint earnedXp, ExpReason reason = ExpReason.Cheat)
        {
            byte maxLevel = GetMaxCharacterLevel();

            if (earnedXp < 1)
                return;

            //if (!IsAlive)
            //    return;

            if (player.Level >= maxLevel)
                return;

            // Signature XP rate was 25% extra.
            uint signatureXp = 0u;
            if (player.SignatureEnabled)
                signatureXp = (uint)(earnedXp * GetSignatureXpRate());

            // Calculate Rest XP Bonus
            uint restXp = 0u;
            if (reason == ExpReason.KillCreature)
            {
                restXp = (uint)(earnedXp * RestXpKillPercent);
                if (restXp > RestBonusXp)
                    restXp = RestBonusXp;

                RestBonusXp -= restXp;
            }

            player.Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained     = earnedXp + signatureXp + restXp,
                RestXpAmount      = restXp,
                SignatureXpAmount = signatureXp,
                Reason            = reason
            });

            uint totalXp = TotalXp + earnedXp + signatureXp + restXp;

            while (player.Level < maxLevel)
            {
                byte nextLevel = (byte)(player.Level + 1);
                if (!TryGetXpForLevel(nextLevel, out uint xpToNextLevel))
                    break;

                if (totalXp < xpToNextLevel)
                    break;

                GrantLevel(nextLevel);
            }

            TotalXp += earnedXp + signatureXp + restXp;
        }

        public void GrantXpForCreatureKill(uint targetLevel, uint groupValue, float targetXpMultiplier = 1f)
        {
            if (targetXpMultiplier < float.Epsilon)
                targetXpMultiplier = 1f;

            uint baseXp;
            switch (groupValue)
            {
                case 5:
                case 20:
                case 40:
                    baseXp = (uint)MathF.Round((25 + targetLevel + MathF.Pow(targetLevel + 1, 2)) / 5) * 5;
                    break;
                case 0:
                default:
                    baseXp = (uint)MathF.Round((25 + targetLevel + MathF.Pow(targetLevel + 1, 2)) / 5) * 5;
                    break;
            }

            baseXp = (uint)(baseXp * targetXpMultiplier);
            GrantXp(baseXp, ExpReason.KillCreature);
        }

        /// <summary>
        /// Sets <see cref="IPlayer"/> to the supplied level and adjusts XP accordingly. Mainly for use with GM commands.
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        /// <param name="reason"><see cref="ExpReason"/> for the level grant</param>
        public void SetLevel(byte newLevel, ExpReason reason = ExpReason.Cheat)
        {
            if (newLevel == player.Level)
                return;

            if (!TryGetXpForLevel(newLevel, out uint newXp))
                return;

            uint xpGained = newXp > TotalXp ? newXp - TotalXp : 0u;
            player.Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained     = xpGained,
                RestXpAmount      = 0,
                SignatureXpAmount = 0,
                Reason            = reason
            });

            TotalXp = newXp;
            GrantLevel(newLevel);
        }

        /// <summary>
        /// Grants <see cref="IPlayer"/> the supplied level and adjusts XP accordingly
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        private void GrantLevel(byte newLevel)
        {
            uint oldLevel = player.Level;
            if (newLevel == oldLevel)
                return;

            player.Level = newLevel;

            if (newLevel <= oldLevel)
                return;

            player.CastSpell(LevelUpFanfareSpell4BaseId, GetLevelUpFanfareTier(newLevel), new SpellParameters());
            player.AchievementManager.SetAchievementProgress(player, AchievementType.CharacterLevel, 0u, 0u, newLevel);
            if (oldLevel < DefaultMaxCharacterLevel && newLevel >= DefaultMaxCharacterLevel)
                player.AchievementManager.CheckAchievements(player, AchievementType.ClassLevel50, (uint)player.Class);

            // Grant Rewards for level up
            player.SpellManager.GrantSpells();
            // Unlock LAS slots
            // Unlock AMPs
            // Add feature access
        }

        private static byte GetMaxCharacterLevel(ISharedConfiguration sharedConfiguration = null)
        {
            try
            {
                return sharedConfiguration?.Get<WorldConfig>()?.MaxCharacterLevel ?? DefaultMaxCharacterLevel;
            }
            catch (InvalidOperationException)
            {
                return DefaultMaxCharacterLevel;
            }
        }

        private float GetSignatureXpRate()
        {
            return sharedConfiguration?.Get<WorldConfig>()?.SignatureXpRate ?? DefaultSignatureXpRate;
        }

        private bool TryGetCurrentLevelXpSpan(out uint levelXpSpan)
        {
            levelXpSpan = 0u;
            if (player.Level >= GetMaxCharacterLevel(sharedConfiguration))
                return true;

            if (!TryGetXpForLevel(player.Level, out uint xpForLevel)
                || !TryGetXpForLevel(player.Level + 1, out uint xpForNextLevel))
                return false;

            levelXpSpan = xpForNextLevel > xpForLevel ? xpForNextLevel - xpForLevel : 0u;
            return true;
        }

        private bool TryGetXpForLevel(uint level, out uint xp)
        {
            xp = 0u;
            XpPerLevelEntry entry = gameTableManager.XpPerLevel?.GetEntry(level);
            if (entry == null)
                return false;

            xp = entry.MinXpForLevel;
            return true;
        }

        private static uint GetMaximumRestBonusXp(uint levelXpSpan)
        {
            return (uint)(levelXpSpan * RestXpCapLevelPercent);
        }

        private static byte GetLevelUpFanfareTier(byte level)
        {
            return (byte)(level - 1);
        }
    }
}
