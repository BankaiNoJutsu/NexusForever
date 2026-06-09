using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Option;

namespace NexusForever.WorldServer.Network.Message.Handler.Option
{
    public class ClientRequestInputKeySetHandler : IMessageHandler<IWorldSession, ClientRequestInputKeySet>
    {
        public void HandleMessage(IWorldSession session, ClientRequestInputKeySet clientRequestInputKeySet)
        {
            if (InputKeySetScopeValidator.IsCharacterScoped(session, clientRequestInputKeySet.CharacterId))
                session.EnqueueMessageEncrypted(session.Player.KeybindingManager.Build());
            else
                session.EnqueueMessageEncrypted(session.Account.KeybindingManager.Build());
        }
    }

    internal static class InputKeySetScopeValidator
    {
        public static bool IsCharacterScoped(IWorldSession session, ulong characterId)
        {
            if (characterId == 0ul)
                return false;

            if (session.Player?.CharacterId != characterId)
                throw new NexusForever.Network.InvalidPacketValueException($"Invalid character keybinding scope received: {characterId}");

            return true;
        }
    }
}
