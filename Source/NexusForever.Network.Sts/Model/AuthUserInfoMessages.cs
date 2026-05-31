using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public abstract class AuthUserInfoMessage : IReadable
    {
        public string UserId { get; private set; }
        public bool ExternalAccount { get; private set; }
        public bool ServiceTimeSchedule { get; private set; }
        public bool ExternalAccountApps { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];
            if (rootNode == null)
                return;

            UserId              = rootNode.GetChildValue<string>("UserId");
            ExternalAccount     = rootNode["ExternalAccount"] != null;
            ServiceTimeSchedule = rootNode["ServiceTimeSchedule"] != null;
            ExternalAccountApps = rootNode["ExternalAccountApps"] != null;
        }
    }

    [Message("/Auth/GetUserInfo")]
    public class AuthGetUserInfoMessage : AuthUserInfoMessage
    {
    }

    [Message("/Auth/GetMyUserInfo")]
    public class AuthGetMyUserInfoMessage : AuthUserInfoMessage
    {
    }

    public class AuthUserInfoResponse : IWritable
    {
        public string UserId { get; set; }
        public uint UserCenter { get; set; }
        public string UserName { get; set; }
        public string LoginName { get; set; }
        public uint UserStatus { get; set; }
        public string Created { get; set; }
        public string ServiceTimeSchedule { get; set; }

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");

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

            writer.WriteStartElement("UserStatus");
            writer.WriteValue(UserStatus);
            writer.WriteEndElement();

            writer.WriteStartElement("Created");
            writer.WriteString(Created ?? "");
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(ServiceTimeSchedule))
            {
                writer.WriteStartElement("ServiceTimeSchedule");
                writer.WriteString(ServiceTimeSchedule);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
