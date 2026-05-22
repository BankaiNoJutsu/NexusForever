using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Housing
{
    internal sealed class ResidenceNeighbor
    {
        [Flags]
        private enum SaveMask
        {
            None       = 0,
            Create     = 1,
            Permission = 2,
            Delete     = 4
        }

        public ulong CharacterId { get; }
        public bool WasPersisted => (saveMask & SaveMask.Create) == 0;
        public bool PendingDelete => (saveMask & SaveMask.Delete) != 0;

        public byte PermissionLevel
        {
            get => permissionLevel;
            set
            {
                if (permissionLevel == value)
                    return;

                permissionLevel = value;
                if ((saveMask & SaveMask.Create) == 0)
                    saveMask |= SaveMask.Permission;
            }
        }

        private byte permissionLevel;
        private SaveMask saveMask;

        public ResidenceNeighbor(ResidenceNeighborModel model)
        {
            CharacterId     = model.NeighborCharacterId;
            permissionLevel = model.PermissionLevel;
        }

        public ResidenceNeighbor(ulong characterId, byte permissionLevel)
        {
            CharacterId     = characterId;
            this.permissionLevel = permissionLevel;
            saveMask        = SaveMask.Create;
        }

        public void MarkDelete()
        {
            saveMask |= SaveMask.Delete;
        }

        public void UnmarkDelete()
        {
            saveMask &= ~SaveMask.Delete;
        }

        public void Save(CharacterContext context, ulong residenceId)
        {
            if (saveMask == SaveMask.None)
                return;

            if ((saveMask & SaveMask.Delete) != 0)
            {
                if ((saveMask & SaveMask.Create) == 0)
                {
                    context.Remove(new ResidenceNeighborModel
                    {
                        ResidenceId        = residenceId,
                        NeighborCharacterId = CharacterId
                    });
                }

                saveMask = SaveMask.None;
                return;
            }

            if ((saveMask & SaveMask.Create) != 0)
            {
                context.Add(new ResidenceNeighborModel
                {
                    ResidenceId         = residenceId,
                    NeighborCharacterId = CharacterId,
                    PermissionLevel     = permissionLevel
                });

                saveMask = SaveMask.None;
                return;
            }

            if ((saveMask & SaveMask.Permission) != 0)
            {
                var model = new ResidenceNeighborModel
                {
                    ResidenceId         = residenceId,
                    NeighborCharacterId = CharacterId,
                    PermissionLevel     = permissionLevel
                };

                EntityEntry<ResidenceNeighborModel> entity = context.Attach(model);
                entity.Property(p => p.PermissionLevel).IsModified = true;
                saveMask = SaveMask.None;
            }
        }
    }
}