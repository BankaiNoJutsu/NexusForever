using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Achievement
{
    public sealed class CharacterAchievementManager : BaseAchievementManager<CharacterAchievementModel>, ICharacterAchievementManager
    {
        private readonly IPlayer owner;
        private readonly IGroupStateManager groupStateManager;
        private readonly IPlayerManager playerManager;
        private readonly IGameTableManager gameTableManager;
        protected override ulong OwnerId => owner.CharacterId;

        /// <summary>
        /// Create a new <see cref="CharacterAchievementManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public CharacterAchievementManager(
            IPlayer owner,
            CharacterModel model,
            IGroupStateManager groupStateManager = null,
            IDisableManager disableManager = null,
            IGlobalAchievementManager globalAchievementManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IPlayerManager playerManager = null,
            IGameTableManager gameTableManager = null)
            : base(disableManager, globalAchievementManager, prerequisiteManager, playerManager)
        {
            this.owner             = owner;
            this.groupStateManager = groupStateManager;
            this.playerManager     = playerManager;
            this.gameTableManager  = gameTableManager;
            Initialise(model.Achievement, true);
        }

        /// <summary>
        /// Send initial <see cref="IAchievement"/> information to owner on login.
        /// </summary>
        /// <remarks>
        /// Guild achievements will also be sent if owner is part of a <see cref="IGuild"/>.
        /// </remarks>
        public override void SendInitialPackets(IPlayer _)
        {
            base.SendInitialPackets(owner);
            owner.GuildManager.Guild?.AchievementManager.SendInitialPackets(owner);
        }

        protected override void SendAchievementUpdate(IEnumerable<IAchievement> updates)
        {
            owner.Session.EnqueueMessageEncrypted(BuildAchievementUpdate(updates));
        }

        /// <summary>
        /// Update or complete player achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0u, uint count = 1u)
        {
            CheckAchievements(target, GetGlobalAchievementManager().GetCharacterAchievements(type), objectId, objectIdAlt, count);
            if (ShouldForwardToGuildAchievements(target, type))
                target.GuildManager.Guild?.AchievementManager.CheckAchievements(target, type, objectId, objectIdAlt, count);
        }

        /// <summary>
        /// Set current progress for player achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
        {
            SetAchievementProgress(target, GetGlobalAchievementManager().GetCharacterAchievements(type), objectId, objectIdAlt, value);
            if (ShouldForwardToGuildAchievements(target, type))
                target.GuildManager.Guild?.AchievementManager.SetAchievementProgress(target, type, objectId, objectIdAlt, value);
        }

        private bool ShouldForwardToGuildAchievements(IPlayer target, AchievementType type)
        {
            return type switch
            {
                AchievementType.KillCreatureChecklist or AchievementType.PublicEventObjectiveComplete => IsAllGuildGroup(target),
                _ => true
            };
        }

        private bool IsAllGuildGroup(IPlayer target)
        {
            IGuild guild = target?.GuildManager.Guild;
            if (guild == null || target.GroupAssociation == 0ul)
                return false;

            if (groupStateManager == null
                || !groupStateManager.TryGetGroupForCharacter(target.Identity, out GroupLootState group)
                || group.Members.Count <= 1)
                return false;

            foreach (GroupLootMember member in group.Members)
            {
                IPlayer memberPlayer = playerManager?.GetPlayer(member.Identity);
                if (memberPlayer?.GuildManager.Guild?.Id != guild.Id)
                    return false;
            }

            return true;
        }

        protected override void CompleteAchievement(IAchievement achievement)
        {
            base.CompleteAchievement(achievement);

            if (achievement.Info.Entry.CharacterTitleId != 0u)
                owner.TitleManager.AddTitle((ushort)achievement.Info.Entry.CharacterTitleId);

            if (GetGlobalAchievementManager().TryClaimRealmFirstAchievement(achievement.Info, false))
                BroadcastRealmFirstAchievement(achievement, false, owner.Name);
        }

        protected override void CompleteAchievement(IPlayer target, IAchievement achievement)
        {
            base.CompleteAchievement(target, achievement);

            IPlayer achievementOwner = target ?? owner;
            GrantCompletedTradeskillAchievementRewards(achievementOwner);
            CheckAchievements(achievementOwner, AchievementType.AchievementComplete, achievement.Id);
        }

        public void GrantCompletedTradeskillAchievementRewards(IPlayer player)
        {
            if (player == null)
                return;

            TradeskillAchievementRewardEntry[] rewardEntries = gameTableManager?.TradeskillAchievementReward?.Entries;
            if (rewardEntries == null || rewardEntries.Length == 0)
                return;

            var earnedTalentPointsByTradeskill = new Dictionary<TradeskillType, uint>();

            foreach (IAchievement achievement in achievements.Values.Where(achievement => achievement.IsComplete()))
            {
                foreach (TradeskillAchievementRewardEntry reward in rewardEntries.Where(entry => entry.AchievementId == achievement.Info.Entry.Id))
                {
                    foreach (uint schematicId in GetRewardSchematicIds(reward))
                        player.LearnSchematic(schematicId);

                    if (reward.TalentPoints == 0u)
                        continue;

                    foreach (TradeskillType tradeskillId in ResolveRewardTradeskills(achievement.Info.Entry, reward))
                    {
                        earnedTalentPointsByTradeskill.TryGetValue(tradeskillId, out uint currentTalentPoints);
                        earnedTalentPointsByTradeskill[tradeskillId] = AddSaturated(currentTalentPoints, reward.TalentPoints);
                    }
                }
            }

            foreach ((TradeskillType tradeskillId, uint earnedTalentPoints) in earnedTalentPointsByTradeskill)
                player.EnsureTradeskillTalentPointTotal(tradeskillId, earnedTalentPoints);
        }

        private IEnumerable<TradeskillType> ResolveRewardTradeskills(AchievementEntry achievement, TradeskillAchievementRewardEntry reward)
        {
            var tradeskillIds = new HashSet<TradeskillType>();

            if (TryResolveTradeskillFromCategory(achievement.AchievementCategoryId, out TradeskillType categoryTradeskillId))
                tradeskillIds.Add(categoryTradeskillId);

            foreach (uint schematicId in GetRewardSchematicIds(reward))
                if (TryResolveTradeskillFromSchematic(schematicId, out TradeskillType schematicTradeskillId))
                    tradeskillIds.Add(schematicTradeskillId);

            if ((AchievementType)achievement.AchievementTypeId == AchievementType.TradeskillTier
                && TryResolveTradeskillFromTier(achievement.ObjectId, out TradeskillType tierTradeskillId))
                tradeskillIds.Add(tierTradeskillId);

            if ((AchievementType)achievement.AchievementTypeId is AchievementType.CraftItem or AchievementType.CraftItemChecklist
                && TryResolveTradeskillFromCraftedItem(achievement.ObjectId, out TradeskillType craftedItemTradeskillId))
                tradeskillIds.Add(craftedItemTradeskillId);

            return tradeskillIds;
        }

        private bool TryResolveTradeskillFromCategory(uint achievementCategoryId, out TradeskillType tradeskillId)
        {
            tradeskillId = default;
            if (achievementCategoryId == 0u)
                return false;

            uint currentCategoryId = achievementCategoryId;
            for (int depth = 0; depth < 8 && currentCategoryId != 0u; depth++)
            {
                TradeskillEntry tradeskill = gameTableManager.Tradeskill?.Entries
                    .FirstOrDefault(entry => entry.AchievementCategoryId == currentCategoryId);
                if (TryConvertTradeskillId(tradeskill?.Id ?? 0u, out tradeskillId))
                    return true;

                AchievementCategoryEntry category = gameTableManager.AchievementCategory?.GetEntry(currentCategoryId);
                currentCategoryId = category?.AchievementCategoryIdParent ?? 0u;
            }

            return false;
        }

        private bool TryResolveTradeskillFromSchematic(uint tradeskillSchematic2Id, out TradeskillType tradeskillId)
        {
            tradeskillId = default;
            TradeskillSchematic2Entry schematic = gameTableManager?.TradeskillSchematic2?.GetEntry(tradeskillSchematic2Id);
            return TryConvertTradeskillId(schematic?.TradeSkillId ?? 0u, out tradeskillId);
        }

        private bool TryResolveTradeskillFromTier(uint tradeskillTierId, out TradeskillType tradeskillId)
        {
            tradeskillId = default;
            TradeskillTierEntry tier = gameTableManager?.TradeskillTier?.GetEntry(tradeskillTierId);
            return TryConvertTradeskillId(tier?.TradeSkillId ?? 0u, out tradeskillId);
        }

        private bool TryResolveTradeskillFromCraftedItem(uint item2Id, out TradeskillType tradeskillId)
        {
            tradeskillId = default;
            if (item2Id == 0u)
                return false;

            TradeskillSchematic2Entry schematic = gameTableManager?.TradeskillSchematic2?.Entries
                .FirstOrDefault(entry => entry.Item2IdOutput == item2Id
                    || entry.Item2IdOutputFail == item2Id
                    || entry.Item2IdOutputCrit == item2Id);
            return TryConvertTradeskillId(schematic?.TradeSkillId ?? 0u, out tradeskillId);
        }

        private static bool TryConvertTradeskillId(uint value, out TradeskillType tradeskillId)
        {
            tradeskillId = (TradeskillType)value;
            return value != 0u && Enum.IsDefined(tradeskillId);
        }

        private static uint AddSaturated(uint left, uint right)
        {
            return uint.MaxValue - left < right ? uint.MaxValue : left + right;
        }

        private static IEnumerable<uint> GetRewardSchematicIds(TradeskillAchievementRewardEntry reward)
        {
            if (reward.TradeSkillSchematicId00 != 0u)
                yield return reward.TradeSkillSchematicId00;
            if (reward.TradeSkillSchematicId01 != 0u)
                yield return reward.TradeSkillSchematicId01;
            if (reward.TradeSkillSchematicId02 != 0u)
                yield return reward.TradeSkillSchematicId02;
            if (reward.TradeSkillSchematicId03 != 0u)
                yield return reward.TradeSkillSchematicId03;
            if (reward.TradeSkillSchematicId04 != 0u)
                yield return reward.TradeSkillSchematicId04;
            if (reward.TradeSkillSchematicId05 != 0u)
                yield return reward.TradeSkillSchematicId05;
            if (reward.TradeSkillSchematicId06 != 0u)
                yield return reward.TradeSkillSchematicId06;
            if (reward.TradeSkillSchematicId07 != 0u)
                yield return reward.TradeSkillSchematicId07;
        }
    }
}
