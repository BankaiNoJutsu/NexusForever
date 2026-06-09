using System.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.Game.Achievement
{
    public abstract class BaseAchievementManager<T> : IBaseAchievementManager<T> where T : class, IAchievementModel, new()
    {
        public uint AchievementPoints { get; protected set; }

        protected abstract ulong OwnerId { get; }
        protected Dictionary<ushort, IAchievement> achievements = new();

        /// <summary>
        /// Initialise a collection of existing achievement database models.
        /// </summary>
        public void Initialise(IEnumerable<T> models, bool isPlayer)
        {
            foreach (T model in models)
            {
                IAchievementInfo info = GlobalAchievementManager.Instance.GetAchievement(model.AchievementId);
                if (info == null)
                    throw new DatabaseDataException($"{(isPlayer ? "Player" : "Guild")} {model.Id} has invalid achievement {model.AchievementId} stored!");

                if (isPlayer && !info.IsPlayerAchievement)
                    throw new DatabaseDataException($"Player {model.Id} has guild achievement {model.AchievementId} stored!");
                if (!isPlayer && info.IsPlayerAchievement)
                    throw new DatabaseDataException($"Guild {model.Id} has player achievement {model.AchievementId} stored!");

                var achievement = new Achievement<T>(info, model);
                if (achievement.IsComplete())
                    AchievementPoints += GetAchievementPoints(achievement.Info);

                achievements.Add(achievement.Id, achievement);
            }
        }

        public void Save(CharacterContext context)
        {
            foreach (Achievement<T> achievement in achievements.Values)
                achievement.Save(context);
        }

        /// <summary>
        /// Returns if the supplied achievement id has been completed.
        /// </summary>
        public bool HasCompletedAchievement(ushort id)
        {
            return achievements.TryGetValue(id, out IAchievement achievement) && achievement.IsComplete();
        }

        /// <summary>
        /// Send initial <see cref="IAchievement"/> information to owner on login.
        /// </summary>
        public virtual void SendInitialPackets(IPlayer target)
        {
            target.Session.EnqueueMessageEncrypted(new ServerAchievementInit
            {
                Achievements = achievements.Values
                    .Select(a => a.Build())
                    .ToList()
            });
        }

        protected ServerAchievementUpdate BuildAchievementUpdate(IEnumerable<IAchievement> updates)
        {
            return new()
            {
                Achievements = updates
                    .Select(a => a.Build())
                    .ToList()
            };
        }

        protected void SendAchievementUpdate(params IAchievement[] updates)
        {
            SendAchievementUpdate(updates.AsEnumerable());
        }

        protected abstract void SendAchievementUpdate(IEnumerable<IAchievement> updates);

        /// <summary>
        /// Grant achievement by supplied achievement id.
        /// </summary>
        public void GrantAchievement(ushort id)
        {
            IAchievementInfo info = GlobalAchievementManager.Instance.GetAchievement(id);
            if (info == null)
                throw new ArgumentException();

            if (HasCompletedAchievement(info.Id))
                throw new ArgumentException();

            IAchievement achievement = GetAchievement(id);
            if (info.ChecklistEntries.Count == 0 || AchievementProgressRules.UsesChecklistValueProgress(info))
            {
                achievement.ProgressCount = AchievementProgressRules.GetRequiredProgress(info.Entry.RequiredProgress);
                foreach (AchievementChecklistEntry entry in info.ChecklistEntries)
                    if (AchievementProgressRules.TryBuildChecklistBit(entry.Bit, out uint bit))
                        achievement.CreditedChecklistMask |= bit;
            }
            else
                foreach (AchievementChecklistEntry entry in info.ChecklistEntries)
                    if (AchievementProgressRules.TryBuildChecklistBit(entry.Bit, out uint bit))
                        achievement.CompletedChecklistMask |= bit;

            Debug.Assert(achievement.IsComplete());
            CompleteAchievement(null, achievement);
            SendAchievementUpdate(achievement);
        }

        /// <summary>
        /// Update or complete any achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public abstract void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0u, uint count = 1u);

        /// <summary>
        /// Set current progress for threshold achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public abstract void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value);

        /// <summary>
        /// Update or complete a collection of achievements as <see cref="IPlayer"/> sending the result to the client.
        /// </summary>
        protected void CheckAchievements(IPlayer target, IEnumerable<IAchievementInfo> achievements, uint objectId, uint objectIdAlt, uint count)
        {
            var updates = new List<IAchievement>();
            foreach (IAchievementInfo info in achievements)
                if (CheckAchievement(target, info, objectId, objectIdAlt, count))
                    updates.Add(GetAchievement(info.Id));

            if (updates.Count != 0)
                SendAchievementUpdate(updates);
        }

        /// <summary>
        /// Set current progress for a collection of achievements as <see cref="IPlayer"/> sending the result to the client.
        /// </summary>
        protected void SetAchievementProgress(IPlayer target, IEnumerable<IAchievementInfo> achievements, uint objectId, uint objectIdAlt, uint value)
        {
            var updates = new List<IAchievement>();
            foreach (IAchievementInfo info in achievements)
                if (SetAchievementProgress(target, info, objectId, objectIdAlt, value))
                    updates.Add(GetAchievement(info.Id));

            if (updates.Count != 0)
                SendAchievementUpdate(updates);
        }

        /// <summary>
        /// Update or complete <see cref="AchievementInfo"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        private bool CheckAchievement(IPlayer target, IAchievementInfo info, uint objectId, uint objectIdAlt, uint count)
        {
            if (HasCompletedAchievement(info.Id))
                return false;

            if (DisableManager.Instance.IsDisabled(DisableType.Achievement, info.Id))
                return false;

            bool sendUpdate = false;

            IAchievement achievement = null;
            if (info.ChecklistEntries.Count == 0)
            {
                if (CanUpdateAchievement(target, info.Entry, objectId, objectIdAlt))
                {
                    if (count == 0u)
                        return false;

                    achievement = GetAchievement(info.Id);
                    achievement.ProgressCount = AddProgress(achievement.ProgressCount, count, AchievementProgressRules.GetRequiredProgress(info.Entry.RequiredProgress));
                    sendUpdate = true;
                }
            }
            else if (AchievementProgressRules.UsesChecklistValueProgress(info))
            {
                if (count == 0u)
                    return false;

                achievement = GetAchievement(info.Id);
                bool matchedNewChecklistEntry = false;
                foreach (AchievementChecklistEntry entry in info.ChecklistEntries)
                {
                    if (!CanUpdateChecklist(target, entry, objectId, objectIdAlt))
                        continue;

                    if (!AchievementProgressRules.TryBuildChecklistBit(entry.Bit, out uint bit))
                        continue;

                    if ((achievement.CreditedChecklistMask & bit) != 0u)
                        continue;

                    achievement.CreditedChecklistMask |= bit;
                    matchedNewChecklistEntry = true;
                }

                if (matchedNewChecklistEntry)
                {
                    achievement.ProgressCount = AddProgress(achievement.ProgressCount, count, AchievementProgressRules.GetRequiredProgress(info.Entry.RequiredProgress));
                    sendUpdate = true;
                }
            }
            else
            {
                achievement = GetAchievement(info.Id);
                foreach (AchievementChecklistEntry entry in info.ChecklistEntries)
                {
                    if (!CanUpdateChecklist(target, entry, objectId, objectIdAlt))
                        continue;

                    if (!AchievementProgressRules.TryBuildChecklistBit(entry.Bit, out uint bit))
                        continue;

                    if ((achievement.CompletedChecklistMask & bit) != 0u)
                        continue;

                    achievement.CompletedChecklistMask |= bit;
                    sendUpdate = true;
                }
            }

            if (achievement != null && achievement.IsComplete())
                CompleteAchievement(target, achievement);

            return sendUpdate;
        }

        /// <summary>
        /// Set current progress for <see cref="AchievementInfo"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        private bool SetAchievementProgress(IPlayer target, IAchievementInfo info, uint objectId, uint objectIdAlt, uint value)
        {
            if (HasCompletedAchievement(info.Id))
                return false;

            if (DisableManager.Instance.IsDisabled(DisableType.Achievement, info.Id))
                return false;

            if (info.ChecklistEntries.Count != 0)
                return CheckAchievement(target, info, objectId, objectIdAlt, 1u);

            if (!CanUpdateAchievement(target, info.Entry, objectId, objectIdAlt))
                return false;

            IAchievement achievement = GetAchievement(info.Id);
            uint progress = Math.Min(Math.Max(achievement.ProgressCount, value), AchievementProgressRules.GetRequiredProgress(info.Entry.RequiredProgress));
            if (progress == achievement.ProgressCount)
                return false;

            achievement.ProgressCount = progress;
            if (achievement.IsComplete())
                CompleteAchievement(target, achievement);

            return true;
        }

        /// <summary>
        /// Check if <see cref="AchievementEntry"/> can be updated as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        private bool CanUpdateAchievement(IPlayer player, AchievementEntry entry, uint objectId, uint objectIdAlt)
        {
            if (entry.PrerequisiteIdServer != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteIdServer))
                return false;
            
            if (entry.PrerequisiteId != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteId))
                return false;

            if (entry.PrerequisiteIdObjective != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteIdObjective))
                return false;

            if (entry.PrerequisiteIdObjectiveAlt != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteIdObjectiveAlt))
                return false;

            if ((AchievementType)entry.AchievementTypeId == AchievementType.EnterWorldZone
                && entry.WorldZoneId != 0u
                && entry.WorldZoneId != objectId)
                return false;

            if (RequiresExplicitObjectMatch((AchievementType)entry.AchievementTypeId)
                && entry.ObjectId == 0u
                && entry.ObjectIdAlt == 0u)
                return false;

            if (entry.ObjectId != 0u && entry.ObjectId != objectId)
                return false;
            if (entry.ObjectIdAlt != 0u && entry.ObjectIdAlt != objectIdAlt)
                return false;

            return true;
        }

        private static bool RequiresExplicitObjectMatch(AchievementType type)
        {
            return type switch
            {
                AchievementType.KillCreatureGroup => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if <see cref="AchievementChecklistEntry"/> can be updated as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        private bool CanUpdateChecklist(IPlayer player, AchievementChecklistEntry entry, uint objectId, uint objectIdAlt)
        {
            if (entry.PrerequisiteId != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteId))
                return false;
            // no checklist entry has PrerequisiteIdAlt set
            if (entry.PrerequisiteIdAlt != 0u && !PrerequisiteManager.Instance.Meets(player, entry.PrerequisiteIdAlt))
                return false;

            if (entry.ObjectId != 0u && entry.ObjectId != objectId)
                return false;
            if (entry.ObjectIdAlt != 0u && entry.ObjectIdAlt != objectIdAlt)
                return false;

            if (entry.ObjectId == 0u && entry.ObjectIdAlt == 0u)
                return false;

            return true;
        }

        protected virtual void CompleteAchievement(IAchievement achievement)
        {
            achievement.DateCompleted = DateTime.UtcNow;
            AchievementPoints += GetAchievementPoints(achievement.Info);
        }

        protected virtual void CompleteAchievement(IPlayer target, IAchievement achievement)
        {
            CompleteAchievement(achievement);
        }

        protected void BroadcastRealmFirstAchievement(IAchievement achievement, bool isGuildAchievement, string name)
        {
            foreach (IPlayer player in PlayerManager.Instance)
                player.Session?.EnqueueMessageEncrypted(new ServerRealmFirstAchievement
                {
                    AchievementId      = achievement.Id,
                    IsGuildAchievement = isGuildAchievement,
                    Name               = name
                });
        }

        /// <summary>
        /// Returns the amount of achievement points earned when completing supplied <see cref="IAchievementInfo"/>.
        /// </summary>
        protected uint GetAchievementPoints(IAchievementInfo info)
        {
            return info.Entry.AchievementPointEnum switch
            {
                1u => 10u,
                2u => 25u,
                3u => 50u,
                _ => 0u
            };
        }

        private static uint AddProgress(uint current, uint count, uint required)
        {
            if (current >= required)
                return current;

            if (uint.MaxValue - current < count)
                return required;

            return Math.Min(current + count, required);
        }

        /// <summary>
        /// Return <see cref="IAchievement"/> with supplied id, if it doesn't exist it will be created.
        /// </summary>
        private IAchievement GetAchievement(ushort id)
        {
            if (achievements.TryGetValue(id, out IAchievement achievement))
                return achievement;

            IAchievementInfo info = GlobalAchievementManager.Instance.GetAchievement(id);
            if (info == null)
                throw new ArgumentException();

            achievement = new Achievement<T>(OwnerId, info);
            achievements.Add(achievement.Id, achievement);
            return achievement;
        }
    }
}
