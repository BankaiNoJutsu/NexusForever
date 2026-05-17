using NexusForever.Network.Message;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSetStanceHandler : IMessageHandler<IWorldSession, ClientSetStance>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientSetStanceHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientSetStance innateChange)
        {
            ClassEntry classEntry = gameTableManager.Class.GetEntry((uint)session.Player.Class);
            if (classEntry == null)
                throw new InvalidPacketValueException();

            if (innateChange.InnateIndex >= classEntry.Spell4IdInnateAbilityActive.Length
                || classEntry.Spell4IdInnateAbilityActive[innateChange.InnateIndex] == 0u)
                throw new InvalidPacketValueException();

            session.Player.InnateIndex = innateChange.InnateIndex;

            session.EnqueueMessageEncrypted(new ServerStanceChanged
            {
                InnateIndex = session.Player.InnateIndex
            });
        }
    }
}
