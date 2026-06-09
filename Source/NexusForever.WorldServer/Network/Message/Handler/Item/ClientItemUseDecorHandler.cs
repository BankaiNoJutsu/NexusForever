using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemUseDecorHandler : IMessageHandler<IWorldSession, ClientItemUseDecor>
    {
        #region Dependency Injection

        private readonly ILogger<ClientItemUseDecorHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientItemUseDecorHandler(
            ILogger<ClientItemUseDecorHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemUseDecor itemUseDecor)
        {
            IItem item = session.Player.Inventory.GetItem(itemUseDecor.ItemGuid);
            if (item == null)
                throw new InvalidPacketValueException();

            HousingDecorInfoEntry entry = gameTableManager.HousingDecorInfo?.GetEntry(item.Info.Entry.HousingDecorInfoId);
            if (entry == null)
                throw new InvalidPacketValueException();

            if (!CanUseItem(item))
                return;

            try
            {
                if (session.Player.ResidenceManager.GetOrCreateResidence() == null)
                    return;
            }
            catch (HousingException exception)
            {
                log.LogWarning(exception, "Player {PlayerGuid} cannot use decor item {ItemGuid}.", session.Player.Guid, itemUseDecor.ItemGuid);
                return;
            }

            if (!session.Player.Inventory.ItemUse(item))
                return;

            session.Player.ResidenceManager.DecorCreate(entry);
        }

        private static bool CanUseItem(IItem item)
        {
            if (item.Info.Entry.MaxCharges == 0 && item.Info.Entry.MaxStackCount == 1)
                return true;

            if (item.Charges <= 0 && item.Info.Entry.MaxCharges > 1)
                return false;

            return item.StackCount > 0 || item.Info.Entry.MaxStackCount <= 1;
        }
    }
}
