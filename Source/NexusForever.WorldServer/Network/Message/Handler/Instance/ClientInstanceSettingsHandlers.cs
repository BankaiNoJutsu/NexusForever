using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Setting;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientClosedInstanceSettingsHandler : IMessageHandler<IWorldSession, ClientClosedInstanceSettings>
    {
        private readonly ILogger<ClientClosedInstanceSettingsHandler> log;

        public ClientClosedInstanceSettingsHandler(ILogger<ClientClosedInstanceSettingsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientClosedInstanceSettings closedInstanceSettings)
        {
            log.LogDebug("Ignoring instance-settings dialog close from player {PlayerGuid}: portal unit id {InstancePortalUnitId}.",
                session.Player?.Guid, closedInstanceSettings.InstancePortalUnitId);
        }
    }

    public class ClientResetSingleInstanceHandler : IMessageHandler<IWorldSession, ClientResetSingleInstance>
    {
        private readonly ILogger<ClientResetSingleInstanceHandler> log;

        public ClientResetSingleInstanceHandler(ILogger<ClientResetSingleInstanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientResetSingleInstance resetSingleInstance)
        {
            log.LogDebug("Rejecting unsupported single instance reset from player {PlayerGuid}: portal unit id {InstancePortalUnitId}.",
                session.Player?.Guid, resetSingleInstance.InstancePortalUnitId);

            session.EnqueueMessageEncrypted(new ServerInstanceResetResult
            {
                Success = false
            });
        }
    }

    public class ClientSetInstanceSettingsHandler : IMessageHandler<IWorldSession, ClientSetInstanceSettings>
    {
        private readonly ILogger<ClientSetInstanceSettingsHandler> log;

        public ClientSetInstanceSettingsHandler(ILogger<ClientSetInstanceSettingsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSetInstanceSettings setInstanceSettings)
        {
            if (setInstanceSettings.Difficulty >= WorldDifficulty.Count)
                throw new InvalidPacketValueException($"Invalid instance difficulty received: {setInstanceSettings.Difficulty}");

            log.LogDebug("Ignoring unsupported instance settings update from player {PlayerGuid}: portal unit id {InstancePortalUnitId}, difficulty {Difficulty}, prime level {PrimeLevel}, rally {Rally}.",
                session.Player?.Guid,
                setInstanceSettings.InstancePortalUnitId,
                setInstanceSettings.Difficulty,
                setInstanceSettings.PrimeLevel,
                setInstanceSettings.Rally);
        }
    }
}
