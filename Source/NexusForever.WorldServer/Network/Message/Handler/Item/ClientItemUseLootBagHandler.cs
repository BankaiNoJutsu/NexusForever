using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Network;
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
            IItem item = session.Player.Inventory.GetItem(itemUseLootBag.ItemLocation);
            if (item == null)
                throw new InvalidPacketValueException();

            if (itemUseLootBag.Guid != item.Guid)
                throw new InvalidPacketValueException();

            if (item.Info.Entry.Item2CategoryId != LOOT_BAG_CATEGORY_ID)
                throw new InvalidPacketValueException();

            if (!lootManager.HasLoot(item))
            {
                session.Player.SendGenericError(GenericError.ItemNoItems);
                return;
            }

            if (!lootManager.TryUseLootBag(session.Player, item, out string reason) && reason == "inventory-full")
                session.Player.SendGenericError(GenericError.ItemInventoryFull);
        }
    }
}
