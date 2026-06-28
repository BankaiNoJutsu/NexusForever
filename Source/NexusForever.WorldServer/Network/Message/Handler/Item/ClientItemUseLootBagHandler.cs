using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemUseLootBagHandler : IMessageHandler<IWorldSession, ClientItemUseLootBag>
    {
        private const uint LOOT_BAG_CATEGORY_ID = 138u;

        #region Dependency Injection

        private readonly IGlobalLootManager lootManager;

        public ClientItemUseLootBagHandler(IGlobalLootManager lootManager)
        {
            this.lootManager = lootManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemUseLootBag itemUseLootBag)
        {
            IItem item = GetItemOrDefault(session, itemUseLootBag);
            if (item == null)
            {
                SendItemError(session, itemUseLootBag.Guid, GenericError.ItemBadId);
                return;
            }

            if (itemUseLootBag.Guid != item.Guid)
            {
                SendItemError(session, itemUseLootBag.Guid, GenericError.ItemBadId);
                return;
            }

            if (lootManager.HasLoot(item))
            {
                if (!lootManager.TryUseLootBag(session.Player, item, out string reason) && reason == "inventory-full")
                    session.Player.SendGenericError(GenericError.ItemInventoryFull);

                return;
            }

            if (item.Info?.Entry?.Item2CategoryId == LOOT_BAG_CATEGORY_ID)
            {
                session.Player.SendGenericError(GenericError.ItemNoItems);
                return;
            }

            if (!lootManager.TrySalvageItem(session.Player, item, out string salvageReason))
                SendItemError(session, item.Guid, GetSalvageError(salvageReason));
        }

        private static IItem GetItemOrDefault(IWorldSession session, ClientItemUseLootBag itemUseLootBag)
        {
            try
            {
                return session.Player?.Inventory?.GetItem(itemUseLootBag.ItemLocation);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void SendItemError(IWorldSession session, ulong itemGuid, GenericError error)
        {
            session.EnqueueMessageEncrypted(new ServerItemError
            {
                ItemGuid  = itemGuid,
                ErrorCode = error
            });
        }

        private static GenericError GetSalvageError(string reason)
        {
            return reason == "inventory-full"
                ? GenericError.ItemInventoryFull
                : GenericError.ItemCannotBeSalvaged;
        }
    }
}
