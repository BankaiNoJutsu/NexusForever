using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public interface IFortuneSessionManager
    {
        void SendStatus(IWorldSession session);
        void Start(IWorldSession session);
        void FlipCard(IWorldSession session, ClientFortuneFlipCard flipCard);
    }
}
