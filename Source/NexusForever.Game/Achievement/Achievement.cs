using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using AchievementNetworkModel = NexusForever.Network.World.Message.Model.Achievement.Achievement;

namespace NexusForever.Game.Achievement
{
    public class Achievement<T> : IAchievement
        where T : class, IAchievementModel, new()
    {
        [Flags]
        protected enum SaveMask
        {
            None                 = 0x00,
            Create               = 0x01,
            ProgressState        = 0x02,
            CreditedChecklistMask = 0x04,
            TimeCompleted        = 0x08
        }

        public IAchievementInfo Info { get; }
        public ushort Id => Info.Id;

        public uint ProgressCount
        {
            get => progressState;
            set => SetProgressState(value);
        }

        public uint CompletedChecklistMask
        {
            get => progressState;
            set => SetProgressState(value);
        }

        // ProgressState stores either scalar progress or the completed checklist bitmask depending on the achievement type.
        private uint progressState;

        public uint CreditedChecklistMask
        {
            get => creditedChecklistMask;
            set
            {
                saveMask |= SaveMask.CreditedChecklistMask;
                creditedChecklistMask = value;
            }
        }

        private uint creditedChecklistMask;

        public DateTime? DateCompleted
        {
            get => dateCompleted;
            set
            {
                saveMask |= SaveMask.TimeCompleted;
                dateCompleted = value;
            }
        }

        private DateTime? dateCompleted;

        protected SaveMask saveMask;

        // this can either be a characterId or guildId depending on the achievement type
        private readonly ulong ownerId;

        /// <summary>
        /// Create a new <see cref="IAchievement"/> from an existing database model.
        /// </summary>
        public Achievement(IAchievementInfo info, IAchievementModel model)
        {
            ownerId       = model.Id;
            Info          = info;
            progressState = model.ProgressState;
            creditedChecklistMask = model.CreditedChecklistMask;
            DateCompleted = model.DateCompleted;
        }

        /// <summary>
        /// Create a new <see cref="IAchievement"/> from <see cref="IAchievementInfo"/> and supplied data.
        /// </summary>
        public Achievement(ulong ownerId, IAchievementInfo info)
        {
            this.ownerId = ownerId;
            Info         = info;

            saveMask |= SaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == SaveMask.None)
                return;

            if ((saveMask & SaveMask.Create) != 0)
            {
                context.Add(new T
                {
                    Id            = ownerId,
                    AchievementId = Id,
                    ProgressState         = progressState,
                    CreditedChecklistMask = creditedChecklistMask,
                    DateCompleted = DateCompleted
                });
            }
            else
            {
                var model = new T
                {
                    Id            = ownerId,
                    AchievementId = Id
                };

                EntityEntry<T> entity = context.Attach(model);
                if ((saveMask & SaveMask.ProgressState) != 0)
                {
                    model.ProgressState = progressState;
                    entity.Property(p => p.ProgressState).IsModified = true;
                }
                if ((saveMask & SaveMask.CreditedChecklistMask) != 0)
                {
                    model.CreditedChecklistMask = creditedChecklistMask;
                    entity.Property(p => p.CreditedChecklistMask).IsModified = true;
                }
                if ((saveMask & SaveMask.TimeCompleted) != 0)
                {
                    model.DateCompleted = DateCompleted;
                    entity.Property(p => p.DateCompleted).IsModified = true;
                }
            }

            saveMask = SaveMask.None;
        }

        /// <summary>
        /// Build a network model from <see cref="IAchievement"/>
        /// </summary>
        public AchievementNetworkModel Build()
        {
            return new()
            {
                AchievementId = Id,
                ProgressState         = progressState,
                CreditedChecklistMask = creditedChecklistMask,
                DateCompleted = (ulong)(DateCompleted?.ToFileTimeUtc() ?? 0L)
            };
        }

        /// <summary>
        /// Returns if <see cref="IAchievement"/> has been completed.
        /// </summary>
        public bool IsComplete()
        {
            if (DateCompleted != null)
                return true;

            if (Info.ChecklistEntries.Count == 0 || AchievementProgressRules.UsesChecklistValueProgress(Info))
                return ProgressCount >= AchievementProgressRules.GetRequiredProgress(Info.Entry.RequiredProgress);

            return Info.ChecklistEntries.All(entry => (CompletedChecklistMask & (1u << (int)entry.Bit)) != 0);
        }

        private void SetProgressState(uint value)
        {
            saveMask |= SaveMask.ProgressState;
            progressState = value;
        }
    }
}
