using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Group;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Achievement
{
    public sealed class CharacterAchievementManager : BaseAchievementManager<CharacterAchievementModel>, ICharacterAchievementManager
    {
        private readonly IPlayer owner;
        protected override ulong OwnerId => owner.CharacterId;

        /// <summary>
        /// Create a new <see cref="CharacterAchievementManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public CharacterAchievementManager(IPlayer owner, CharacterModel model)
        {
            this.owner = owner;
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
            CheckAchievements(target, GlobalAchievementManager.Instance.GetCharacterAchievements(type), objectId, objectIdAlt, count);
            if (ShouldForwardToGuildAchievements(target, type))
                target.GuildManager.Guild?.AchievementManager.CheckAchievements(target, type, objectId, objectIdAlt, count);
        }

        /// <summary>
        /// Set current progress for player achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
        {
            SetAchievementProgress(target, GlobalAchievementManager.Instance.GetCharacterAchievements(type), objectId, objectIdAlt, value);
            if (ShouldForwardToGuildAchievements(target, type))
                target.GuildManager.Guild?.AchievementManager.SetAchievementProgress(target, type, objectId, objectIdAlt, value);
        }

        private static bool ShouldForwardToGuildAchievements(IPlayer target, AchievementType type)
        {
            return type switch
            {
                AchievementType.KillCreatureChecklist or AchievementType.PublicEventObjectiveComplete => IsAllGuildGroup(target),
                _ => true
            };
        }

        private static bool IsAllGuildGroup(IPlayer target)
        {
            IGuild guild = target?.GuildManager.Guild;
            if (guild == null || target.GroupAssociation == 0ul)
                return false;

            if (!GroupStateManager.Instance.TryGetGroupForCharacter(target.Identity, out GroupLootState group) || group.Members.Count <= 1)
                return false;

            foreach (GroupLootMember member in group.Members)
            {
                IPlayer memberPlayer = PlayerManager.Instance.GetPlayer(member.Identity);
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

            if (GlobalAchievementManager.Instance.TryClaimRealmFirstAchievement(achievement.Info, false))
                BroadcastRealmFirstAchievement(achievement, false, owner.Name);
        }

        protected override void CompleteAchievement(IPlayer target, IAchievement achievement)
        {
            base.CompleteAchievement(target, achievement);

            IPlayer achievementOwner = target ?? owner;
            CheckAchievements(achievementOwner, AchievementType.AchievementComplete, achievement.Id);
        }
    }
}
