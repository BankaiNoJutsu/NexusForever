using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemUseHandler : IMessageHandler<IWorldSession, ClientItemUse>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientItemUseHandler(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemUse itemUse)
        {
            IItem item = session.Player.Inventory.GetItem(itemUse.Location);
            if (item == null)
                throw new InvalidPacketValueException();

            ItemSpecialEntry itemSpecial = gameTableManager.ItemSpecial.GetEntry(item.Info.Entry.ItemSpecialId00);
            if (itemSpecial == null)
                throw new InvalidPacketValueException();

            if (itemSpecial.Spell4IdOnActivate > 0u)
            {
                if (itemSpecial.PrerequisiteIdGeneric00 > 0)
                {
                    if (!PrerequisiteEvaluation.Meets(session.Player, itemSpecial.PrerequisiteIdGeneric00, item))
                    {
                        session.Player.SendGenericError(GenericError.UnlockItemFailed);
                        return;
                    }
                }

                if (session.Player.Inventory.ItemUse(item))
                {
                    var spellParameters = new SpellParameters
                    {
                        PrimaryTargetId     = itemUse.TargetUnitId,
                        Position            = itemUse.Position,
                        CancelActiveTrade   = true,
                        ClientContextToken  = itemUse.ContextToken,
                        ClientRequestSource = nameof(ClientItemUse)
                    };

                    ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
                    session.Player.CastSpell(itemSpecial.Spell4IdOnActivate, spellParameters);
                }
            }
        }
    }
}
