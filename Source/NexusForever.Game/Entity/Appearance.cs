using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public class Appearance : IAppearance
    {
        [Flags]
        public enum AppearanceSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Modify = 0x0002,
            Delete = 0x0004
        }

        public ulong Owner { get; }
        public ItemSlot ItemSlot { get; }

        public ushort DisplayId
        {
            get => displayId;
            set
            {
                mutationAllowed?.Invoke();
                if (displayId != value)
                {
                    displayId = value;
                    saveMask |= AppearanceSaveMask.Modify;
                }
            }
        }
        private ushort displayId;

        private readonly Action mutationAllowed;

        public bool PendingDelete => (saveMask & AppearanceSaveMask.Delete) != 0;

        private AppearanceSaveMask saveMask;

        /// <summary>
        /// Create a new <see cref="IAppearance"/> from database model.
        /// </summary>
        public Appearance(CharacterAppearanceModel model) : this(model, null) { }

        internal Appearance(CharacterAppearanceModel model, Action mutationAllowed)
        {
            this.mutationAllowed = mutationAllowed;
            Owner     = model.Id;
            ItemSlot  = (ItemSlot)model.Slot;
            displayId = model.DisplayId;

            saveMask  = AppearanceSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IAppearance"/> from supplied data.
        /// </summary>
        public Appearance(ulong characterId, ItemSlot itemSlot, ushort displayId) : this(characterId, itemSlot, displayId, null) { }

        internal Appearance(ulong characterId, ItemSlot itemSlot, ushort displayId, Action mutationAllowed)
        {
            this.mutationAllowed = mutationAllowed;
            Owner          = characterId;
            ItemSlot       = itemSlot;
            this.displayId = displayId;

            saveMask       = AppearanceSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == AppearanceSaveMask.None)
                return;

            var model = new CharacterAppearanceModel
            {
                Id   = Owner,
                Slot = (byte)ItemSlot
            };

            EntityEntry<CharacterAppearanceModel> entity = context.Attach(model);

            if ((saveMask & AppearanceSaveMask.Create) != 0)
            {
                model.DisplayId = DisplayId;

                context.Add(model);
            }
            else if ((saveMask & AppearanceSaveMask.Delete) != 0)
            {
                context.Entry(model).State = EntityState.Deleted;
            }
            else if ((saveMask & AppearanceSaveMask.Modify) != 0)
            {
                model.DisplayId = DisplayId;
                entity.Property(e => e.DisplayId).IsModified = true;
            }

            saveMask = AppearanceSaveMask.None;
        }

        public void Delete()
        {
            mutationAllowed?.Invoke();
            if ((saveMask & AppearanceSaveMask.Create) != 0)
                saveMask = AppearanceSaveMask.None;
            else
                saveMask = AppearanceSaveMask.Delete;
        }
    }
}
