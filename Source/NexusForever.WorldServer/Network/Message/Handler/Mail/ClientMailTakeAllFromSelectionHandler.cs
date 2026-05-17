using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.WorldServer.Network.Message.Handler.Mail
{
    public class ClientMailTakeAllFromSelectionHandler : IMessageHandler<IWorldSession, ClientMailTakeAllFromSelection>
    {
        /// <summary>
        /// Handles a client request to take all cash and attachments from a selection of <see cref="IMailItem"/>s.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientMailTakeAllFromSelection mailTakeAll)
        {
            session.Player.MailManager.MailTakeAllFromSelection(mailTakeAll.MailList, mailTakeAll.MailboxUnitId);
        }
    }
}
