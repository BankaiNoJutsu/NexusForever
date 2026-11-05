using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public class Bone : IBone
    {
        [Flags]
        public enum BoneSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Modify = 0x0002,
            Delete = 0x0004
        }

        public ulong Owner { get; }
        public byte BoneIndex { get; }

        public float BoneValue
        {
            get => boneValue;
            set
            {
                mutationAllowed?.Invoke();
                if (boneValue != value)
                {
                    boneValue = value;
                    saveMask |= BoneSaveMask.Modify;
                }
            }
        }
        private float boneValue;

        private readonly Action mutationAllowed;

        public bool PendingDelete => (saveMask & BoneSaveMask.Delete) != 0;

        private BoneSaveMask saveMask;

        /// <summary>
        /// Create a new <see cref="IBone"/> from database model.
        /// </summary>
        public Bone(CharacterBoneModel model) : this(model, null) { }

        internal Bone(CharacterBoneModel model, Action mutationAllowed)
        {
            this.mutationAllowed = mutationAllowed;
            Owner     = model.Id;
            BoneIndex = model.BoneIndex;
            boneValue = model.Bone;

            saveMask  = BoneSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IBone"/> from supplied data.
        /// </summary>
        public Bone(ulong characterId, byte boneIndex, float value) : this(characterId, boneIndex, value, null) { }

        internal Bone(ulong characterId, byte boneIndex, float value, Action mutationAllowed)
        {
            this.mutationAllowed = mutationAllowed;
            Owner     = characterId;
            BoneIndex = boneIndex;
            boneValue = value;

            saveMask  = BoneSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == BoneSaveMask.None)
                return;

            var model = new CharacterBoneModel
            {
                Id        = Owner,
                BoneIndex = BoneIndex
            };

            EntityEntry<CharacterBoneModel> entity = context.Attach(model);

            if ((saveMask & BoneSaveMask.Create) != 0)
            {
                model.Bone = BoneValue;

                context.Add(model);
            }
            else if ((saveMask & BoneSaveMask.Delete) != 0)
            {
                context.Entry(model).State = EntityState.Deleted;
            }
            else if ((saveMask & BoneSaveMask.Modify) != 0)
            {
                model.Bone = BoneValue;
                entity.Property(e => e.Bone).IsModified = true;
            }

            saveMask = BoneSaveMask.None;
        }

        public void Delete()
        {
            mutationAllowed?.Invoke();
            if ((saveMask & BoneSaveMask.Create) != 0)
                saveMask = BoneSaveMask.None;
            else
                saveMask = BoneSaveMask.Delete;
        }
    }
}
