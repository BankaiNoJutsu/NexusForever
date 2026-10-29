using System.Numerics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Map;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private readonly object bindStateLock = new();
        private ushort bindPointId;
        private ushort bindWorldId;
        private Vector3 bindPosition;
        private bool bindExists;
        private bool bindDirty;
        private uint bindRevision;
        private IMapPosition lastOpenWorldPosition;

        private void LoadBindPoint(CharacterBindModel model)
        {
            if (model == null)
                return;
            bindPointId = model.BindPointId;
            bindWorldId = model.WorldId;
            bindPosition = new Vector3(model.X, model.Y, model.Z);
            bindExists = true;
        }

        public void SetBindPoint(ushort bindPointId, ushort worldId, Vector3 position)
        {
            if (bindPointId == 0 || worldId == 0 || !float.IsFinite(position.X)
                || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                throw new ArgumentException("A bind point requires a valid id, world and finite position.");

            lock (bindStateLock)
            {
                this.bindPointId = bindPointId;
                bindWorldId = worldId;
                bindPosition = position;
                bindDirty = true;
                bindRevision++;
            }
            // Existing 0x016D model already has the upstream bind-point wire shape.
            Session.EnqueueMessageEncrypted(new ServerActionSetUInt16 { Value = bindPointId });
        }

        public bool TryGetBindPoint(out ushort worldId, out Vector3 position)
        {
            lock (bindStateLock)
            {
                worldId = bindWorldId;
                position = bindPosition;
                return (bindExists || bindDirty) && bindPointId != 0 && worldId != 0;
            }
        }

        private void SaveBindPoint(CharacterContext context)
        {
            lock (bindStateLock)
                SaveBindPointCore(context);
        }

        private void SaveBindPointCore(CharacterContext context)
        {
            if (!bindDirty)
                return;
            uint revision = bindRevision;
            var model = new CharacterBindModel
            {
                Id = CharacterId, BindPointId = bindPointId, WorldId = bindWorldId,
                X = bindPosition.X, Y = bindPosition.Y, Z = bindPosition.Z
            };
            if (bindExists)
                context.Entry(model).State = EntityState.Modified;
            else
                context.Add(model);

            // A failed save must leave an INSERT retryable; a new bind during an
            // in-flight save must remain dirty for the next save.
            context.SavedChanges += (_, _) =>
            {
                lock (bindStateLock)
                {
                    bindExists = true;
                    if (bindRevision == revision)
                        bindDirty = false;
                }
            };
        }

        private void CaptureOpenWorldReturnPosition()
        {
            if (Map?.Entry == null || Map is IMapInstance)
                return;
            lastOpenWorldPosition = new MapPosition
            {
                Info = new MapInfo { Entry = Map.Entry },
                Position = Position
            };
        }

        public IMapPosition GetOpenWorldReturnPosition()
        {
            if (lastOpenWorldPosition != null)
                return lastOpenWorldPosition;
            if (!TryGetBindPoint(out ushort worldId, out Vector3 position))
                return null;
            var entry = gameTableManager?.World?.GetEntry(worldId);
            return entry == null ? null : new MapPosition
            {
                Info = new MapInfo { Entry = entry }, Position = position
            };
        }
    }
}
