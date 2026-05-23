using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPetSetStanceHandler : IMessageHandler<IWorldSession, ClientPetSetStance>
    {
        private readonly ILogger<ClientPetSetStanceHandler> log;

        public ClientPetSetStanceHandler(ILogger<ClientPetSetStanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPetSetStance petSetStance)
        {
            if (session.Player?.Map == null)
                return;

            if (!Enum.IsDefined(petSetStance.Stance))
                throw new InvalidPacketValueException();

            uint? petUnitId = petSetStance.PetUnitId == 0u
                ? session.Player.VanityPetGuid
                : petSetStance.PetUnitId;

            if (!petUnitId.HasValue)
            {
                log.LogDebug("ClientPetSetStance: player={Player} has no active pet for stance={Stance}.",
                    session.Player.Guid, petSetStance.Stance);
                return;
            }

            IPetEntity pet = session.Player.Map.GetEntity<IPetEntity>(petUnitId.Value);
            if (pet == null || pet.OwnerGuid != session.Player.Guid)
            {
                log.LogDebug("ClientPetSetStance: pet {PetUnitId} not found or not owned by player {Player}.",
                    petUnitId.Value, session.Player.Guid);
                return;
            }

            pet.Stance = petSetStance.Stance;
            log.LogDebug("ClientPetSetStance: player={Player} petUnitId={PetUnitId} stance={Stance}",
                session.Player.Guid, petUnitId.Value, petSetStance.Stance);
        }
    }
}
