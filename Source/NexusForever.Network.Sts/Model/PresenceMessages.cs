using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public abstract class PresenceMessage : IReadable
    {
        public string UserId { get; private set; }
        public string LoginName { get; private set; }
        public string UserName { get; private set; }
        public string Alias { get; private set; }
        public string GameCode { get; private set; }
        public string GameAccountId { get; private set; }
        public string ClientNetAddress { get; private set; }
        public string LocationId { get; private set; }
        public string AppId { get; private set; }

        public virtual void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];
            if (rootNode == null)
                return;

            UserId           = rootNode.GetChildValue<string>("UserId");
            LoginName        = rootNode.GetChildValue<string>("LoginName");
            UserName         = rootNode.GetChildValue<string>("UserName");
            Alias            = rootNode.GetChildValue<string>("Alias");
            GameCode         = rootNode.GetChildValue<string>("GameCode");
            GameAccountId    = rootNode.GetChildValue<string>("GameAccountId");
            ClientNetAddress = rootNode.GetChildValue<string>("ClientNetAddress");
            LocationId       = rootNode.GetChildValue<string>("LocationId");
            AppId            = rootNode.GetChildValue<string>("AppId");
        }
    }

    [Message("/Presence/Login")]
    public class PresenceLoginMessage : PresenceMessage
    {
    }

    [Message("/Presence/Logout")]
    public class PresenceLogoutMessage : PresenceMessage
    {
    }

    [Message("/Presence/GetUserInfo")]
    public class PresenceGetUserInfoMessage : PresenceMessage
    {
    }

    [Message("/Presence/SetAppData")]
    public class PresenceSetAppDataMessage : PresenceMessage
    {
    }

    [Message("/Presence/Reversed")]
    public class PresenceReversedMessage : PresenceMessage
    {
    }

    [Message("/Presence/SendUserInfo")]
    public class PresenceSendUserInfoMessage : PresenceMessage
    {
    }

    [Message("/Presence/XferRequest")]
    public class PresenceXferRequestMessage : PresenceMessage
    {
    }

    [Message("/Presence/XferPresences")]
    public class PresenceXferPresencesMessage : PresenceMessage
    {
    }

    public class PresenceUserInfoResponse : IWritable
    {
        public string LocationId { get; set; }
        public string UserId { get; set; }
        public uint UserCenter { get; set; }
        public string UserName { get; set; }
        public string LoginName { get; set; }
        public long AccessMask { get; set; }
        public uint UserStatus { get; set; }
        public string Status { get; set; }
        public string Created { get; set; }
        public List<string> Aliases { get; } = [];

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");

            writer.WriteStartElement("LocationId");
            writer.WriteString(LocationId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserId");
            writer.WriteString(UserId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserCenter");
            writer.WriteValue(UserCenter);
            writer.WriteEndElement();

            writer.WriteStartElement("UserName");
            writer.WriteString(UserName ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("LoginName");
            writer.WriteString(LoginName ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("AccessMask");
            writer.WriteValue(AccessMask);
            writer.WriteEndElement();

            writer.WriteStartElement("UserStatus");
            writer.WriteValue(UserStatus);
            writer.WriteEndElement();

            writer.WriteStartElement("Status");
            writer.WriteString(Status ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("Created");
            writer.WriteString(Created ?? "");
            writer.WriteEndElement();

            if (Aliases.Count != 0)
            {
                writer.WriteStartElement("Aliases");

                foreach (string alias in Aliases)
                {
                    writer.WriteStartElement("Alias");
                    writer.WriteString(alias ?? "");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }

    public class EmptyStsResponse : IWritable
    {
        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");
            writer.WriteEndElement();
        }
    }
}
