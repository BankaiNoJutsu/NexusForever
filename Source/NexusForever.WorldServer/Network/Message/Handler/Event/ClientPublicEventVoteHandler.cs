using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;

namespace NexusForever.WorldServer.Network.Message.Handler.Event
{
    public class ClientPublicEventVoteHandler : IMessageHandler<IWorldSession, ClientPublicEventVote>
    {
        public void HandleMessage(IWorldSession session, ClientPublicEventVote publicEventVote)
        {
            // WildStar64.exe client opcode 0x06EE carries event, vote, team, and choice;
            // route the whole mapped envelope so stale or cross-team replies are ignored.
            session.Player.Map.PublicEventManager.RespondVote(
                session.Player,
                publicEventVote.EventId,
                publicEventVote.VoteId,
                publicEventVote.TeamId,
                publicEventVote.Choice);
        }
    }
}
