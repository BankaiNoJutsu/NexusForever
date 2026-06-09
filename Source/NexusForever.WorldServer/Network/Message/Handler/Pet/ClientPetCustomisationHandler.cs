using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Pet;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPetCustomisationHandler : IMessageHandler<IWorldSession, ClientPetCustomisation>
    {
        public void HandleMessage(IWorldSession session, ClientPetCustomisation petcustomisation)
        {
            IPlayer player = session.Player;
            IPetCustomisationManager petCustomisationManager = player?.PetCustomisationManager;
            if (petCustomisationManager == null)
                return;

            if (!Enum.IsDefined(petcustomisation.PetType))
            {
                SendFailure(session, petcustomisation, PetCustomizeResult.PetTypeNotSupported);
                return;
            }

            if (petcustomisation.FlairSlotIndex >= PetCustomisationManager.MaxCustomisationFlairs)
            {
                SendFailure(session, petcustomisation, PetCustomizeResult.InvalidSlot);
                return;
            }

            petCustomisationManager.AddCustomisation(petcustomisation.PetType,
                petcustomisation.PetObjectId,
                petcustomisation.FlairSlotIndex,
                petcustomisation.FlairId);
        }

        private static void SendFailure(IWorldSession session, ClientPetCustomisation petCustomisation, PetCustomizeResult reason)
        {
            session.EnqueueMessageEncrypted(new ServerPetCustomisationFailed
            {
                Reason         = reason,
                Type           = petCustomisation.PetType,
                PetUnitId      = petCustomisation.PetObjectId,
                FlairSlotIndex = petCustomisation.FlairSlotIndex,
                PetFlairId     = petCustomisation.FlairId
            });
        }
    }
}
