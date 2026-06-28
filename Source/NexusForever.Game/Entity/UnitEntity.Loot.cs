using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public abstract partial class UnitEntity
    {
        private void RemoveLootForOwner()
        {
            GetGlobalLootManager()?.RemoveLootForOwner(Guid);
        }

        private IGlobalLootManager GetGlobalLootManager()
        {
            return globalLootManagerResolver?.Invoke();
        }

        private void SendLootRemoveForOwnerToVisiblePlayers()
        {
            if (this is not INonPlayerEntity || Guid == 0u)
                return;

            foreach (IPlayer player in visibleEntities.Values.OfType<IPlayer>().ToList())
            {
                player.Session.EnqueueMessageEncrypted(new ServerLootRemove
                {
                    OwnerUnitId = Guid
                });
            }
        }

        private void RefreshVisiblePlayersAfterRespawn()
        {
            if (this is not INonPlayerEntity || Guid == 0u)
                return;

            foreach (IPlayer player in visibleEntities.Values.OfType<IPlayer>().ToList())
            {
                // The client can keep corpse presentation state for reused creature GUIDs.
                player.Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid = Guid,
                    Flag = true
                });

                player.Session.EnqueueMessageEncrypted(BuildCreatePacket(false));
            }
        }
    }
}
