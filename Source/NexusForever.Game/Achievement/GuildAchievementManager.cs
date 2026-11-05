using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Guild;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Achievement;
using GameGuild = NexusForever.Game.Guild.Guild;
using AchievementNetworkModel = NexusForever.Network.World.Message.Model.Achievement.Achievement;

namespace NexusForever.Game.Achievement
{
    public sealed class GuildAchievementManager : BaseAchievementManager<GuildAchievementModel>, IGuildAchievementManager
    {
        private readonly IGuild guild;
        protected override ulong OwnerId => guild.Id;
        private readonly Dictionary<ushort, GuildMutationPersistence.Achievement> published = [];
        internal RealmFirstPersistence RealmFirst { get; private set; }
        protected override object RowMutationGate => (guild as GameGuild)?.MutationGate;
        protected override Action RowMutationAdmission => RequireEditable;
        protected override Action<Achievement<GuildAchievementModel>> RowOwnerAdmission => RequireRow;
        private void RequireRow(Achievement<GuildAchievementModel> row)
        {
            if (OriginalGuild() != null && (row.SaveOwnerId != guild.Id || !achievements.TryGetValue(row.Id, out IAchievement current) || !ReferenceEquals(current, row)))
                throw new InvalidOperationException("Guild achievement lost its original collection owner.");
        }
        protected override Func<CharacterContext, bool> CharacterSaveOwner => SaveGuild;

