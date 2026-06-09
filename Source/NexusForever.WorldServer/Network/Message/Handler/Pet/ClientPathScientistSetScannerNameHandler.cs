using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPathScientistSetScannerNameHandler : IMessageHandler<IWorldSession, ClientPathScientistSetScannerName>
    {
        public void HandleMessage(IWorldSession session, ClientPathScientistSetScannerName setScannerName)
        {
            if (setScannerName.PetType != PetType.ScanBot)
                throw new InvalidPacketValueException();

            if (session.Player.PetCustomisationManager.GetCustomisation(PetType.ScanBot, setScannerName.PathScientistScanBotProfileId) == null)
                throw new InvalidPacketValueException();

            session.Player.PetCustomisationManager.RenamePet(PetType.ScanBot, setScannerName.PathScientistScanBotProfileId,
                setScannerName.Name);
        }
    }
}
