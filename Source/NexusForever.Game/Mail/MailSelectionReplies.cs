using NexusForever.Game.Static.Mail;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Mail
{
    internal static class MailSelectionReplies
    {
        internal static IWritable[] Build(ulong mailId, IEnumerable<uint> attachmentIndices, bool cash, GenericError result)
        {
            var replies = new List<IWritable>();
            if (cash) replies.Add(new ServerMailResult { MailId = mailId, Action = MailResultAction.TakeCash, Result = result });
            foreach (uint index in attachmentIndices)
                replies.Add(new ServerMailTakeAttachment { MailId = mailId, Index = index, Result = result });
            return replies.ToArray();
        }
    }
}