        private GameGuild OriginalGuild()
        {
            if (guild is not GameGuild original) return null;
            original.RequireAchievementOwner(this);
            return original;
        }
        private void RequireEditable() => OriginalGuild()?.RequireCreationAchievementEditable(this);
        private bool SaveGuild(CharacterContext context)
        {
            try
            {
                GameGuild original = OriginalGuild();
                if (original == null) return false;
                original.Save(context); return true;
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }
        internal GuildMutationPersistence.Achievement[] Capture()
        {
            _ = OriginalGuild();
            return achievements.OrderBy(pair => pair.Key).Select(pair =>
            {
                if (pair.Value is not Achievement<GuildAchievementModel> row || row.SaveOwnerId != guild.Id || pair.Key != row.Id || row.Info.IsPlayerAchievement)
                    throw new InvalidOperationException("Guild achievement lost its original row identity.");
                return new GuildMutationPersistence.Achievement(row.Id, row.ProgressCount, row.CreditedChecklistMask, row.DateCompleted);
            }).ToArray();
        }
        public override void Initialise(IEnumerable<GuildAchievementModel> models, bool isPlayer)
        {
            if (guild is GameGuild) throw new InvalidOperationException("Guild achievements are loaded by their original guild.");
            base.Initialise(models, isPlayer);
        }
        public override bool HasCompletedAchievement(ushort id)
        {
            lock (RowMutationGate ?? this) return base.HasCompletedAchievement(id);
        }
        public override void GrantAchievement(ushort id)
        {
            lock (RowMutationGate ?? this)
            {
                RequireEditable();
                if (GetGlobalAchievementManager().GetAchievement(id)?.IsPlayerAchievement == true)
                    throw new InvalidOperationException("A guild cannot grant a character achievement.");
                base.GrantAchievement(id);
            }
        }
        public override void SendInitialPackets(IPlayer target)
        {
            lock (RowMutationGate ?? this)
            {
                if (OriginalGuild() == null) { base.SendInitialPackets(target); return; }
                target.Session.EnqueueMessageEncrypted(new ServerAchievementInit { Achievements = published.Values.OrderBy(row => row.Id).Select(Build).ToList() });
            }
        }
        private static AchievementNetworkModel Build(GuildMutationPersistence.Achievement row) => new()
        {
            AchievementId = row.Id, ProgressState = row.Progress, CreditedChecklistMask = row.Credited,
            DateCompleted = (ulong)(row.Completed?.ToFileTimeUtc() ?? 0L)
        };
        internal void Acknowledge(GuildMutationPersistence.Achievement[] before, GuildMutationPersistence.Achievement[] intent, GuildMutationPersistence.Achievement[] stored)
        {
            GameGuild original = OriginalGuild();
            foreach (var row in intent)
                ((Achievement<GuildAchievementModel>)achievements[row.Id]).AcknowledgeGuild(row.Progress, row.Credited, row.Completed);
            var updates = stored.Where(row => before.SingleOrDefault(old => old.Id == row.Id) != row).ToArray();
            if (updates.Length != 0) original.EnqueueAchievementUpdate(this, updates);
        }
        internal void Publish(GuildMutationPersistence.Achievement[] rows)
        {
            lock (RowMutationGate ?? this)
            {
                _ = OriginalGuild();
                guild.Broadcast(new ServerAchievementUpdate { Achievements = rows.Select(Build).ToList() });
                foreach (var row in rows) published[row.Id] = row;
            }
        }
        internal void PublishRealmFirst(ushort id, string name)
        {
            lock (RowMutationGate ?? this)
            {
                _ = OriginalGuild();
                BroadcastRealmFirstAchievement(id, true, name);
            }
        }

        /// <summary>
        /// Create a new <see cref="IGuildAchievementManager"/> from existing <see cref="GuildModel"/> database model.
        /// </summary>
        public GuildAchievementManager(
            IGuild guild,
            GuildModel model,
            IDisableManager disableManager = null,
            IGlobalAchievementManager globalAchievementManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IPlayerManager playerManager = null)
            : base(disableManager, globalAchievementManager, prerequisiteManager, playerManager)
        {
            this.guild = guild;
            base.Initialise(model.Achievement, false);
            foreach (GuildAchievementModel row in model.Achievement)
                published.Add(row.AchievementId, new(row.AchievementId, row.ProgressState, row.CreditedChecklistMask, row.DateCompleted));
        }

        /// <summary>
        /// Create a new <see cref="IGuildAchievementManager"/> for <see cref="IGuild"/>.
        /// </summary>
        public GuildAchievementManager(
            IGuild guild,
            IDisableManager disableManager = null,
            IGlobalAchievementManager globalAchievementManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IPlayerManager playerManager = null)
            : base(disableManager, globalAchievementManager, prerequisiteManager, playerManager)
        {
            this.guild = guild;
        }

        protected override void SendAchievementUpdate(IEnumerable<IAchievement> updates)
        {
            if (OriginalGuild() == null) guild.Broadcast(BuildAchievementUpdate(updates));
        }

        protected override void CompleteAchievement(IAchievement achievement)
        {
            base.CompleteAchievement(achievement);

            if (achievement.Info.IsRealmFirst)
            {
                if (OriginalGuild() == null)
                {
                    if (GetGlobalAchievementManager().TryClaimRealmFirstAchievement(achievement.Info, true))
                        BroadcastRealmFirstAchievement(achievement, true, guild.Name);
                }
                else (RealmFirst ??= new(true, guild.Id)).Retain(achievement.Id, guild.Name);
            }
        }

        protected override void CompleteAchievement(IPlayer target, IAchievement achievement)
        {
            base.CompleteAchievement(target, achievement);

            if (target != null)
                target.AchievementManager.CheckAchievements(target, AchievementType.AchievementComplete, achievement.Id);
        }

        /// <summary>
        /// Update or complete player achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0, uint count = 1)
        {
            lock (RowMutationGate ?? this)
            {
                RequireEditable();
                CheckAchievements(target, GetGlobalAchievementManager().GetGuildAchievements(type), objectId, objectIdAlt, count);
            }
        }

        /// <summary>
        /// Set current progress for guild achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
        {
            lock (RowMutationGate ?? this)
            {
                RequireEditable();
                SetAchievementProgress(target, GetGlobalAchievementManager().GetGuildAchievements(type), objectId, objectIdAlt, value);
            }
        }
    }
}
