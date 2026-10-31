using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet;

public class ClientPetMiniCommandHandler : IMessageHandler<IWorldSession, ClientPetMiniCommand>
{
    public void HandleMessage(IWorldSession session, ClientPetMiniCommand message)
    {
        if (session.Player is IPlayerPetMiniBar owner)
            owner.TryDismissPetMiniBar(message.ShortcutSet, message.SlotIndex);
    }
}
